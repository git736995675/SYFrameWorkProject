using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZSYFramework.Core;

namespace ZSYFramework.AssetBundle.Editor
{
    /// <summary>
    /// 清单生成器：扫描构建产物 + AssetDatabase 生成 manifest.json。
    /// 清单结构（BundleManifest，运行时同构）：
    /// - version: 版本号
    /// - bundles[]: bundle 名 / md5 / 大小 / 依赖 / 是否远程
    /// - resources{}: 资源名 → bundle 名（运行时按资源名定位包）
    /// version.json: bundle 名 → md5 扁平索引（与 HotUpdate.VersionUpdater 对齐）。
    /// </summary>
    public static class BundleManifestGenerator
    {
        /// <summary>扫描构建产物生成清单。</summary>
        /// <param name="outputPath">bundle 输出目录（Assets/StreamingAssets/Bundles/&lt;平台&gt;）。</param>
        /// <param name="layout">打包布局（显式构建的计算结果，提供资源映射与远程标记）。</param>
        /// <param name="version">版本号。</param>
        /// <param name="target">目标平台。</param>
        public static BundleManifest Generate(string outputPath, BundleLayout layout,
            string version, BuildTarget target)
        {
            var manifest = new BundleManifest
            {
                version = version,
                platform = target.ToString(),
            };

            var bundleFilePaths = Directory.GetFiles(outputPath)
                .Where(f => !f.EndsWith(".manifest") && !Path.GetFileName(f).StartsWith("Bundles"))
                .ToList();

            foreach (var file in bundleFilePaths)
            {
                var bundleName = Path.GetFileName(file);

                // Unity 生成的平台主清单（Bundles.<平台> 文件）已过滤；其余每个文件视为一个 bundle
                var layoutInfo = layout != null && layout.bundles.TryGetValue(bundleName, out var li) ? li : null;
                var info = new BundleFileInfo
                {
                    name = bundleName,
                    size = new FileInfo(file).Length,
                    md5 = ComputeMd5(file),
                    isRemote = layoutInfo?.isRemote ?? false,
                };

                // 从 .manifest 文件读取显式依赖（Unity 会为每个 bundle 生成同名 .manifest）
                var manifestFile = file + ".manifest";
                if (File.Exists(manifestFile))
                {
                    info.dependencies = File.ReadAllLines(manifestFile)
                        .Where(l => l.StartsWith("- "))
                        .Select(l => Path.GetFileNameWithoutExtension(l[2..].Trim()))
                        .Distinct()
                        .ToList();
                }
                else
                {
                    info.dependencies = new List<string>();
                }

                manifest.bundles.Add(info);
            }

            // 资源名 → bundle 映射：直接取自布局计算结果（不再扫描 importer）
            if (layout != null)
            {
                foreach (var kv in layout.assetToBundle)
                {
                    var resName = Path.GetFileNameWithoutExtension(kv.Key);
                    if (manifest.resources.ContainsKey(resName))
                    {
                        SYLog.Warning($"[BundleSystem] 清单资源重名被覆盖: {resName} ({kv.Key})",
                            LogCategory.Resource);
                    }
                    manifest.resources[resName] = kv.Value;
                }
            }

            // 平台主清单（Bundles.<平台>）也注册为资源，便于工具链按资源名访问 Unity 原生 manifest
            var platformManifestFile = bundleFilePaths.FirstOrDefault(f =>
                Path.GetFileName(f) == $"Bundles.{target}");
            if (platformManifestFile != null)
            {
                manifest.resources[$"Bundles.{target}"] = $"Bundles.{target}";
            }

            return manifest;
        }

        /// <summary>写清单 JSON（缩进格式，便于 diff 与人读）。</summary>
        public static void Write(BundleManifest manifest, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, JsonUtility.ToJson(manifest, true), System.Text.Encoding.UTF8);
        }

        /// <summary>写 version.json 扁平索引（结构与 HotUpdate.VersionManifest 对齐，但不依赖其类型——依赖方向必须保持 HotUpdate→AssetBundle 单向）。</summary>
        public static void WriteVersionIndex(BundleManifest manifest, string path)
        {
            // 与 HotUpdate.VersionManifest 同构: { version, files: [{name, md5, size}] }
            var sb = new System.Text.StringBuilder("{\"version\":");
            sb.Append(LitJson.JsonMapper.ToJson(manifest.version));
            sb.Append(",\"files\":[");
            for (var i = 0; i < manifest.bundles.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var b = manifest.bundles[i];
                sb.Append("{\"name\":").Append(LitJson.JsonMapper.ToJson(b.name));
                sb.Append(",\"md5\":").Append(LitJson.JsonMapper.ToJson(b.md5));
                sb.Append(",\"size\":").Append(b.size).Append('}');
            }
            sb.Append("]}");

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
        }

        private static string ComputeMd5(string path)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            using var stream = File.OpenRead(path);
            var hash = md5.ComputeHash(stream);
            var sb = new System.Text.StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
