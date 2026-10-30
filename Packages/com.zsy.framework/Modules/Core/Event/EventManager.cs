using System;
using System.Collections.Generic;
using UnityEngine;
namespace ZSYFramework.Core
{
/// <summary>
/// 简易事件分发系统
/// </summary>
public class EventManager : MonoSingleton<EventManager>
{
// 带参数事件字典
private readonly Dictionary<Type, Dictionary<string, List<Delegate>>> _genericEvents = new();

// 无参数事件字典
private readonly Dictionary<string, List<Action>> _noParamEvents = new();

// 所有者-事件注册记录
private readonly Dictionary<object, List<EventRegistration>> _ownerRegistrations = new();

    #region 带参数事件注册管理

/// <summary>
/// 注册带参数事件
/// </summary>
public void AddEventListener<T>(string eventName, Action<T> callback, object owner)
{
    if (string.IsNullOrEmpty(eventName) || callback == null || owner == null)
    {
        Debug.LogWarning("Invalid parameters for AddEventListener");
        return;
    }

    var paramType = typeof(T);

    // 初始化类型字典
    if (!_genericEvents.ContainsKey(paramType))
    {
        _genericEvents[paramType] = new Dictionary<string, List<Delegate>>();
    }

    // 初始化事件列表
    if (!_genericEvents[paramType].ContainsKey(eventName))
    {
        _genericEvents[paramType][eventName] = new List<Delegate>();
    }

    _genericEvents[paramType][eventName].Add(callback);

    // 记录注册信息
    RegisterEventOwner(owner, eventName, paramType, callback);
}

/// <summary>
/// 触发带参数事件
/// </summary>
public void TriggerEvent<T>(string eventName, T param)
{
    if (string.IsNullOrEmpty(eventName))
    {
        Debug.LogWarning("Event name is null or empty");
        return;
    }

    var paramType = typeof(T);

    if (!_genericEvents.TryGetValue(paramType, out var typeEvents) ||
        !typeEvents.TryGetValue(eventName, out var listeners)) return;
    // 复制列表防止修改
    var listenersCopy = new List<Delegate>(listeners);

    foreach (var listener in listenersCopy)
    {
        try
        {
            ((Action<T>)listener)?.Invoke(param);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error dispatching event {eventName}: {e}");
        }
    }
}

    #endregion

    #region 无参数事件

/// <summary>
/// 注册无参数事件
/// </summary>
public void AddEventListener(string eventName, Action callback, object owner)
{
    if (string.IsNullOrEmpty(eventName) || callback == null || owner == null)
    {
        Debug.LogWarning("Invalid parameters for AddEventListener");
        return;
    }

    if (!_noParamEvents.ContainsKey(eventName))
    {
        _noParamEvents[eventName] = new List<Action>();
    }

    _noParamEvents[eventName].Add(callback);

    // 记录注册信息
    RegisterEventOwner(owner, eventName, null, callback);
}

/// <summary>
/// 触发无参数事件
/// </summary>
public void TriggerEvent(string eventName)
{
    if (string.IsNullOrEmpty(eventName))
    {
        Debug.LogWarning("Event name is null or empty");
        return;
    }

    if (!_noParamEvents.TryGetValue(eventName, out var listeners)) return;
    // 复制列表防止修改
    var listenersCopy = new List<Action>(listeners);

    foreach (var listener in listenersCopy)
    {
        try
        {
            listener?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error dispatching no-param event {eventName}: {e}");
        }
    }
}

    #endregion

