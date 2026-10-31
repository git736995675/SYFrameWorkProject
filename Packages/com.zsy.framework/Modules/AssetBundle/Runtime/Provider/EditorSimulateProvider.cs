#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using ZSYFramework.Core;
using Object = UnityEngine.Object;

namespace ZSYFramework.AssetBundle
{
    /// <summary>
    /// 编辑器模拟资源提供者。绕过 AB 打包，直接用 AssetDatabase 加载工程内资产，
    /// 开发期无需每次改动资源都重新打 AB。仅编辑器可用。
    /// 资源名 → 资产定位：按「文件名（不含扩展名）」在工程内搜索，与
    /// BundleManifestGenerator 写入清单的资源名规则（GetFileNameWithoutExtension）一致。
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

            var asset = FindAssetByName(location, assetType);
            if (asset == null)
            {
                SYLog.Error($"[EditorSimulate] 工程内找不到资源: {location}", LogCategory.Resource);
            }

            _cache[location] = asset;
            return asset;
        }

        /// <summary>按文件名搜索工程资产。多命中时优先 t:Object 且路径较短者（顶层资源优先）。</summary>
        private static Object FindAssetByName(string fileName, Type assetType)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            var guids = AssetDatabase.FindAssets($"\"{fileName}\"", new[] { "Assets" });
            Object best = null;
            var bestPathLength = int.MaxValue;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                if (!System.IO.Path.GetFileNameWithoutExtension(path)
                        .Equals(fileName, StringComparison.OrdinalIgnoreCase)) continue;

                var asset = AssetDatabase.LoadAssetAtPath(path, assetType)
                            ?? AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset == null) continue;

                if (path.Length < bestPathLength)
                {
                    best = asset;
                    bestPathLength = path.Length;
                }
            }

            return best;
        }
    }
}
#endif
