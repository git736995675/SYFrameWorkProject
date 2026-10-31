using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZSYFramework.Core;

namespace ZSYFramework.AssetBundle.Editor
{
    /// <summary>
    /// 打包布局计算结果：bundle 名 → 资源明细，供预览窗口 / 显式构建共享。
    /// </summary>
    public sealed class BundleLayout
    {
        /// <summary>单 bundle 明细。</summary>
        public sealed class BundleInfo
        {
            /// <summary>bundle 名（即产物文件名）。</summary>
            public string name;

            /// <summary>是否远程（CDN 热更）。</summary>
            public bool isRemote;

            /// <summary>bundle 内资源明细。</summary>
            public readonly List<string> assetPaths = new();

            /// <summary>依赖的其他 bundle 名（构建后由清单填充；预览阶段为空）。</summary>
            public readonly List<string> dependencies = new();
        }

        /// <summary>bundle 名 → 明细。</summary>
        public readonly Dictionary<string, BundleInfo> bundles = new(StringComparer.Ordinal);

        /// <summary>资源路径 → 所属 bundle 名（资源总览用）。</summary>
        public readonly Dictionary<string, string> assetToBundle = new(StringComparer.Ordinal);

        /// <summary>命名冲突列表（bundle 名 → 冲突文件）。</summary>
        public readonly Dictionary<string, List<string>> conflicts = new(StringComparer.Ordinal);

        /// <summary>参与打包的资源总数。</summary>
        public int TotalAssets => assetToBundle.Count;

        /// <summary>是否可以继续构建。</summary>
        public bool Success => conflicts.Count == 0;
    }

    /// <summary>
    /// 规则应用器：把 BundleRuleAsset 列表计算为 bundle 布局。
    /// 纯计算器，不写 AssetImporter——构建走 BuildPipeline.BuildAssetBundles 的
    /// AssetBundleBuild[] 显式重载，工程内 importer 状态保持干净。
    /// ClearBundleNames 仅用于清理旧方案遗留的脏 abname。
    /// </summary>
    public static class BundleRuleApplier
    {
        // 不参与打包的扩展名（脚本/工程文件/表格源/规则资产）
        private static readonly string[] BuiltinExcludeExtensions =
        {
            ".meta", ".cs", ".xlsx", ".xls", ".csv", ".json", ".txt", ".asmdef", ".asset",
        };

        /// <summary>
        /// 计算全部规则的 bundle 布局。只读计算，无任何工程状态副作用。
        /// </summary>
        public static BundleLayout ComputeLayout(IReadOnlyList<BundleRuleAsset> rules)
        {
            var layout = new BundleLayout();
            foreach (var rule in rules)
            {
                ComputeOne(rule, layout);
            }

            return layout;
        }

        private static void ComputeOne(BundleRuleAsset rule, BundleLayout layout)
        {
            var root = rule.TargetAssetPath;
            if (root == null)
            {
                SYLog.Error($"[BundleSystem] 规则 {rule.name} 未配置有效的目标目录", LogCategory.Resource);
                return;
            }

            var rootName = SanitizeBundleName(
                rule.namingMode == BundleNamingMode.RootDir ? rule.rootBundleName : root.Split('/').Last());
            if (string.IsNullOrEmpty(rootName))
            {
                SYLog.Error($"[BundleSystem] 规则 {rule.name} 的 bundle 名为空", LogCategory.Resource);
                return;
            }

            // 手动例外查找表：相对路径 → override
            var overrideMap = new Dictionary<string, BundleOverride>(StringComparer.OrdinalIgnoreCase);
            foreach (var ov in rule.overrides)
            {
                if (!string.IsNullOrEmpty(ov.relativePath))
                {
                    overrideMap[NormalizeRelative(ov.relativePath)] = ov;
                }
            }

            foreach (var assetPath in CollectAssetFiles(root, rule))
            {
                var relative = assetPath[(root.Length + 1)..];
                overrideMap.TryGetValue(NormalizeRelative(relative), out var ov);

                string bundleName;
                switch (rule.namingMode)
                {
                    case BundleNamingMode.Manual:
                        // Manual 模式必须有例外条目给名字；无条目跳过并提示
                        if (ov == null || string.IsNullOrWhiteSpace(ov.bundleName))
                        {
                            SYLog.Warning($"[BundleSystem] Manual 模式缺少包名: {assetPath}", LogCategory.Resource);
                            continue;
                        }
                        bundleName = SanitizeBundleName(ov.bundleName);
                        break;

                    case BundleNamingMode.RootDir:
                        bundleName = rootName;
                        break;

                    case BundleNamingMode.SubDir:
                        // 一级子目录名；根目录下的散文件归入根包
                        var firstSegment = relative.Split('/')[0];
                        bundleName = relative.Contains('/')
                            ? SanitizeBundleName(firstSegment)
                            : rootName;
                        break;

                    case BundleNamingMode.AssetName:
                        bundleName = SanitizeBundleName(Path.GetFileNameWithoutExtension(assetPath));
                        break;

                    default:
                        continue;
                }

                // 跨规则 bundle 重名检测（同规则内同 bundle 多文件是合法的）
                if (!layout.bundles.TryGetValue(bundleName, out var info))
                {
                    info = new BundleLayout.BundleInfo { name = bundleName };
                    layout.bundles[bundleName] = info;
                }

                // 登记资源（同路径防御性去重）
                if (!layout.assetToBundle.ContainsKey(assetPath))
                {
                    layout.assetToBundle[assetPath] = bundleName;
                    info.assetPaths.Add(assetPath);
                    info.isRemote = rule.remoteRule switch
                    {
                        BundleRemoteRule.AllRemote => true,
                        BundleRemoteRule.PerBundle => ov != null && ov.isRemote,
                        _ => false,
                    };
                }
                else if (layout.assetToBundle[assetPath] != bundleName)
                {
                    // 同一资源被多条规则算入不同 bundle → 冲突
                    if (!layout.conflicts.TryGetValue(bundleName, out var list))
                    {
                        layout.conflicts[bundleName] = list = new List<string>();
                    }
                    if (list.Count < 10) list.Add(assetPath);
                }
            }
        }

