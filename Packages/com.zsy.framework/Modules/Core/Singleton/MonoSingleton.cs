using ZSYFramework.Core.Json;
﻿using UnityEngine;

namespace ZSYFramework.Core;
// MonoBehaviour单例基类
/// <summary>
/// MonoBehaviour 泛型单例基类。子类挂载后通过 Instance 全局访问，场景中自动查找或创建实例。
/// </summary>
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
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
        DontDestroyOnLoad(gameObject);
    }
    else if (_instance != this)
    {
        Debug.LogWarning($"Multiple {typeof(T).Name} detected. Destroying duplicate.");
        Destroy(gameObject);
    }
}

private static void CreateInstance()
{
    var go = new GameObject($"[Global] {typeof(T).Name}");
    _instance = go.AddComponent<T>();
    _instance.OnInstanceCreated();
    
    if(Application.isPlaying)
        DontDestroyOnLoad(go);
}

protected virtual void OnInstanceCreated()
{
    // 动态创建时的初始化
    
}

protected virtual void OnApplicationQuit()
{
    if (_instance == this) _instance = null;
}

public static void Release()
{
    if (_instance == null) return;
    var go = _instance.gameObject;
    _instance = null;
    if (Application.isPlaying) Destroy(go);
    else DestroyImmediate(go);
}
}
