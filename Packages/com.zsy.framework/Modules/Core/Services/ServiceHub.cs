using System;
using Object = UnityEngine.Object;

namespace ZSYFramework.Core
{
    /// <summary>
    /// 跨模块服务注册中心（组合根）。功能模块依赖 Core 里的接口而非彼此：
    /// 启动时由资源/音频等能力的提供方把实现注入进来，消费方零直接引用。
    ///
    /// 使用约定：
    /// - 注入方（AssetBundle/Audio 模块）在 GameRoot 初始化阶段调用 <see cref="Register"/>；
    /// - 消费方（Table/Audio/Localization/UI）随时经 <see cref="Asset"/> 等属性取用；
    /// - 未注入时消费方收到错误日志与 null 回调，不抛异常，保证模块可独立调试。
    /// </summary>
    public static class ServiceHub
    {
        private static IAssetService _asset;
        private static IConfigTextLoader _configText;
        private static IAudioFeedback _audioFeedback;

        /// <summary>资源加载服务。未注入时返回日志兜底实现。</summary>
        public static IAssetService Asset => _asset ??= new MissingAssetService();

        /// <summary>配置文本加载服务。未注入时返回日志兜底实现。</summary>
        public static IConfigTextLoader ConfigText => _configText ??= new MissingConfigTextLoader();

        /// <summary>UI 音效反馈服务。未注入时静默忽略。</summary>
        public static IAudioFeedback AudioFeedback => _audioFeedback;

        /// <summary>注册（或替换）资源加载服务实现。传 null 取消注册。</summary>
        public static void Register(IAssetService service) => _asset = service;

        /// <summary>注册（或替换）配置文本加载服务实现。传 null 取消注册。</summary>
        public static void Register(IConfigTextLoader service) => _configText = service;

        /// <summary>注册（或替换）UI 音效反馈服务实现。传 null 取消注册。</summary>
        public static void Register(IAudioFeedback service) => _audioFeedback = service;

        /// <summary>资源服务缺失时的日志兜底：调用方拿到 null 回调并收到明确错误提示。</summary>
        private sealed class MissingAssetService : IAssetService
        {
            public void Load<T>(string assetName, Action<T> onSuccess) where T : Object
            {
                SYLog.Error($"[ServiceHub] IAssetService 未注入, 无法加载 {assetName}" +
                            "(由 AssetBundle 模块在启动时注册)", LogCategory.Resource);
                onSuccess?.Invoke(null);
            }

            public System.Threading.Tasks.Task<T> LoadAsync<T>(string assetName) where T : Object
            {
                SYLog.Error($"[ServiceHub] IAssetService 未注入, 无法加载 {assetName}", LogCategory.Resource);
                return System.Threading.Tasks.Task.FromException<T>(new InvalidOperationException(
                    "[ServiceHub] IAssetService 未注入"));
            }
        }

        /// <summary>配置加载服务缺失时的日志兜底。</summary>
        private sealed class MissingConfigTextLoader : IConfigTextLoader
        {
            public void LoadConfig<T>(string assetName, Action<T> onSuccess)
            {
                SYLog.Error($"[ServiceHub] IConfigTextLoader 未注入, 无法加载配置 {assetName}", LogCategory.Config);
                onSuccess?.Invoke(default);
            }

            public void LoadConfigRaw(string bundleName, string assetName, Action<Object> onSuccess)
            {
                SYLog.Error($"[ServiceHub] IConfigTextLoader 未注入, 无法加载 {bundleName}/{assetName}",
                    LogCategory.Config);
                onSuccess?.Invoke(null);
            }
        }
    }
}
