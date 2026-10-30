using System;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace ZSYFramework.Core
{
    /// <summary>
    /// 资源加载服务抽象。让数据/功能模块（Table、Audio 等）需要按名取资源时
    /// 不必直接依赖具体资源管线（AssetBundle 模块），由启动方通过 <see cref="ServiceHub"/>
    /// 注入实现。接口只约定"按框架资源名取对象"，屏蔽 bundle/路径细节。
    /// </summary>
    public interface IAssetService
    {
        /// <summary>按框架资源名加载资产（回调风格，与既有管线一致）。</summary>
        /// <param name="assetName">框架资源名（与资源清单中的资源名一致）。</param>
        /// <param name="onSuccess">加载成功回调，失败时收到 null。</param>
        /// <typeparam name="T">资产类型。</typeparam>
        void Load<T>(string assetName, Action<T> onSuccess) where T : Object;

        /// <summary>按框架资源名加载资产（Task 风格，失败时抛异常）。</summary>
        /// <param name="assetName">框架资源名。</param>
        /// <typeparam name="T">资产类型。</typeparam>
        Task<T> LoadAsync<T>(string assetName) where T : Object;
    }

    /// <summary>
    /// 配置文本加载抽象。把"从资源管线读配置 JSON 文本并反序列化"收敛为 Core 契约，
    /// Localization 等模块经此接口加载配置即可不依赖 Table/AssetBundle 模块。
    /// </summary>
    public interface IConfigTextLoader
    {
        /// <summary>按配置名加载并反序列化（回调风格）。</summary>
        /// <param name="assetName">配置资源名（不含扩展名，与生成表资源名一致）。</param>
        /// <param name="onSuccess">成功回调，收到反序列化后的对象；失败时收到 default。</param>
        /// <typeparam name="T">反序列化目标类型。</typeparam>
        void LoadConfig<T>(string assetName, Action<T> onSuccess);

        /// <summary>按 bundle 名与资源名加载原始配置文本资产（回调风格）。</summary>
        /// <param name="bundleName">bundle 名。</param>
        /// <param name="assetName">资源名。</param>
        /// <param name="onSuccess">成功回调，收到 TextAsset；失败时收到 null。</param>
        void LoadConfigRaw(string bundleName, string assetName, Action<Object> onSuccess);
    }

    /// <summary>
    /// UI 通用音效反馈抽象。UI 模块播放按钮音效等场景经此接口触发，
    /// 不直接依赖 Audio 模块；未注入实现时静默忽略（UI 依旧可用）。
    /// </summary>
    public interface IAudioFeedback
    {
        /// <summary>播放 UI 通用点击音效。</summary>
        /// <param name="soundName">音效资源名。</param>
        void PlaySfx(string soundName);
    }
}
