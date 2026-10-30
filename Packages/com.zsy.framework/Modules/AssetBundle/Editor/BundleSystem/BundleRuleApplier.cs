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
    /// 规则应用结果：供窗口与构建流程展示。
    /// </summary>
    public sealed class RuleApplyResult
    {
        /// <summary>成功分配 bundle 名的资源数。</summary>
        public int assignedCount;

        /// <summary>生成的 bundle 名 → 文件数。</summary>
        public readonly Dictionary<string, int> bundleFileCounts = new();

        /// <summary>命中的 bundle 名 → 远程标记。</summary>
        public readonly Dictionary<string, bool> bundleRemoteFlags = new();

        /// <summary>命名冲突列表（bundle 名 → 冲突文件）。</summary>
        public readonly Dictionary<string, List<string>> conflicts = new();

        /// <summary>是否可以继续构建。</summary>
        public bool Success => conflicts.Count == 0;
    }

    /// <summary>
    /// 规则应用器：把 BundleRuleAsset 翻译为 AssetImporter.assetBundleName。
    /// 职责：枚举目标文件 → 按命名模式分配 bundle 名 → 冲突检测 → 写入 importer。
    /// 全程不使用 variant（bundle 名即最终文件名，干净且与清单一一对应）。
    /// </summary>
    public static class BundleRuleApplier
    {
        // 不参与打包的扩展名（脚本/工程文件/表格源）
        private static readonly string[] BuiltinExcludeExtensions =
        {
            ".meta", ".cs", ".xlsx", ".xls", ".csv", ".json", ".txt", ".asmdef",
        };

        /// <summary>把全部规则应用到 importer。先清空旧 bundle 名保证幂等。</summary>
        /// <param name="rules">规则资产列表。</param>
        /// <param name="dryRun">只计算不写入（供预检）。</param>
        public static RuleApplyResult ApplyAll(IReadOnlyList<BundleRuleAsset> rules, bool dryRun = false)
        {
            if (!dryRun)
            {
                ClearAllBundleNames();
            }

            var result = new RuleApplyResult();
            var seenBundleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rule in rules)
            {
                ApplyOne(rule, result, seenBundleNames, dryRun);
            }

            if (!dryRun)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return result;
        }

        private static void ApplyOne(BundleRuleAsset rule, RuleApplyResult result,
            HashSet<string> seenBundleNames, bool dryRun)
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

            var files = CollectAssetFiles(root, rule);
            foreach (var assetPath in files)
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

                // 冲突检测：同名资源在不同位置分到同一 bundle 名 → 覆盖风险
                if (!result.bundleFileCounts.TryAdd(bundleName, 1))
                {
                    result.bundleFileCounts[bundleName]++;
                }

                if (!seenBundleNames.Add(bundleName))
                {
                    // 同 bundle 名出现即记录（允许同 bundle 多文件，但跨规则重名要提示）
                    if (!result.conflicts.TryGetValue(bundleName, out var list))
                    {
                        result.conflicts[bundleName] = list = new List<string>();
                    }
                    if (list.Count < 10) list.Add(assetPath);
                }

                // 远程标记解析
                var isRemote = rule.remoteRule switch
                {
                    BundleRemoteRule.AllRemote => true,
                    BundleRemoteRule.PerBundle => ov != null && ov.isRemote,
                    _ => false,
                };
                result.bundleRemoteFlags[bundleName] = isRemote;

                if (!dryRun)
                {
                    var importer = AssetImporter.GetAtPath(assetPath);
                    if (importer == null)
                    {
                        SYLog.Warning($"[BundleSystem] 无法获取 importer: {assetPath}", LogCategory.Resource);
                        continue;
                    }
                    importer.assetBundleName = bundleName;
                    importer.assetBundleVariant = null; // 新方案不使用 variant
                }
            }
        }

        /// <summary>枚举参与打包的资源文件（Assets/ 路径）。</summary>
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

                    var assetPath = file.Replace("\\", "/");
                    var relative = assetPath[(rootFull.Length + 1)..].Replace("\\", "/");
                    if (IsExcluded(relative, rule.excludePatterns)) continue;
                    result.Add(assetPath);
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

        /// <summary>清空工程内全部 bundle 名（含孤儿名）。</summary>
        public static void ClearAllBundleNames()
        {
            foreach (var name in AssetDatabase.GetAllAssetBundleNames())
            {
                AssetDatabase.RemoveAssetBundleName(name, true);
            }
        }
    }
}
