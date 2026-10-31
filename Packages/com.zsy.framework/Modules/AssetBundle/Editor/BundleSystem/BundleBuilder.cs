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
    /// Bundle 构建流水线：应用规则 → BuildPipeline 构建 → 拷贝完整产物到版本化输出目录。
    /// 输出位置：
    /// - 包内置: Assets/StreamingAssets/Bundles/&lt;平台&gt;/        （随包分发, 运行时直接读）
    /// - 完整产物: &lt;工程根&gt;/Bundles/&lt;平台&gt;/&lt;版本号&gt;/           （整个目录上传 CDN）
    /// manifest.json 始终散文件存放（不进 bundle），便于热更比对。
    /// </summary>
    public static class BundleBuilder
    {
        /// <summary>StreamingAssets 下的 bundle 输出目录（包内置）。</summary>
        public static string StreamingOutputPath(BuildTarget target) =>
            $"Assets/StreamingAssets/Bundles/{target}";

        /// <summary>工程根下版本化完整产物目录（CDN 上传源）。</summary>
        public static string VersionedOutputPath(BuildTarget target, string version) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Bundles", target.ToString(), version));

        /// <summary>从版本化目录回溯 CDN 根（Bundles/&lt;平台&gt;/），VersionUpdater 清单 URL 基准。</summary>
        public static string CdnRootPath(BuildTarget target) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Bundles", target.ToString()));

        /// <summary>
        /// 执行完整构建。冲突未解决时中止并返回 false。
        /// 显式构建：布局计算 → AssetBundleBuild[] 直接传入 BuildPipeline，
        /// 不写 AssetImporter，工程内 bundle 名状态保持干净。
        /// </summary>
        /// <param name="rules">参与打包的规则资产。</param>
        /// <param name="target">目标平台。</param>
        /// <param name="version">资源版本号（写入清单；如日期或自增）。</param>
        /// <param name="layout">外部已算好的布局（可空；为空时内部计算）。</param>
        /// <param name="options">构建选项（增量时由调用方去掉 ForceRebuild 选项自行控制）。</param>
        public static bool Build(IReadOnlyList<BundleRuleAsset> rules, BuildTarget target,
            string version, BundleLayout layout = null,
            BuildAssetBundleOptions options =
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode)
        {
            if (rules == null || rules.Count == 0)
            {
                SYLog.Error("[BundleSystem] 没有任何打包规则", LogCategory.Resource);
                return false;
            }

            // 1. 计算布局并预检冲突（外部传入则复用，保证窗口预览与实际构建一致）
            layout ??= BundleRuleApplier.ComputeLayout(rules);
            if (!layout.Success)
            {
                var detail = string.Join("\n", layout.conflicts
                    .SelectMany(kv => kv.Value.Select(f => $"  {kv.Key}: {f}")));
                SYLog.Error($"[BundleSystem] 存在 bundle 命名冲突, 已中止构建:\n{detail}", LogCategory.Resource);
                return false;
            }

            // 2. 清空输出目录
            var outPath = StreamingOutputPath(target);
            if (Directory.Exists(outPath)) Directory.Delete(outPath, true);
            Directory.CreateDirectory(outPath);

            // 3. 显式构建：bundle 名与资源列表来自布局，importer 无需设置 abname
            var builds = BundleRuleApplier.ToAssetBundleBuilds(layout);
            var build = BuildPipeline.BuildAssetBundles(outPath, builds, options, target);
            if (build == null)
            {
                SYLog.Error("[BundleSystem] BuildAssetBundles 失败", LogCategory.Resource);
                return false;
            }

            // 4. 生成清单
            var manifest = BundleManifestGenerator.Generate(outPath, layout, version, target);
            BundleManifestGenerator.Write(manifest, Path.Combine(outPath, "manifest.json"));
            BundleManifestGenerator.WriteVersionIndex(manifest, Path.Combine(outPath, "version.json"));

            // 5. 拷贝完整产物到版本化目录
            var versionedPath = VersionedOutputPath(target, version);
            if (Directory.Exists(versionedPath)) Directory.Delete(versionedPath, true);
            CopyDirectory(outPath, versionedPath);

            var remoteCount = layout.bundles.Values.Count(b => b.isRemote);
            SYLog.Info($"[BundleSystem] 构建完成: {layout.bundles.Count} 个 bundle" +
                       $"（远程 {remoteCount}），{layout.TotalAssets} 个资源，清单版本 {version}\n" +
                       $"  包内置: {outPath}\n  CDN 产物: {versionedPath}", LogCategory.Resource);
            return true;
        }

        private static void CopyDirectory(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var file in Directory.GetFiles(src))
            {
                File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), true);
            }

            foreach (var dir in Directory.GetDirectories(src))
            {
                CopyDirectory(dir, Path.Combine(dst, Path.GetFileName(dir)));
            }
        }
    }
}
