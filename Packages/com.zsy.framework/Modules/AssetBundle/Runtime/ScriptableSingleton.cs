using System;
using UnityEngine;
using ZSYFramework.Core.Json;

namespace ZSYFramework.AssetBundle;
/// <summary>
/// ScriptableObject 单例基类。通过 AssetBundle 加载持久化配置资产并缓存为全局实例。
/// </summary>
public abstract class ScriptableSingleton<T> : ScriptableObject where T : ScriptableSingleton<T>
{
public static T Instance { get; private set; }

public static void Init(Action callback)
{
    if (Instance)
    {
        callback();
        return;
    }

    ABLoadManager.Instance.Load<T>(typeof(T).ToString(), (setting) =>
    {
        Instance = setting;
        callback();
    });
}
}
