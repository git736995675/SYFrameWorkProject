using System;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace ZSYFramework.AssetBundle
{
    /// <summary>
    /// AssetBundle 资源提供者。把 <see cref="BundleAssetLoader"/>（清单驱动加载器）
    /// 适配为 <see cref="IResourceProvider"/> 的 Task 异步风格。
    /// </summary>
    public sealed class AssetBundleProvider : IResourceProvider
    {
        /// <inheritdoc />
        public string ProviderName => "AssetBundle";

        /// <inheritdoc />
        public Task<T> LoadAssetAsync<T>(string location) where T : Object
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            BundleAssetLoader.Load<T>(location, result =>
            {
                if (result != null)
                {
                    tcs.TrySetResult(result);
                }
                else
                {
                    tcs.TrySetException(new InvalidOperationException(
                        $"[AssetBundle] 资源加载失败: {typeof(T).Name} @ {location}"));
                }
            });
            return tcs.Task;
        }

        /// <inheritdoc />
        public Task<Object> LoadAssetAsync(string location, Type assetType)
        {
            // BundleAssetLoader 按 Object 类型过滤加载即可——AB 内资产名唯一，类型由调用方自行转换
            var tcs = new TaskCompletionSource<Object>(TaskCreationOptions.RunContinuationsAsynchronously);
            BundleAssetLoader.Load<Object>(location, result =>
            {
                if (result != null)
                {
                    tcs.TrySetResult(result);
                }
                else
                {
                    tcs.TrySetException(new InvalidOperationException(
                        $"[AssetBundle] 资源加载失败: {assetType.Name} @ {location}"));
                }
            });
            return tcs.Task;
        }
    }
}
