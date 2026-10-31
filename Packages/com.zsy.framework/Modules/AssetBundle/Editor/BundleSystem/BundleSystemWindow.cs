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
    /// AB 打包主窗口：资源信息中心。
    /// 结构：构建操作区（一行紧凑）→ 打包规则列表（只读+定位）→ Tab 明细
    /// （资源总览 / Bundle 视图 / 依赖视图 / 构建产物清单）。
    /// 规则来源于文件夹 Inspector 配置的 BundleRuleAsset，此处只读展示，
    /// 编辑一律跳回文件夹 Inspector（单一数据源，杜绝两处配置打架）。
    /// </summary>
    public sealed class BundleSystemWindow : EditorWindow
    {
        // 菜单路径与 SYToolMenus.AssetBundle 分组保持一致。
        // 不直接引用 SYToolMenus 类型: 它在 DevTools 程序集, 而依赖方向是 DevTools→AB.Editor, 反向引用会成环。
        private const string MenuRoot = "SY-Tool/AssetBundle";

        private BundleRuleAsset[] _rules = Array.Empty<BundleRuleAsset>();
        private BundleLayout _layout;
        private int _tab;
        private Vector2 _scroll;
        private Vector2 _detailScroll;
        private string _selectedBundle;

        private static readonly string[] TabNames = { "资源总览", "Bundle 视图", "依赖视图", "产物清单" };

        [MenuItem(MenuRoot + "/AssetBundles管理", false, 1)]
        public static void Open()
        {
            GetWindow<BundleSystemWindow>("AssetBundles管理");
        }

        private void OnEnable()
        {
            RefreshRules();
        }

        private void RefreshRules()
        {
            _rules = CollectAllRules();
            _layout = null; // 规则变了, 旧布局失效
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(4);

            if (_rules.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "工程内没有打包规则。\n请在 Project 窗口选中 Assets/ 下的文件夹，" +
                    "在 Inspector 的「AssetBundle 打包设置」里点「启用 AB 打包」。", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawRulesSection();

            if (_layout == null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox("点击「刷新预览」计算资源划分结果。", MessageType.None);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (!_layout.Success)
            {
                EditorGUILayout.Space(6);
                DrawConflicts();
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.Space(6);
            DrawTabBar();
            EditorGUILayout.Space(4);
            DrawTabContent();
            EditorGUILayout.EndScrollView();
        }

        // ── 顶部操作区 ───────────────────────────────────────────

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("刷新规则", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshRules();
            }
            if (GUILayout.Button("刷新预览", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                _layout = BundleRuleApplier.ComputeLayout(_rules);
            }

            GUILayout.FlexibleSpace();

            // 构建统一走 AB打包工具（平台选择/版本号/增量在那里配），此处只保留清理入口
            if (GUILayout.Button("AB打包工具", EditorStyles.toolbarButton, GUILayout.Width(84)))
            {
                BundleBuildWindow.Open();
            }
            if (GUILayout.Button("清理历史 abname", EditorStyles.toolbarButton, GUILayout.Width(104)))
            {
                BundleRuleApplier.ClearBundleNames();
                Debug.Log("[BundleSystem] 已清空工程内全部 AssetBundle 名（旧方案遗留）");
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── 规则列表（只读） ─────────────────────────────────────

        private void DrawRulesSection()
        {
            EditorGUILayout.LabelField($"打包规则（{_rules.Length}）", EditorStyles.boldLabel);

            foreach (var rule in _rules)
            {
                EditorGUILayout.BeginHorizontal();

                // 只读展示: 目标目录 + 命名模式 + 远程策略, 点击定位目录
                var dir = rule.TargetAssetPath;
                var naming = rule.namingMode switch
                {
                    BundleNamingMode.Manual => "手动",
                    BundleNamingMode.RootDir => "根目录名",
                    BundleNamingMode.SubDir => "子目录名",
                    BundleNamingMode.AssetName => "资源名",
                    _ => "?",
                };
                var remote = rule.remoteRule switch
                {
                    BundleRemoteRule.AllLocal => "内置",
                    BundleRemoteRule.AllRemote => "远程",
                    BundleRemoteRule.PerBundle => "按例外",
                    _ => "?",
                };

                if (GUILayout.Button(dir ?? $"{rule.name}（无效目录）", EditorStyles.linkLabel))
                {
                    EditorGUIUtility.PingObject(rule.targetDirectory != null
                        ? rule.targetDirectory
                        : (UnityEngine.Object)rule);
                }
                GUILayout.Label(naming, EditorStyles.miniLabel, GUILayout.Width(56));
                GUILayout.Label(remote, EditorStyles.miniLabel, GUILayout.Width(44));
                GUILayout.Label($"排除 {rule.excludePatterns.Count}", EditorStyles.miniLabel, GUILayout.Width(58));
                GUILayout.Label($"例外 {rule.overrides.Count}", EditorStyles.miniLabel, GUILayout.Width(52));

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawConflicts()
        {
            EditorGUILayout.HelpBox($"存在 {_layout.conflicts.Count} 个 bundle 命名冲突，无法构建：",
                MessageType.Error);
            foreach (var kv in _layout.conflicts)
            {
                EditorGUILayout.LabelField($"· {kv.Key}", EditorStyles.miniLabel);
                foreach (var f in kv.Value)
                {
                    EditorGUILayout.LabelField($"    {f}", EditorStyles.miniLabel);
                }
            }
        }

        private void DrawTabBar()
        {
            _tab = GUILayout.Toolbar(_tab, TabNames);
        }

        // ── Tab 内容 ─────────────────────────────────────────────

        private void DrawTabContent()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.Height(360));

            switch (_tab)
            {
                case 0: DrawAssetOverview(); break;
                case 1: DrawBundleView(); break;
                case 2: DrawDependencyView(); break;
                case 3: DrawBuildOutputView(); break;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>资源总览：全部待打包资源 → 所属 bundle。</summary>
        private void DrawAssetOverview()
        {
            EditorGUILayout.LabelField($"待打包资源 {_layout.TotalAssets} 个 / " +
                $"{_layout.bundles.Count} 个 bundle", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            foreach (var b in _layout.bundles.Values.OrderBy(b => b.name, StringComparer.Ordinal))
            {
                EditorGUILayout.LabelField($"[{b.name}]{(b.isRemote ? "  远程" : "")}",
                    EditorStyles.boldLabel);
                foreach (var asset in b.assetPaths)
                {
                    if (GUILayout.Button(asset, EditorStyles.linkLabel))
                    {
                        EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(asset));
                    }
                }
                EditorGUILayout.Space(3);
            }
        }

        /// <summary>Bundle 视图：左列 bundle 列表，右列选中包的资源清单。</summary>
        private void DrawBundleView()
        {
            EditorGUILayout.LabelField($"共 {_layout.bundles.Count} 个 bundle", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(230));
            foreach (var b in _layout.bundles.Values.OrderBy(b => b.name, StringComparer.Ordinal))
            {
                var title = $"{b.name} ({b.assetPaths.Count})";
                if (GUILayout.Toggle(_selectedBundle == b.name, title, "Button"))
                {
                    _selectedBundle = b.name;
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            if (_selectedBundle != null &&
                _layout.bundles.TryGetValue(_selectedBundle, out var info))
            {
                EditorGUILayout.LabelField(info.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    info.isRemote ? "远程（CDN 热更）" : "内置（StreamingAssets）",
                    EditorStyles.miniLabel);
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField($"资源清单（{info.assetPaths.Count}）", EditorStyles.boldLabel);
                foreach (var asset in info.assetPaths)
                {
                    if (GUILayout.Button(Path.GetFileName(asset), EditorStyles.linkLabel))
                    {
                        EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(asset));
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("选择左侧 bundle 查看资源清单。", MessageType.None);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>依赖视图：构建产物存在时展示 bundle 间依赖；否则提示先构建。</summary>
        private void DrawDependencyView()
        {
            var outPath = BundleBuilder.StreamingOutputPath(EditorUserBuildSettings.activeBuildTarget);
            if (!Directory.Exists(outPath) || !Directory.GetFiles(outPath, "*.manifest").Any())
            {
                EditorGUILayout.HelpBox("尚无构建产物，依赖关系由 Unity 在构建时计算。\n" +
                    "先执行「构建当前平台」，之后此处展示每个 bundle 的依赖列表。", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField($"构建产物依赖（{EditorUserBuildSettings.activeBuildTarget}）",
                EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            foreach (var bundleFile in Directory.GetFiles(outPath, "*.manifest"))
            {
                var bundleName = Path.GetFileNameWithoutExtension(bundleFile);
                if (bundleName.StartsWith("Bundles")) continue; // 平台主清单无意义

                var deps = File.ReadAllLines(bundleFile)
                    .Where(l => l.StartsWith("- "))
                    .Select(l => Path.GetFileNameWithoutExtension(l[2..].Trim()))
                    .Where(d => !d.StartsWith("Bundles"))
                    .Distinct()
                    .ToList();

                EditorGUILayout.LabelField($"{bundleName}  →  {(deps.Count == 0 ? "（无依赖）" : "")}",
                    EditorStyles.miniLabel);
                foreach (var d in deps)
                {
                    EditorGUILayout.LabelField($"    · {d}", EditorStyles.miniLabel);
                }
            }
        }

        /// <summary>产物清单：最近一次构建的 manifest.json 内容。</summary>
        private void DrawBuildOutputView()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Bundles",
                BundleManifestManager.PlatformName, "manifest.json");
            if (!File.Exists(path))
            {
                EditorGUILayout.HelpBox("尚未构建，无产物清单。", MessageType.None);
                return;
            }

            var manifest = BundleManifestManager.Parse(File.ReadAllText(path));
            var remote = manifest.bundles.Count(b => b.isRemote);
            EditorGUILayout.LabelField(
                $"版本 {manifest.version} · {manifest.bundles.Count} bundles（远程 {remote}）· " +
                $"{manifest.resources.entries.Count} 资源", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            foreach (var b in manifest.bundles)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(b.name, GUILayout.Width(200));
                EditorGUILayout.LabelField(FormatSize(b.size), GUILayout.Width(70));
                GUILayout.Label(b.isRemote ? "远程" : "内置", GUILayout.Width(40));
                EditorGUILayout.LabelField($"依赖 {b.dependencies.Count}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        // ── 构建 ─────────────────────────────────────────────────
        // 构建执行统一收敛在 BundleBuildWindow（平台选择/版本号/增量），
        // 本窗口只做资源信息展示；构建完成后回到「产物清单」Tab 查看结果。

        /// <summary>扫描工程内全部规则资产（文件夹 Inspector 生成 + 手动创建）。
        /// 公开给 BundleBuildWindow 复用，保证两个窗口的规则来源一致。</summary>
        public static BundleRuleAsset[] CollectAllRulesPublic() => CollectAllRules();

        /// <summary>扫描工程内全部规则资产（文件夹 Inspector 生成 + 手动创建）。</summary>
        private static BundleRuleAsset[] CollectAllRules()
        {
            return AssetDatabase.FindAssets("t:BundleRuleAsset")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<BundleRuleAsset>(path))
                .Where(r => r != null && r.TargetAssetPath != null)
                .OrderBy(r => r.TargetAssetPath, StringComparer.Ordinal)
                .ToArray();
        }

        private static string FormatSize(long bytes) =>
            bytes >= 1 << 20 ? $"{bytes / (float)(1 << 20):F1} MB" : $"{bytes / 1024f:F0} KB";
    }
}
