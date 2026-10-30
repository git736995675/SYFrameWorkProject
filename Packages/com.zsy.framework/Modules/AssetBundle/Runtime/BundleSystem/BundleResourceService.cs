using System;
using System.Threading.Tasks;
using Object = UnityEngine.Object;
using ZSYFramework.Core;

namespace ZSYFramework.AssetBundle
{
    /// <summary>
    /// 新一代资源服务实现：走 BundleAssetLoader（清单驱动），替换旧的回调式管线。
    /// 由 AssetServiceInstaller 安装为全局 IAssetService（ServiceHub 消费）。
    /// </summary>
    public static class BundleResourceService
    {
        /// <summary>按框架资源名加载（回调风格，失败回调 null）。</summary>
        public static void Load<T>(string assetName, Action<T> onSuccess) where T : Object
        {
            BundleAssetLoader.Load(assetName, onSuccess);
        }

        /// <summary>按框架资源名加载（Task 风格，失败回调 null 而非异常——与服务契约一致）。</summary>
        public static Task<T> LoadAsync<T>(string assetName) where T : Object
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            BundleAssetLoader.Load<T>(assetName, result =>
            {
                if (result != null) tcs.TrySetResult(result);
                else tcs.TrySetException(new InvalidOperationException(
                    $"[BundleSystem] 资源加载失败: {assetName}"));
            });
            return tcs.Task;
        }
    }
}
