using System;
using UnityEngine;

namespace SY
{
    public static class EventDispatcherExtensions
    {
        #region 带参数的事件

        /// <summary>
        /// 注册带参数的事件 (MonoBehaviour 扩展方法)
        /// </summary>
        public static void AddEventListener<T>(this MonoBehaviour mono, string eventName, Action<T> callback)
        {
            EventManager.Instance.AddEventListener(eventName, callback, mono);
        }

        /// <summary>
        /// 触发带参数的事件 (MonoBehaviour 扩展方法)
        /// </summary>
        public static void TriggerEvent<T>(this MonoBehaviour mono, string eventName, T param)
        {
            EventManager.Instance.TriggerEvent(eventName, param);
        }

        /// <summary>
        /// 移除带参数的事件监听 (MonoBehaviour 扩展方法)
        /// </summary>
        public static void RemoveEventListener<T>(this MonoBehaviour mono, string eventName, Action<T> callback)
        {
            EventManager.Instance.RemoveEventListener(eventName, callback);
        }

        #endregion

        #region 不带参数的事件

        /// <summary>
        /// 注册不带参数的事件 (MonoBehaviour 扩展方法)
        /// </summary>
        public static void AddEventListener(this MonoBehaviour mono, string eventName, Action callback)
        {
            EventManager.Instance.AddEventListener(eventName, callback, mono);
        }

        /// <summary>
        /// 触发不带参数的事件 (MonoBehaviour 扩展方法)
        /// </summary>
        public static void TriggerEvent(this MonoBehaviour mono, string eventName)
        {
            EventManager.Instance.TriggerEvent(eventName);
        }

        /// <summary>
        /// 移除不带参数的事件监听 (MonoBehaviour 扩展方法)
        /// </summary>
        public static void RemoveEventListener(this MonoBehaviour mono, string eventName, Action callback)
        {
            EventManager.Instance.RemoveEventListener(eventName, callback);
        }

        #endregion

        /// <summary>
        /// 移除对象所有事件 (MonoBehaviour 扩展方法)
        /// </summary>
        public static void RemoveOwnerEvents(this MonoBehaviour mono)
        {
            EventManager.Instance.RemoveEventsByOwner(mono);
        }
    }
}