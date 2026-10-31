using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using ZSYFramework.Core;

namespace ZSYFramework.AssetBundle
{
    /// <summary>
    /// AssetBundle 加载器：清单驱动 + 依赖解析 + 引用计数。
    /// - 定位来自 manifest.json（资源名→bundle 名→路径），不依赖运行时拼路径；
    /// - 远程 bundle（isRemote）未下载时给出明确错误，提示走 VersionUpdater；
    /// - 引用计数精细到 bundle，Release 时依赖计数一并递减。
    /// </summary>
    public static class BundleAssetLoader
    {
        // bundle 名 → 已加载 bundle
        private static readonly Dictionary<string, LoadedBundle> _loaded = new();

        private sealed class LoadedBundle
        {
            public UnityEngine.AssetBundle bundle;
            public int refCount;
            // 资源缓存（bundle 内相对路径 → 资产）
            public readonly Dictionary<string, Object> assets = new();
        }

        // 资源名 → 等待回调（防重复加载）
        private static readonly Dictionary<string, List<Action<Object>>> _pending = new();

        /// <summary>已加载 bundle 数（调试用）。</summary>
        public static int LoadedCount => _loaded.Count;

        /// <summary>
        /// 按框架资源名加载资产（回调风格）。内部完成：清单定位 → 依赖加载 → bundle 加载 → 资产加载。
        /// </summary>
        public static void Load<T>(string resourceName, Action<T> onSuccess) where T : Object
        {
            LoadInternal<T>(resourceName, onSuccess).FireAndForget();
        }

        private static async Task LoadInternal<T>(string resourceName, Action<T> onSuccess) where T : Object
        {
            var manifest = await BundleManifestManager.LoadAsync();
            if (manifest == null)
            {
                SYLog.Error($"[BundleSystem] 清单未就绪, 无法加载 {resourceName}", LogCategory.Resource);
                onSuccess?.Invoke(null);
                return;
            }

            var bundleName = manifest.GetBundleOf(resourceName);
            if (bundleName == null)
            {
                SYLog.Error($"[BundleSystem] 清单中不存在资源: {resourceName}", LogCategory.Resource);
                onSuccess?.Invoke(null);
                return;
            }

            var bundle = await EnsureBundleAsync(bundleName);
            if (bundle == null)
            {
                onSuccess?.Invoke(null);
                return;
            }

            // 资产加载（bundle 内按名字查找即可，Unity 对 assetBundle.LoadAsset 支持省路径匹配）
            if (!bundle.assets.TryGetValue(resourceName, out var asset) || asset == null)
            {
                var request = bundle.bundle.LoadAssetAsync<T>(resourceName);
                while (!request.isDone) await Task.Yield();
                asset = request.asset;
                bundle.assets[resourceName] = asset;
            }

            if (asset == null)
            {
                SYLog.Error($"[BundleSystem] bundle {bundleName} 中未找到 {resourceName}", LogCategory.Resource);
            }

            onSuccess?.Invoke(asset as T);
        }

        /// <summary>释放资源（对应 bundle 及其依赖计数 -1，归零时卸载）。</summary>
        public static void Release(string resourceName)
        {
            var manifest = BundleManifestManager.Current;
            if (manifest == null) return;
            var bundleName = manifest.GetBundleOf(resourceName);
            if (bundleName == null) return;

            UnwindRef(bundleName);
            var info = manifest.GetBundle(bundleName);
            if (info?.dependencies != null)
            {
                foreach (var dep in info.dependencies)
                {
                    UnwindRef(dep);
                }
            }
        }

        private static void UnwindRef(string bundleName)
        {
            if (!_loaded.TryGetValue(bundleName, out var loaded)) return;
            loaded.refCount--;
            if (loaded.refCount <= 0)
            {
                loaded.bundle?.Unload(true);
                _loaded.Remove(bundleName);
            }
        }

        /// <summary>确保 bundle 及其全部依赖已加载并计数 +1。</summary>
        private static async Task<LoadedBundle> EnsureBundleAsync(string bundleName)
        {
            var manifest = BundleManifestManager.Current;

            // 1. 依赖先行
            var info = manifest?.GetBundle(bundleName);
            if (info?.dependencies != null)
            {
                foreach (var dep in info.dependencies)
                {
                    await EnsureBundleAsync(dep);
                }
            }

            // 2. 已加载 → 计数 +1
            if (_loaded.TryGetValue(bundleName, out var loaded))
            {
                loaded.refCount++;
                return loaded;
            }

            // 3. 检查远程 bundle 是否已下载到本地
            var path = BundleManifestManager.BundlePath(bundleName);
            if (info is { isRemote: true } && !File.Exists(Path.Combine(ResourceUtils.HotResDir, bundleName)))
            {
                SYLog.Error($"[BundleSystem] 远程 bundle 尚未下载: {bundleName}。" +
                            "请先执行版本更新(VersionUpdater)或改回内置", LogCategory.Resource);
                return null;
            }

            // 4. 从本地加载（Android apk 内路径走文件系统 API 即可，Unity 支持 jar: 协议的 LoadFromFile）
            if (!File.Exists(path))
            {
                SYLog.Error($"[BundleSystem] bundle 文件不存在: {path}", LogCategory.Resource);
                return null;
            }

            var request = UnityEngine.AssetBundle.LoadFromFileAsync(path);
            while (!request.isDone) await Task.Yield();
            if (request.assetBundle == null)
            {
                SYLog.Error($"[BundleSystem] bundle 加载失败: {bundleName}", LogCategory.Resource);
                return null;
            }

            loaded = new LoadedBundle { bundle = request.assetBundle, refCount = 1 };
            _loaded[bundleName] = loaded;
            return loaded;
        }
    }

    /// <summary>Task 丢弃辅助（fire-and-forget）。</summary>
    internal static class TaskExtensions
    {
        public static async void FireAndForget(this Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                SYLog.Error($"[BundleSystem] 异步加载异常: {e.Message}", LogCategory.Resource);
            }
        }
    }
}
