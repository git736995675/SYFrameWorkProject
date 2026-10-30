using System;
using System.Threading.Tasks;
using Object = UnityEngine.Object;
using UnityEngine;
using ZSYFramework.Core;

namespace ZSYFramework.AssetBundle
{
    /// <summary>
    /// 资源服务注入器：把 AssetBundle 管线适配为 Core 的 <see cref="IAssetService"/> 与
    /// <see cref="IConfigTextLoader"/> 契约，并注册进 <see cref="ServiceHub"/>。
    /// 资源管线就绪后由 GameRoot 启动流程调用 <see cref="Install"/>；
    /// 安装前消费方调用会收到日志兜底（不崩，模块可独立调试）。
    /// </summary>
    public static class AssetServiceInstaller
    {
        private static bool _installed;

        // 进 Play 模式前自动注册（BeforeSplashScreen 阶段），任何消费方都不需要知道安装时机。
        // 编辑器/运行时均生效；未安装时 ServiceHub 也有日志兜底。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void AutoInstall()
        {
            Install();
        }

        /// <summary>把 AB 管线注册为全局资源/配置服务。重复调用幂等。</summary>
        public static void Install()
        {
            if (_installed) return;
            _installed = true;
            ServiceHub.Register(new AssetService());
            ServiceHub.Register(new ConfigTextLoader());
        }

        private sealed class AssetService : IAssetService
        {
            public void Load<T>(string assetName, Action<T> onSuccess) where T : Object
            {
                ABLoadManager.Instance.Load(assetName, onSuccess);
            }

            public Task<T> LoadAsync<T>(string assetName) where T : Object
            {
                return ResourceProvider.LoadAssetAsync<T>(assetName);
            }
        }

        private sealed class ConfigTextLoader : IConfigTextLoader
        {
            public void LoadConfig<T>(string assetName, Action<T> onSuccess)
            {
                ABLoadManager.Instance.LoadTextAsset(assetName, json =>
                {
                    onSuccess?.Invoke(LitJson.JsonMapper.ToObject<T>(json.text));
                });
            }

            public void LoadConfigRaw(string bundleName, string assetName, Action<Object> onSuccess)
            {
                ABLoadManager.Instance.Load<TextAsset>(bundleName, assetName, json =>
                {
                    onSuccess?.Invoke(json);
                });
            }
        }
    }
}
