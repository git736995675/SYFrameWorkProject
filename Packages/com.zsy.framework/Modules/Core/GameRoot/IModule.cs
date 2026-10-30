namespace ZSYFramework.Core
{
    /// <summary>
    /// 框架模块生命周期接口。实现后通过 <see cref="GameRoot.RegisterModule"/> 注册，
    /// 由 GameRoot 按优先级统一驱动初始化、帧更新与销毁。
    /// 推荐继承 <see cref="ModuleBase"/>，只需重写关心的生命周期。
    /// </summary>
    public interface IModule
    {
        /// <summary>模块名称，用于日志与查询。</summary>
        string ModuleName { get; }

        /// <summary>初始化优先级，数值越大越先初始化；销毁时按相反顺序执行。</summary>
        int Priority { get; }

        /// <summary>是否需要每帧回调 <see cref="OnUpdate"/>。</summary>
        bool EnableUpdate { get; }

        /// <summary>模块初始化，由 GameRoot 在启动时按优先级从高到低调用。</summary>
        void OnInit();

        /// <summary>每帧更新，仅当 <see cref="EnableUpdate"/> 为 true 时被调用。</summary>
        void OnUpdate();

        /// <summary>模块销毁，由 GameRoot 在退出时按初始化的相反顺序调用。</summary>
        void OnShutdown();
    }

    /// <summary>
    /// 模块实现基类。默认不参与帧更新，子类按需重写各生命周期。
    /// </summary>
    public abstract class ModuleBase : IModule
    {
        /// <inheritdoc />
        public abstract string ModuleName { get; }

        /// <inheritdoc />
        public virtual int Priority => 0;

        /// <inheritdoc />
        public virtual bool EnableUpdate => false;

        /// <inheritdoc />
        public virtual void OnInit()
        {
        }

        /// <inheritdoc />
        public virtual void OnUpdate()
        {
        }

        /// <inheritdoc />
        public virtual void OnShutdown()
        {
        }
    }
}
