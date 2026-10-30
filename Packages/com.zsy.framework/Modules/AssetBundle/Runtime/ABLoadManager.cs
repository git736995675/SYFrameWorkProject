using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;
using ZSYFramework.Core;
namespace ZSYFramework.AssetBundle
{
/// <summary>
/// 资源加载模式枚举：Editor 直读或 AssetBundle 加载。
/// </summary>
public enum LoadMode
{
Editor, // 编辑器模式
AssetBundle // AssetBundle模式
}

/// <summary>
/// AssetBundle 加载管理器。负责 AB 包的加载、依赖管理与资源卸载，支持编辑器/AB双模式。
/// </summary>
public class ABLoadManager : MonoSingleton<ABLoadManager>
{
#if UNITY_EDITOR
public LoadMode loadMode = LoadMode.Editor;
#else
  public LoadMode loadMode = LoadMode.AssetBundle;
#endif

// 清单文件
private AssetBundleManifest _manifest;

// manifest文件是否已加载
private bool _manifestLoading = false;

// 已加载bundle缓存
private readonly Dictionary<string, UnityEngine.AssetBundle> _bundleCache = new();

//资源缓存
private readonly Dictionary<string, Object> _assetCache = new();

// 正在加载中的回调记录（防止重复加载）
private readonly Dictionary<string, List<Action<Object>>> _loadingCallbacks = new();

//bundle的引用计数
private readonly Dictionary<string, (UnityEngine.AssetBundle bundle, int refCount)> _bundleReferenceCount = new();

//正在加载中的bundle
private readonly HashSet<string> _loadingBundles = new();

protected override void Awake()
{
    base.Awake();

#if UNITY_EDITOR
    loadMode = LoadModeSetting;
#endif
}

public static LoadMode LoadModeSetting
{
    get
    {
        if (!PlayerPrefs.HasKey("ResLoadMode")) return LoadMode.Editor;
        var mode = (LoadMode)PlayerPrefs.GetInt("ResLoadMode");
        return mode;
    }
    set
    {
        PlayerPrefs.SetInt("ResLoadMode", (int)value);
        if (Application.isPlaying)
            Instance.loadMode = value;
    }
}

public void LoadAudioClip(string assetName, Action<AudioClip> callback)
{
    ResourceUtils.Instance.GetResInfo(assetName,
        (abName, resPath) => { StartCoroutine(LoadAssetAsync(abName, resPath, callback)); });
}

public void LoadSprite(string assetName, Action<Sprite> callback)
{
    ResourceUtils.Instance.GetResInfo(assetName,
        (abName, resPath) => { StartCoroutine(LoadAssetAsync(abName, resPath, callback)); });
}

public void LoadPrefab(string assetName, Action<GameObject> callback)
{
    ResourceUtils.Instance.GetResInfo(assetName,
        (abName, resPath) => { StartCoroutine(LoadAssetAsync(abName, resPath, callback)); });
}

public void LoadTextAsset(string assetName, Action<TextAsset> callback)
{
    ResourceUtils.Instance.GetResInfo(assetName,
        (abName, resPath) => { StartCoroutine(LoadAssetAsync(abName, resPath, callback)); });
}

public static void Load(string assetName, Action onComplete)
{
    Instance.Load<Object>(assetName, o => { onComplete?.Invoke(); });
}

public void Load<T>(string assetName, Action<T> callback) where T : Object
{
    ResourceUtils.Instance.GetResInfo(assetName,
        (abName, resPath) => { StartCoroutine(LoadAssetAsync(abName, resPath, callback)); });
}

public void Load<T>(string bundleName, string assetPath, Action<T> callback) where T : Object
{
    StartCoroutine(LoadAssetAsync(bundleName, assetPath, callback));
}

public void LoadAsync<T>(string assetName, Action<T> completed) where T : Object
{
    ResourceUtils.Instance.GetResInfo(assetName,
        (abName, resPath) => { StartCoroutine(Instance.LoadAssetAsync(abName, resPath, completed)); });
}

public void LoadAsync<T>(string bundleName, string assetPath, Action<T> completed) where T : Object
{
    StartCoroutine(Instance.LoadAssetAsync(bundleName, assetPath, completed));
}

private static string GetBundlePath(string bundleName)
{
    // 优先使用热更下载目录中的同名 bundle（版本更新后的新版本），不存在再回落到包内置资源
    var hotPath = Path.Combine(ResourceUtils.HotResDir, bundleName);
    if (File.Exists(hotPath)) return hotPath;

    var path = Path.Combine(Application.streamingAssetsPath, GetPlatformName(), bundleName);
    return path;
}

private static string GetPlatformName()
{
#if UNITY_WEBGL && !UNITY_EDITOR
    return "WebGL";
#elif UNITY_ANDROID && !UNITY_EDITOR
    return "Android";
#elif UNITY_IOS && !UNITY_EDITOR
    return "iOS";
#elif UNITY_EDITOR || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    return "StandaloneWindows";
#endif
}

// 加载主Manifest
// ReSharper disable Unity.PerformanceAnalysis
private IEnumerator LoadManifest()
{
    if (_manifestLoading)
    {
        while (_manifestLoading || _manifest == null) 
        {
            yield return null;
        }
        yield break;
    }

    _manifestLoading = true;
    // 加载主Manifest的AssetBundle
    var path = GetBundlePath($"{GetPlatformName()}.unity3d");

#if UNITY_WEBGL && !UNITY_EDITOR
     var request = UnityWebRequestAssetBundle.GetAssetBundle(path);
     yield return request.SendWebRequest();
     if (request.result != UnityWebRequest.Result.Success)
     {
         Debug.LogError("Failed to load asset bundle" + request.error);
         _manifestLoading = false; // 确保失败时重置状态
         yield break;
     }
     var bundle = DownloadHandlerAssetBundle.GetContent(request);
#else
    var request = UnityEngine.AssetBundle.LoadFromFileAsync(path);
    yield return request;
    if (request.assetBundle == null)
    {
        Debug.LogError("Failed to load asset bundle");
        yield break;
    }

    var bundle = request.assetBundle;
#endif
    var assetRequest = bundle.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");
    yield return assetRequest;
    _manifest = assetRequest.asset as AssetBundleManifest;
    ;
    // 卸载Bundle（但不销毁已加载的Manifest）
    bundle.Unload(false);
    _manifestLoading = false;
}


#if UNITY_EDITOR
private void AssetDatabaseLoad<T>(string assetName, Action<T> onSuccess) where T : Object
{
    if (_assetCache.TryGetValue(assetName, out var asset))
    {
        onSuccess?.Invoke(asset as T);
        return;
    }

    asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetName);
    _assetCache[assetName] = asset;
    onSuccess?.Invoke((T)asset);
}
#endif

private IEnumerator LoadAssetAsync<T>(string bundleName, string assetName, Action<T> onSuccess) where T : Object
{
    // 加载时检查缓存
    if (_assetCache.TryGetValue(assetName, out var cacheAsset))
    {
        onSuccess?.Invoke(cacheAsset as T);
        yield break;
    }
    
#if UNITY_EDITOR
    if (loadMode == LoadMode.Editor)
    {
        AssetDatabaseLoad(assetName, onSuccess);
        yield break;
    }
#endif
    
    // 加载时检查缓存
    if (_bundleCache.TryGetValue(bundleName, out var cacheBundle))
    {
        var cacheRequest = cacheBundle.LoadAssetAsync<T>(assetName);
        yield return cacheRequest;
        onSuccess?.Invoke(cacheRequest.asset as T);
        yield break;
    }

    // 如果正在加载中，添加到回调列表
    if (_loadingCallbacks.TryGetValue(assetName, out var callbacks))
    {
        callbacks.Add(obj => onSuccess?.Invoke(obj as T));
        yield break;
    }

    // 新建加载任务
    _loadingCallbacks[assetName] = new List<Action<Object>>()
    {
        obj => onSuccess?.Invoke(obj as T)
    };

    // 如果该bundle加载中等待
    while (_loadingBundles.Contains(bundleName))
    {
        yield return null;
    }
    _loadingBundles.Add(bundleName);

    yield return LoadManifest();
    
    // 2. 加载所有依赖
    var dependencies = _manifest.GetAllDependencies(bundleName);
    foreach (var dep in dependencies)
    {
        if (!_bundleCache.ContainsKey(dep))
        {
            yield return LoadBundleAsync(dep);
        }
    }

    // 3. 加载目标Bundle
    if (!_bundleCache.ContainsKey(bundleName))
    {
        yield return LoadBundleAsync(bundleName);
    }

    // 增加计数
    if (_bundleReferenceCount.TryGetValue(bundleName, out var cacheEntry))
    {
        _bundleReferenceCount[bundleName] = (cacheEntry.bundle, cacheEntry.refCount + 1);
    }
    else
    {
        _bundleReferenceCount[bundleName] = (_bundleCache[bundleName], 1);
    }

    // 4. 加载资源
    var bundle = _bundleCache[bundleName];
    var request = bundle.LoadAssetAsync<T>(assetName);
    yield return request;

    if (request.asset == null)
    {
        SYLog.Error($"Failed to load asset: {assetName}", LogCategory.Resource, this);
        yield break;
    }

    var asset = request.asset as T;
    _assetCache[assetName] = asset;
    _loadingBundles.Remove(bundleName);
    
    // 调用所有回调
    foreach (var callback in _loadingCallbacks[assetName])
    {
        callback?.Invoke(asset);
    }
    _loadingCallbacks.Remove(assetName);
}

// 加载单个Bundle
private IEnumerator LoadBundleAsync(string bundleName)
{
    var path = GetBundlePath(bundleName);

#if UNITY_WEBGL && !UNITY_EDITOR
    var request = UnityWebRequestAssetBundle.GetAssetBundle(path);
    yield return request.SendWebRequest();
    if (request.result != UnityWebRequest.Result.Success)
    {
        yield break;
    }
    var bundle = DownloadHandlerAssetBundle.GetContent(request);
#else
    var request = UnityEngine.AssetBundle.LoadFromFileAsync(path);
    yield return request;
    if (request.assetBundle == null)
    {
        yield break;
    }

    var bundle = request.assetBundle;
#endif
    _bundleCache[bundleName] = bundle;
    _loadingBundles.Remove(bundleName);
}

// 销毁后减少计数
public void Release(string assetName)
{
    ResourceUtils.Instance.GetResInfo(assetName, (abName, resPath) =>
    {
        // 1. 释放目标Bundle
        if (_bundleReferenceCount.TryGetValue(abName, out var entry))
        {
            var newCount = entry.refCount - 1;
            if (newCount <= 0)
            {
                // 卸载Bundle
                UnloadBundle(abName, true);
            }
            else
            {
                // 更新计数
                _bundleReferenceCount[abName] = (entry.bundle, newCount);
            }
        }

        // 2. 释放依赖
        if (_manifest != null)
        {
            var dependencies = _manifest.GetAllDependencies(abName);
            foreach (var dep in dependencies)
            {
                ReleaseDependency(dep);
            }
        }
    });
}

private void ReleaseDependency(string depBundleName)
{
    if (!_bundleReferenceCount.TryGetValue(depBundleName, out var depEntry)) return;
    var newCount = depEntry.refCount - 1;
    if (newCount <= 0)
    {
        depEntry.bundle.Unload(true);
        _bundleReferenceCount.Remove(depBundleName);
    }
    else
    {
        _bundleReferenceCount[depBundleName] = (depEntry.bundle, newCount);
    }
}


// 卸载资源
private void UnloadBundle(string bundleName, bool unloadAll = false)
{
    if (!_bundleCache.TryGetValue(bundleName, out var bundle)) return;
    bundle.Unload(unloadAll);
    _bundleCache.Remove(bundleName);
    _bundleReferenceCount.Remove(bundleName);
}

// 延迟卸载示例
private IEnumerator UnloadAfterDelay(string bundleName)
{
    yield return new WaitForSeconds(10);
    if (!_bundleReferenceCount.ContainsKey(bundleName) ||
        _bundleReferenceCount[bundleName].refCount > 0) yield break;
    _bundleReferenceCount[bundleName].bundle.Unload(true);
    _bundleReferenceCount.Remove(bundleName);
}
}
}
