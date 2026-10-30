using System;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace ZSYFramework.AssetBundle
{
    /// <summary>
    /// AssetBundle 资源提供者。把既有 <see cref="ABLoadManager"/> 的回调式加载
    /// 适配为 <see cref="IResourceProvider"/> 的 Task 异步风格。
    /// </summary>
    public sealed class AssetBundleProvider : IResourceProvider
    {
        // ABLoadManager 加载失败时不回调（仅打日志），用超时兜底避免 await 永久挂起
        private const int LoadTimeoutMilliseconds = 30_000;

        /// <inheritdoc />
        public string ProviderName => "AssetBundle";

        /// <inheritdoc />
        public Task<T> LoadAssetAsync<T>(string location) where T : Object
        {
            return CallbackToTask<T>(callback => ABLoadManager.Instance.Load(location, callback), location);
        }

        /// <inheritdoc />
        public Task<Object> LoadAssetAsync(string location, Type assetType)
        {
            return CallbackToTask<Object>(callback =>
                ABLoadManager.Instance.Load<Object>(location, asset => callback(asset)), location);
        }

        private static Task<T> CallbackToTask<T>(Action<Action<T>> invoke, string location) where T : Object
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            invoke(result =>
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

            WatchTimeout<T>(tcs, location);
            return tcs.Task;
        }

        private static async void WatchTimeout<T>(TaskCompletionSource<T> tcs, string location) where T : Object
        {
            await Task.Delay(LoadTimeoutMilliseconds);
            tcs.TrySetException(new TimeoutException(
                $"[AssetBundle] 资源加载超时({LoadTimeoutMilliseconds / 1000}s): {location}"));
        }
    }
}
