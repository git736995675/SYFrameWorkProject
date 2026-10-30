using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using ZSYFramework.Core;

namespace ZSYFramework.AssetBundle
{
    /// <summary>
    /// 清单管理器：负责 manifest.json 的加载与查询。
    /// 读取优先级：热更目录（persistentDataPath/HotRes）→ 包内置（StreamingAssets/Bundles/平台）。
    /// 提供资源名 → bundle 名 → 本地路径的完整定位链。
    /// </summary>
    public static class BundleManifestManager
    {
        private static BundleManifest _manifest;

        /// <summary>当前生效的清单。未加载时返回 null（先调 <see cref="LoadAsync"/>）。</summary>
        public static BundleManifest Current => _manifest;

        /// <summary>热更目录下的清单路径（VersionUpdater 更新后写入）。</summary>
        public static string HotManifestPath =>
            Path.Combine(ResourceUtils.HotResDir, "manifest.json");

        /// <summary>包内置清单的 URI。</summary>
        public static string BuiltinManifestUri =>
            "file://" + Path.Combine(Application.streamingAssetsPath, "Bundles", PlatformName, "manifest.json")
                .Replace("\\", "/");

        /// <summary>当前平台名（与构建输出目录一致）。</summary>
        public static string PlatformName
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return "Android";
#elif UNITY_IOS && !UNITY_EDITOR
                return "iOS";
#elif UNITY_WEBGL && !UNITY_EDITOR
                return "WebGL";
#else
                return "StandaloneWindows";
#endif
            }
        }

        /// <summary>
        /// 加载清单。优先热更目录，失败回落包内置。幂等（已加载直接返回）。
        /// </summary>
        public static async Task<BundleManifest> LoadAsync(bool forceReload = false)
        {
            if (_manifest != null && !forceReload) return _manifest;

            // 1. 热更清单（本地文件，肯定可读）
            if (File.Exists(HotManifestPath))
            {
                try
                {
                    _manifest = Parse(File.ReadAllText(HotManifestPath));
                    SYLog.Info($"[BundleSystem] 已加载热更清单 (版本 {_manifest.version})",
                        LogCategory.Resource);
                    return _manifest;
                }
                catch (Exception e)
                {
                    SYLog.Error($"[BundleSystem] 热更清单损坏, 回落内置清单: {e.Message}", LogCategory.Resource);
                }
            }

            // 2. 包内置清单（Android 上 StreamingAssets 在 apk 内必须走 UnityWebRequest）
            var json = await ReadBuiltinManifestAsync();
            if (json == null)
            {
                SYLog.Error("[BundleSystem] 内置清单不存在——先在 SY-Tool/AssetBundle 执行打包",
                    LogCategory.Resource);
                return null;
            }

            _manifest = Parse(json);
            SYLog.Info($"[BundleSystem] 已加载内置清单 (版本 {_manifest.version})", LogCategory.Resource);
            return _manifest;
        }

        /// <summary>解析清单 JSON。优先 JsonUtility（与生成端同构），异常时返回 null。</summary>
        public static BundleManifest Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            var manifest = JsonUtility.FromJson<BundleManifest>(json);
            return manifest != null && manifest.bundles != null ? manifest : null;
        }

        /// <summary>解析资源在本地的 bundle 文件完整路径。</summary>
        /// <param name="resourceName">框架资源名。</param>
        /// <returns>bundle 本地路径；资源或 bundle 不存在时返回 null。</returns>
        public static string ResolveBundlePath(string resourceName)
        {
            if (_manifest == null) return null;
            var bundleName = _manifest.GetBundleOf(resourceName);
            if (bundleName == null) return null;

            // 热更目录优先
            var hotPath = Path.Combine(ResourceUtils.HotResDir, bundleName);
            if (File.Exists(hotPath)) return hotPath;

            return Path.Combine(Application.streamingAssetsPath, "Bundles", PlatformName, bundleName)
                .Replace("\\", "/");
        }

        /// <summary>bundle 名对应的本地路径（不含资源名解析）。</summary>
        public static string BundlePath(string bundleName)
        {
            var hotPath = Path.Combine(ResourceUtils.HotResDir, bundleName);
            if (File.Exists(hotPath)) return hotPath;
            return Path.Combine(Application.streamingAssetsPath, "Bundles", PlatformName, bundleName)
                .Replace("\\", "/");
        }

        private static async Task<string> ReadBuiltinManifestAsync()
        {
            var uri = Path.Combine(Application.streamingAssetsPath, "Bundles", PlatformName, "manifest.json")
                .Replace("\\", "/");
            if (!uri.Contains("://")) uri = "file://" + uri;

            using var request = UnityWebRequest.Get(uri);
            request.timeout = 15;
            var op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            return request.result == UnityWebRequest.Result.Success
                ? request.downloadHandler.text
                : null;
        }
    }
}
