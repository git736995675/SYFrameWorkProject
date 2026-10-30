using System;

namespace ZSYFramework.Core
{
    /// <summary>
    /// 委托适配器：把既有 Manager（如各 MonoSingleton）包装成 <see cref="IModule"/>
    /// 接入 <see cref="GameRoot"/> 调度，无需改动其继承结构。
    /// 通常通过 <see cref="GameRoot.RegisterAdapter"/> 创建，无需手动 new。
    /// </summary>
    public sealed class ModuleAdapter : IModule
    {
        private readonly Action _onInit;
        private readonly Action _onUpdate;
        private readonly Action _onShutdown;

        /// <inheritdoc />
        public string ModuleName { get; }

        /// <inheritdoc />
        public int Priority { get; }

        /// <inheritdoc />
        public bool EnableUpdate { get; }

        /// <summary>
        /// 创建一个委托适配器。
        /// </summary>
        /// <param name="moduleName">模块名称，用于日志与查询。</param>
        /// <param name="priority">初始化优先级，越大越先初始化。</param>
        /// <param name="enableUpdate">是否参与每帧更新。</param>
        /// <param name="onInit">初始化回调。</param>
        /// <param name="onUpdate">每帧回调。</param>
        /// <param name="onShutdown">销毁回调。</param>
        public ModuleAdapter(string moduleName, int priority = 0, bool enableUpdate = false,
            Action onInit = null, Action onUpdate = null, Action onShutdown = null)
        {
            ModuleName = moduleName;
            Priority = priority;
            EnableUpdate = enableUpdate;
            _onInit = onInit;
            _onUpdate = onUpdate;
            _onShutdown = onShutdown;
        }

        /// <inheritdoc />
        public void OnInit()
        {
            _onInit?.Invoke();
        }

        /// <inheritdoc />
        public void OnUpdate()
        {
            _onUpdate?.Invoke();
        }

        /// <inheritdoc />
        public void OnShutdown()
        {
            _onShutdown?.Invoke();
        }
    }
}
