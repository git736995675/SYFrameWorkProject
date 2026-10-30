using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZSYFramework.Core;

namespace ZSYFramework.AssetBundle.Editor
{
    /// <summary>
    /// AB 打包主窗口：规则管理 → 冲突预检 → 一键构建 → 清单查看。
    /// 入口: SY-Tool/AssetBundle/AssetBundles管理。
    /// </summary>
    public sealed class BundleSystemWindow : EditorWindow
    {
        private Vector2 _scroll;
        private RuleApplyResult _lastCheck;
        private string _version = "";
        private bool _incremental;

        // 菜单路径与 SYToolMenus.AssetBundle 分组保持一致。
        // 不直接引用 SYToolMenus 类型: 它在 DevTools 程序集, 而依赖方向是 DevTools→AB.Editor, 反向引用会成环。
        private const string MenuRoot = "SY-Tool/AssetBundle";

        [MenuItem(MenuRoot + "/AssetBundles管理", false, 1)]
        public static void Open()
        {
            GetWindow<BundleSystemWindow>("AssetBundles管理");
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawRuleSection();
            EditorGUILayout.Space(8);
            DrawBuildSection();
            EditorGUILayout.Space(8);
            DrawManifestSection();

            EditorGUILayout.EndScrollView();
        }

        // ── 规则管理 ─────────────────────────────────────────────

        private void DrawRuleSection()
        {
            EditorGUILayout.LabelField("打包规则资产", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "每个参与打包的根目录一个规则资产（Create → ZSYFramework → Bundle Rule Asset）。\n" +
                "把规则资产拖入下方列表即可参与打包。命名冲突未解决时无法构建。", MessageType.Info);

            _rules = DrawRuleList(_rules);
        }

        private BundleRuleAsset[] _rules = Array.Empty<BundleRuleAsset>();