        /// <summary>把布局转为显式构建参数（按名排序保证产物确定性）。</summary>
        public static AssetBundleBuild[] ToAssetBundleBuilds(BundleLayout layout)
        {
            return layout.bundles.Values
                .OrderBy(b => b.name, StringComparer.Ordinal)
                .Select(b => new AssetBundleBuild
                {
                    assetBundleName = b.name,
                    assetNames = b.assetPaths.OrderBy(p => p, StringComparer.Ordinal).ToArray(),
                })
                .ToArray();
        }

        /// <summary>枚举参与打包的资源文件（返回 Assets/ 工程相对路径，AssetBundleBuild.assetNames 只接受这种格式）。</summary>
        private static List<string> CollectAssetFiles(string root, BundleRuleAsset rule)
        {
            var result = new List<string>();
            var rootFull = Path.GetFullPath(root);

            void Walk(string dir)
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (Array.IndexOf(BuiltinExcludeExtensions, ext) >= 0) continue;
                    if (file.EndsWith("~")) continue; // Unity 隐藏目录语义（文件级）

                    var full = file.Replace("\\", "/");
                    var relative = full[(rootFull.Length + 1)..];
                    if (IsExcluded(relative, rule.excludePatterns)) continue;
                    result.Add(root + "/" + relative);
                }

                foreach (var sub in Directory.GetDirectories(dir))
                {
                    if (sub.Replace("\\", "/").EndsWith("~")) continue; // 隐形目录跳过
                    Walk(sub);
                }
            }

            Walk(rootFull);
            return result;
        }

        /// <summary>通配排除：支持 * 任意字符匹配，模式按相对路径比较。</summary>
        private static bool IsExcluded(string relativePath, List<string> patterns)
        {
            foreach (var raw in patterns)
            {
                var pattern = NormalizeRelative(raw);
                if (string.IsNullOrEmpty(pattern)) continue;

                // 纯目录模式（无通配符且无扩展名）→ 匹配其下所有文件
                var glob = pattern.Contains('*') ? pattern : pattern.TrimEnd('/') + "/*";
                if (PathMatcher(glob, relativePath)) return true;
            }

            return false;
        }

        private static bool PathMatcher(string glob, string path)
        {
            // 把通配符转成正则；'*' 不跨越目录边界，'**' 跨越
            var regex = "^" + System.Text.RegularExpressions.Regex.Escape(glob)
                .Replace(@"\*\*/", ".*")
                .Replace(@"\*\*", ".*")
                .Replace(@"\*", @"[^/]*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(path, regex,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        /// <summary>bundle 名规范化：小写、空格转下划线、去非法字符。</summary>
        public static string SanitizeBundleName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var sb = new System.Text.StringBuilder(name.Length);
            foreach (var c in name.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c) || c is '_' or '-' or '.') sb.Append(c);
                else sb.Append('_');
            }

            return sb.ToString();
        }

        private static string NormalizeRelative(string p) =>
            p?.Trim().Replace("\\", "/").TrimStart('/').ToLowerInvariant() ?? "";

        /// <summary>
        /// 清空工程内全部 bundle 名。显式构建方案下 importer 不再被写入，
        /// 此方法仅用于清理旧方案遗留的脏 abname（窗口里手动触发一次即可）。
        /// </summary>
        public static void ClearBundleNames()
        {
            foreach (var name in AssetDatabase.GetAllAssetBundleNames())
            {
                AssetDatabase.RemoveAssetBundleName(name, true);
            }
        }
    }
}