    #region 事件清理

/// <summary>
/// 移除特定事件名称的所有回调
/// </summary>
public void RemoveEvent(string eventName)
{
    // 移除带参数事件
    foreach (var typeEntry in _genericEvents)
    {
        if (!typeEntry.Value.TryGetValue(eventName, out var value)) continue;
        // 从所有者记录中移除
        foreach (var callback in value)
        {
            RemoveEventFromOwnerRecords(eventName, typeEntry.Key, callback);
        }

        typeEntry.Value.Remove(eventName);
    }

    // 移除无参数事件
    if (!_noParamEvents.TryGetValue(eventName, out var @event)) return;
    foreach (var callback in @event)
    {
        RemoveEventFromOwnerRecords(eventName, null, callback);
    }

    _noParamEvents.Remove(eventName);
}

/// <summary>
/// 移除特定类型事件特定回调
/// </summary>
public void RemoveEventListener<T>(string eventName, Action<T> callback)
{
    if (string.IsNullOrEmpty(eventName) || callback == null) return;

    var paramType = typeof(T);

    if (!_genericEvents.TryGetValue(paramType, out var typeEvents) ||
        !typeEvents.TryGetValue(eventName, out var listeners)) return;
    listeners.Remove(callback);

    if (listeners.Count == 0)
    {
        typeEvents.Remove(eventName);
    }

    // 从所有者记录中移除
    RemoveEventFromOwnerRecords(eventName, paramType, callback);
}

/// <summary>
/// 移除特定无参数事件特定回调
/// </summary>
public void RemoveEventListener(string eventName, Action callback)
{
    if (string.IsNullOrEmpty(eventName) || callback == null) return;

    if (!_noParamEvents.TryGetValue(eventName, out var listeners)) return;
    listeners.Remove(callback);

    if (listeners.Count == 0)
    {
        _noParamEvents.Remove(eventName);
    }

    // 从所有者记录中移除
    RemoveEventFromOwnerRecords(eventName, null, callback);
}

/// <summary>
/// 移除所有者注册的所有事件
/// </summary>
public void RemoveEventsByOwner(object owner)
{
    if (owner == null || !_ownerRegistrations.TryGetValue(owner, out var registration)) return;

    var registrations = new List<EventRegistration>(registration);

    foreach (var reg in registrations)
    {
        if (reg.ParamType != null)
        {
            // 带参数事件
            if (!_genericEvents.TryGetValue(reg.ParamType, out var typeEvents) ||
                !typeEvents.TryGetValue(reg.EventName, out var listeners)) continue;
            listeners.Remove(reg.Callback);

            if (listeners.Count == 0)
            {
                typeEvents.Remove(reg.EventName);
            }
        }
        else
        {
            // 无参数事件
            if (!_noParamEvents.TryGetValue(reg.EventName, out var listeners)) continue;
            listeners.Remove((Action)reg.Callback);

            if (listeners.Count == 0)
            {
                _noParamEvents.Remove(reg.EventName);
            }
        }
    }

    _ownerRegistrations.Remove(owner);
}

    #endregion

    #region 所有者管理

private void RegisterEventOwner(object owner, string eventName, Type paramType, Delegate callback)
{
    if (!_ownerRegistrations.ContainsKey(owner))
    {
        _ownerRegistrations[owner] = new List<EventRegistration>();
    }

    _ownerRegistrations[owner].Add(new EventRegistration
    {
        EventName = eventName,
        ParamType = paramType,
        Callback = callback
    });
}

private void RemoveEventFromOwnerRecords(string eventName, Type paramType, Delegate callback)
{
    // 把自己加入的回调从中移除记录
    var ownersToRemove = new List<object>();

    foreach (var ownerEntry in _ownerRegistrations)
    {
        for (var i = ownerEntry.Value.Count - 1; i >= 0; i--)
        {
            var reg = ownerEntry.Value[i];
            if (reg.EventName == eventName &&
                reg.ParamType == paramType &&
                reg.Callback == callback)
            {
                ownerEntry.Value.RemoveAt(i);
            }
        }

        if (ownerEntry.Value.Count == 0)
        {
            ownersToRemove.Add(ownerEntry.Key);
        }
    }

    foreach (var owner in ownersToRemove)
    {
        _ownerRegistrations.Remove(owner);
    }
}

    #endregion

/// <summary>
/// 清理所有事件监听
/// </summary>
public void ClearAllEvents()
{
    _genericEvents.Clear();
    _noParamEvents.Clear();
    _ownerRegistrations.Clear();
}

private void OnDestroy()
{
    ClearAllEvents();
}

/// <summary>
/// 事件注册记录
/// </summary>
private struct EventRegistration
{
    public string EventName;
    public Type ParamType;
    public Delegate Callback;
}
}
}
