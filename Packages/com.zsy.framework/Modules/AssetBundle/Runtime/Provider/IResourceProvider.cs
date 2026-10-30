using System;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace ZSYFramework.AssetBundle
{
    /// <summary>
    /// 资源提供者抽象。上层业务只依赖此接口加载资源，具体实现
    /// （编辑器直读 / AssetBundle / 未来的小游戏远程加载）可无缝切换。
    /// <paramref name="location"/> 为框架资源名（与资源清单 ResourcesInfoList 中的资源名一致）。
    /// </summary>
    public interface IResourceProvider
    {
        /// <summary>提供者名称，用于日志。</summary>
        string ProviderName { get; }

        /// <summary>按资源名加载资产，失败时抛出异常。</summary>
        /// <param name="location">框架资源名。</param>
        /// <typeparam name="T">资产类型。</typeparam>
        Task<T> LoadAssetAsync<T>(string location) where T : Object;

        /// <summary>按资源名加载指定类型的资产，失败时抛出异常。</summary>
        /// <param name="location">框架资源名。</param>
        /// <param name="assetType">资产类型。</param>
        Task<Object> LoadAssetAsync(string location, Type assetType);
    }
}
