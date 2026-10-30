#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using Object = UnityEngine.Object;

namespace ZSYFramework.AssetBundle
{
    /// <summary>
    /// 编辑器模拟资源提供者。绕过 AB 打包，直接用 AssetDatabase 加载工程内资产，
    /// 开发期无需每次改动资源都重新打 AB。仅编辑器可用。
    /// </summary>
    public sealed class EditorSimulateProvider : IResourceProvider
    {
        private readonly Dictionary<string, Object> _cache = new();

        /// <inheritdoc />
        public string ProviderName => "EditorSimulate";

        /// <inheritdoc />
        public Task<T> LoadAssetAsync<T>(string location) where T : Object
        {
            var asset = LoadInternal(location, typeof(T));
            if (asset == null)
            {
                return Task.FromException<T>(new InvalidOperationException(
                    $"[EditorSimulate] 资源加载失败: {location} (类型 {typeof(T).Name})"));
            }

            return Task.FromResult((T)asset);
        }

        /// <inheritdoc />
        public Task<Object> LoadAssetAsync(string location, Type assetType)
        {
            var asset = LoadInternal(location, assetType);
            if (asset == null)
            {
                return Task.FromException<Object>(new InvalidOperationException(
                    $"[EditorSimulate] 资源加载失败: {location} (类型 {assetType.Name})"));
            }

            return Task.FromResult(asset);
        }

        private Object LoadInternal(string location, Type assetType)
        {
            if (_cache.TryGetValue(location, out var cached)) return cached;

            Object asset = null;
            // 与 AB 模式一致：先经资源清单把资源名解析为工程内路径
            ResourceUtils.Instance.GetResInfo(location, (_, fullPath) =>
            {
                asset = AssetDatabase.LoadAssetAtPath(fullPath, assetType);
            });

            _cache[location] = asset;
            return asset;
        }
    }
}
#endif