        private static BundleRuleAsset[] DrawRuleList(BundleRuleAsset[] rules)
        {
            var newList = new BundleRuleAsset[rules.Length + 1];
            rules.CopyTo(newList, 0);
            newList[^1] = null;

            for (var i = 0; i < newList.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                newList[i] = (BundleRuleAsset)EditorGUILayout.ObjectField(
                    $"  规则 {i + 1}", newList[i], typeof(BundleRuleAsset), false);
                EditorGUILayout.EndHorizontal();

                if (newList[i] != null && newList[i].TargetAssetPath == null)
                {
                    EditorGUILayout.HelpBox($"规则 {newList[i].name} 未配置有效目标目录", MessageType.Warning);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加一行", GUILayout.Width(100)))
            {
                // 长度已 +1, 直接返回
            }
            if (GUILayout.Button("移除空行", GUILayout.Width(100)))
            {
                return newList.Where(r => r != null).ToArray();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            return newList;
        }

        // ── 构建区 ───────────────────────────────────────────────

        private void DrawBuildSection()
        {
            EditorGUILayout.LabelField("构建", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _version = EditorGUILayout.TextField("版本号", string.IsNullOrEmpty(_version)
                ? DateTime.Now.ToString("yyyyMMdd_HHmm") : _version);
            if (GUILayout.Button("取当前时间", GUILayout.Width(90)))
            {
                _version = DateTime.Now.ToString("yyyyMMdd_HHmm");
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            _incremental = EditorGUILayout.Toggle(new GUIContent("增量构建",
                "开启后不做输出目录清理（依赖 Unity 哈希比对复用未变更 bundle）"), _incremental);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("① 预检冲突（不写入）"))
            {
                _lastCheck = BundleRuleApplier.ApplyAll(ActiveRules(), dryRun: true);
                ShowCheckResult();
            }
            if (GUILayout.Button("② 应用规则到资源"))
            {
                _lastCheck = BundleRuleApplier.ApplyAll(ActiveRules(), dryRun: false);
                ShowCheckResult();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!ValidVersion()))
            {
                if (GUILayout.Button("③ 构建当前平台"))
                {
                    Build(EditorUserBuildSettings.activeBuildTarget);
                }
                if (GUILayout.Button("构建 Windows"))
                {
                    Build(BuildTarget.StandaloneWindows);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"包内置输出: {BundleBuilder.StreamingOutputPath(EditorUserBuildSettings.activeBuildTarget)}",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"CDN 产物: {BundleBuilder.VersionedOutputPath(EditorUserBuildSettings.activeBuildTarget, _version)}",
                EditorStyles.miniLabel);
        }

        private void Build(BuildTarget target)
        {
            if (!ValidVersion())
            {
                EditorUtility.DisplayDialog("版本号无效", "请填写版本号（如 20261031_0240）", "确定");
                return;
            }

            if (_lastCheck == null || !_lastCheck.Success)
            {
                _lastCheck = BundleRuleApplier.ApplyAll(ActiveRules(), dryRun: true);
                if (!_lastCheck.Success)
                {
                    ShowCheckResult();
                    EditorUtility.DisplayDialog("存在冲突",
                        $"有 {_lastCheck.conflicts.Count} 个 bundle 命名冲突，请先解决（见 Console）", "确定");
                    return;
                }
            }

            var options = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode;
            if (!_incremental)
            {
                // 全量: 强制重建全部 bundle; 增量时 Unity 按资产哈希自动复用未变更 bundle
                options |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
            }

            try
            {
                EditorUtility.DisplayProgressBar("AssetBundles", "构建中…", 0.5f);
                var ok = BundleBuilder.Build(ActiveRules(), target, _version.Trim(), options);
                if (ok)
                {
                    _lastCheck = null;
                    EditorUtility.DisplayDialog("构建完成",
                        $"版本 {_version}\n输出: {BundleBuilder.StreamingOutputPath(target)}", "确定");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private BundleRuleAsset[] ActiveRules() => _rules.Where(r => r != null).ToArray();

        private bool ValidVersion() => !string.IsNullOrWhiteSpace(_version);

        private void ShowCheckResult()
        {
            if (_lastCheck == null) return;
            if (_lastCheck.Success)
            {
                var total = _lastCheck.bundleFileCounts.Count;
                var remote = _lastCheck.bundleRemoteFlags.Values.Count(r => r);
                Debug.Log($"[BundleSystem] 预检通过: {total} 个 bundle, {_lastCheck.assignedCount} 个资源, 远程 {remote} 个");
            }
            else
            {
                foreach (var kv in _lastCheck.conflicts)
                {
                    Debug.LogError($"[BundleSystem] bundle 名冲突 [{kv.Key}]:\n  " + string.Join("\n  ", kv.Value));
                }
            }
        }

        // ── 清单查看 ─────────────────────────────────────────────

        private Vector2 _manifestScroll;
        private BundleManifest _preview;

        private void DrawManifestSection()
        {
            EditorGUILayout.LabelField("清单", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("打开清单文件"))
            {
                var path = Path.GetFullPath(BundleManifestManager.HotManifestPath);
                if (!File.Exists(path))
                {
                    path = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath,
                        "Bundles", BundleManifestManager.PlatformName, "manifest.json"));
                }
                if (File.Exists(path)) EditorUtility.RevealInFinder(path);
                else EditorUtility.DisplayDialog("清单不存在", "尚未打包生成清单", "确定");
            }
            if (GUILayout.Button("打开 CDN 产物目录"))
            {
                var dir = BundleBuilder.CdnRootPath(EditorUserBuildSettings.activeBuildTarget);
                if (Directory.Exists(dir)) EditorUtility.RevealInFinder(dir);
                else EditorUtility.DisplayDialog("目录不存在", "尚未执行过构建", "确定");
            }
            if (GUILayout.Button(_preview == null ? "查看清单内容" : "刷新清单内容"))
            {
                LoadPreview();
            }
            EditorGUILayout.EndHorizontal();

            if (_preview == null) return;

            EditorGUILayout.HelpBox(
                $"版本 {_preview.version} · 平台 {_preview.platform} · " +
                $"{_preview.bundles.Count} bundles · {_preview.resources.entries.Count} 资源", MessageType.None);

            _manifestScroll = EditorGUILayout.BeginScrollView(_manifestScroll, GUILayout.Height(220));
            foreach (var b in _preview.bundles)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(b.name, GUILayout.Width(220));
                EditorGUILayout.LabelField(FormatSize(b.size), GUILayout.Width(70));
                GUILayout.Label(b.isRemote ? "远程" : "内置", GUILayout.Width(40));
                EditorGUILayout.LabelField($"依赖 {b.dependencies.Count}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void LoadPreview()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Bundles",
                BundleManifestManager.PlatformName, "manifest.json");
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("清单不存在", "尚未打包生成清单", "确定");
                return;
            }
            _preview = BundleManifestManager.Parse(File.ReadAllText(path));
        }

        private static string FormatSize(long bytes) =>
            bytes >= 1 << 20 ? $"{bytes / (float)(1 << 20):F1} MB" : $"{bytes / 1024f:F0} KB";
    }
}
