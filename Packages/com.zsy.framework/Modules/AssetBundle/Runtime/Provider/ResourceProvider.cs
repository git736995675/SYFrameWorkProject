using System;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace ZSYFramework.AssetBundle
{
    /// <summary>
    /// 资源加载统一门面。业务代码只调用 <see cref="ResourceProvider.LoadAssetAsync{T}"/>，
    /// 具体由哪个 <see cref="IResourceProvider"/> 完成加载（编辑器直读 / AB / 远程）对业务透明。
    /// 默认按编辑器资源加载模式（<see cref="ABLoadManager.LoadModeSetting"/>）自动选择实现。
    /// </summary>
    public static class ResourceProvider
    {
        private static IResourceProvider _current;

        /// <summary>当前资源提供者。未手动设置时按编辑器资源加载模式自动选择。</summary>
        public static IResourceProvider Current => _current ??= CreateDefault();

        /// <summary>替换资源提供者（如小游戏端注入远程加载实现）。</summary>
        public static void SetProvider(IResourceProvider provider)
        {
            _current = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>按资源名加载资产，失败时抛出异常。</summary>
        /// <param name="location">框架资源名（与资源清单中的资源名一致）。</param>
        /// <typeparam name="T">资产类型。</typeparam>
        public static Task<T> LoadAssetAsync<T>(string location) where T : Object
        {
            return Current.LoadAssetAsync<T>(location);
        }

        /// <summary>按资源名加载指定类型的资产，失败时抛出异常。</summary>
        /// <param name="location">框架资源名。</param>
        /// <param name="assetType">资产类型。</param>
        public static Task<Object> LoadAssetAsync(string location, Type assetType)
        {
            return Current.LoadAssetAsync(location, assetType);
        }

        private static IResourceProvider CreateDefault()
        {
#if UNITY_EDITOR
            if (ABLoadManager.LoadModeSetting == LoadMode.Editor)
            {
                return new EditorSimulateProvider();
            }
#endif
            return new AssetBundleProvider();
        }
    }
}
