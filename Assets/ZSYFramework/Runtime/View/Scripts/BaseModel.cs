// Model基类（使用ScriptableObject持久化数据）

using System;
using UnityEngine;

namespace ZSYFramework.Runtime.View.Scripts
{
    public abstract class BaseModel : ScriptableObject
    {
        public UILayer layer = UILayer.Normal; // 层级管理
        public float maskAlpha = 0.8f;
        public bool isMaskClose = true;

        public ViewArgs BaseArgs { get; private set; } //参数

        private Action OnDataChanged { get; set; } // 数据变更事件


        public void AddChangeEvent(Action changeEvent)
        {
            OnDataChanged += changeEvent;
        }

        public void RemoveChangeEvent(Action changeEvent)
        {
            OnDataChanged -= changeEvent;
        }

        public void Initialize(ViewArgs viewArgs)
        {
            BaseArgs = viewArgs;
            Initialize();
        }

        public void RefreshArgs(ViewArgs viewArgs)
        {
            BaseArgs = viewArgs;
            Reset();
            NotifyDataChanged();
        }

        // 通知View更新
        private void NotifyDataChanged()
        {
            OnDataChanged?.Invoke();
        }


        protected virtual void Initialize()
        {
        }

        // 重置
        protected virtual void Reset()
        {
        }
    }
}