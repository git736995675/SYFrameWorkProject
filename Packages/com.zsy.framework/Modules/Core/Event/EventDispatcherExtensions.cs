using System;
using UnityEngine;

namespace ZSYFramework.Core;
/// <summary>
/// 事件派发扩展方法集，为任意对象提供便捷的事件注册与触发入口。
/// </summary>
public static class EventDispatcherExtensions
{
    #region 带参数事件

/// <summary>
/// 注册带参数事件 (MonoBehaviour 扩展方法)
/// </summary>
public static void AddEventListener<T>(this MonoBehaviour mono, string eventName, Action<T> callback)
{
    EventManager.Instance.AddEventListener(eventName, callback, mono);
}

/// <summary>
/// 触发带参数事件 (MonoBehaviour 扩展方法)
/// </summary>
public static void TriggerEvent<T>(this MonoBehaviour mono, string eventName, T param)
{
    EventManager.Instance.TriggerEvent(eventName, param);
}

/// <summary>
/// 移除带参数事件回调 (MonoBehaviour 扩展方法)
/// </summary>
public static void RemoveEventListener<T>(this MonoBehaviour mono, string eventName, Action<T> callback)
{
    EventManager.Instance.RemoveEventListener(eventName, callback);
}

    #endregion

    #region 无参数事件

/// <summary>
/// 注册无参数事件 (MonoBehaviour 扩展方法)
/// </summary>
public static void AddEventListener(this MonoBehaviour mono, string eventName, Action callback)
{
    EventManager.Instance.AddEventListener(eventName, callback, mono);
}

/// <summary>
/// 触发无参数事件 (MonoBehaviour 扩展方法)
/// </summary>
public static void TriggerEvent(this MonoBehaviour mono, string eventName)
{
    EventManager.Instance.TriggerEvent(eventName);
}

/// <summary>
/// 移除无参数事件回调 (MonoBehaviour 扩展方法)
/// </summary>
public static void RemoveEventListener(this MonoBehaviour mono, string eventName, Action callback)
{
    EventManager.Instance.RemoveEventListener(eventName, callback);
}

    #endregion

/// <summary>
/// 移除所有者所有事件 (MonoBehaviour 扩展方法)
/// </summary>
public static void RemoveOwnerEvents(this MonoBehaviour mono)
{
    EventManager.Instance.RemoveEventsByOwner(mono);
}
}
