using UnityEngine;
namespace ZSYFramework.Core
{
// 场景单例基类 (单泡)
/// <summary>
/// 场景级 MonoBehaviour 单例基类。切换场景后实例销毁，不跨场景存活。
/// </summary>
public abstract class SceneMonoSingleton<T> : MonoBehaviour where T : SceneMonoSingleton<T>
{
private static T _instance;

public static T Instance
{
    get
    {
        if (_instance != null) return _instance;
        _instance = FindObjectOfType<T>(true);

        if (_instance == null)
        {
            CreateInstance();
        }

        return _instance;
    }
}

protected virtual void Awake()
{
    if (_instance == null)
    {
        _instance = this as T;
    }
    else if (_instance != this)
    {
        Debug.LogWarning($"Multiple {typeof(T).Name} detected. Destroying duplicate.");
        Destroy(gameObject);
    }
}

private static void CreateInstance()
{
    var go = new GameObject($"[Scene] {typeof(T).Name}");
    _instance = go.AddComponent<T>();
    _instance.OnInstanceCreated();
}

protected virtual void OnInstanceCreated()
{
    // 单例创建时的额外初始化
}

protected virtual void OnDestroy()
{
    if (_instance == this)
    {
        _instance = null;
    }
}

public static void Release()
{
    if (_instance == null) return;
    var go = _instance.gameObject;
    _instance = null;
    if (Application.isPlaying)
        Destroy(go);
    else
        DestroyImmediate(go);
}
}
}
