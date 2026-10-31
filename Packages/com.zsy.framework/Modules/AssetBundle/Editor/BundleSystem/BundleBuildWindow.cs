using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ZSYFramework.AssetBundle.Editor
{
    /// <summary>
    /// AB 打包工具：独立于资源信息窗口（BundleSystemWindow）的批量构建入口。
    /// 职责单一——选平台（支持多选顺序构建）、填版本、增量/全量、执行构建、直达产物目录。
    /// 规则、预览、明细等资源信息查看仍走 BundleSystemWindow（资源总览/Bundle/依赖/清单）。
    /// </summary>
    public sealed class BundleBuildWindow : EditorWindow
    {
        // 菜单路径与 SYToolMenus.AssetBundle 分组保持一致（DevTools 程序集依赖方向限制，字符串契约）。
        private const string MenuPath = "SY-Tool/AssetBundle/AB打包工具";

        /// <summary>可勾选的构建平台（按项目目标定；iOS 构建需 macOS，此处仍列出供 CI 参考）。</summary>
        private static readonly (BuildTarget target, string label)[] Platforms =
        {
            (BuildTarget.StandaloneWindows64, "Windows"),
            (BuildTarget.Android, "Android"),
            (BuildTarget.iOS, "iOS"),
            (BuildTarget.WebGL, "WebGL"),
        };

        private readonly HashSet<BuildTarget> _selected = new();
        private string _version = "";
        private bool _incremental;
        private bool _openOutputAfterBuild = true;
        private Vector2 _scroll;

        [MenuItem(MenuPath, false, 2)]
        public static void Open()
        {
            GetWindow<BundleBuildWindow>("AB打包工具");
        }

        private void OnEnable()
        {
            // 默认勾选当前平台
            _selected.Add(EditorUserBuildSettings.activeBuildTarget);
        }

        private void OnGUI()
        {
            DrawPlatformSection();
            EditorGUILayout.Space(6);
            DrawBuildOptions();
            EditorGUILayout.Space(8);
            DrawBuildAction();
            EditorGUILayout.Space(8);
            DrawOutputSection();
        }

        // ── 平台选择 ─────────────────────────────────────────────

        private void DrawPlatformSection()
        {
            EditorGUILayout.LabelField("构建平台", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS && Application.platform != RuntimePlatform.OSXEditor
                    ? "当前编辑器为 Windows，勾选 iOS 构建会失败（iOS 需在 macOS 出包）。"
                    : "可多选；按勾选顺序依次构建，切平台会触发脚本重编译，请耐心等待。",
                MessageType.Info);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (var (target, label) in Platforms)
            {
                var isCurrent = EditorUserBuildSettings.activeBuildTarget == target;
                var text = isCurrent ? $"{label}（当前）" : label;

                EditorGUILayout.BeginHorizontal();
                var on = EditorGUILayout.ToggleLeft(text, _selected.Contains(target),
                    GUILayout.Width(160));
                if (on && !_selected.Contains(target)) _selected.Add(target);
                if (!on && _selected.Contains(target)) _selected.Remove(target);

                // 平台产物状态
                var dir = BundleBuilder.StreamingOutputPath(target);
                GUILayout.Label(DirState(dir), EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("打开", EditorStyles.miniButton, GUILayout.Width(48)))
                {
                    var full = Path.GetFullPath(dir);
                    if (Directory.Exists(full)) EditorUtility.RevealInFinder(full);
                    else Debug.Log($"[BundleBuild] 产物目录不存在: {full}");
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                _selected.Clear();
                foreach (var (target, _) in Platforms) _selected.Add(target);
            }

            if (GUILayout.Button("清空", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                _selected.Clear();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>产物目录状态描述。</summary>
        private static string DirState(string assetPath)
        {
            var full = Path.GetFullPath(assetPath);
            if (!Directory.Exists(full)) return "未构建";
            var files = Directory.GetFiles(full).Where(f => !f.EndsWith(".manifest")).ToList();
            var manifest = Path.Combine(full, "manifest.json");
            var version = File.Exists(manifest)
                ? BundleManifestManager.Parse(File.ReadAllText(manifest))?.version ?? "?"
                : "?";
            return $"已构建 {files.Count} 个文件 · 版本 {version}";
        }

        // ── 构建选项 ─────────────────────────────────────────────

        private void DrawBuildOptions()
        {
            EditorGUILayout.LabelField("构建选项", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("版本号", GUILayout.Width(48));
            _version = EditorGUILayout.TextField(
                string.IsNullOrEmpty(_version) ? DateTime.Now.ToString("yyyyMMdd_HHmm") : _version);
            if (GUILayout.Button("当前时间", EditorStyles.miniButton, GUILayout.Width(64)))
            {
                _version = DateTime.Now.ToString("yyyyMMdd_HHmm");
            }

            EditorGUILayout.EndHorizontal();

            _incremental = EditorGUILayout.ToggleLeft(
                new GUIContent("增量构建（Unity 按资产哈希复用未变更 bundle，更快；改动大时建议全量）", 
                    "去掉 ForceRebuild 选项"), _incremental);
            _openOutputAfterBuild = EditorGUILayout.ToggleLeft("构建完成后打开产物目录", _openOutputAfterBuild);

            EditorGUILayout.EndVertical();
        }

        // ── 构建动作 ─────────────────────────────────────────────

        private void DrawBuildAction()
        {
            var rules = BundleSystemWindow.CollectAllRulesPublic();
            var ruleInfo = rules.Length == 0
                ? "工程内没有打包规则（BundleRuleAsset）"
                : $"{rules.Length} 条规则 · {_layoutInfo(rules)}";

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(ruleInfo, EditorStyles.miniLabel);
                EditorGUILayout.Space(2);

                using (new EditorGUI.DisabledScope(_selected.Count == 0 || rules.Length == 0))
                {
                    var label = _selected.Count switch
                    {
                        0 => "开始构建",
                        1 => $"开始构建（{Platforms.First(p => p.target == FirstSelected()).label}）",
                        _ => $"开始构建（{_selected.Count} 个平台）",
                    };
                    if (GUILayout.Button(label, GUILayout.Height(30)))
                    {
                        BuildSelected();
                    }
                }
            }
        }

        private BuildTarget FirstSelected() => _selected.First();

        private string _layoutInfo(IReadOnlyList<BundleRuleAsset> rules)
        {
            var layout = BundleRuleApplier.ComputeLayout(rules);
            if (!layout.Success)
                return $"⚠ {layout.conflicts.Count} 个命名冲突（构建会被中止，详见 Console）";
            return $"{layout.bundles.Count} 个 bundle · {layout.TotalAssets} 个资源";
        }

        private void BuildSelected()
        {
            if (string.IsNullOrWhiteSpace(_version))
            {
                EditorUtility.DisplayDialog("版本号无效", "请填写版本号（如 20261031_1300）", "确定");
                return;
            }

            var rules = BundleSystemWindow.CollectAllRulesPublic();
            if (rules.Length == 0)
            {
                EditorUtility.DisplayDialog("没有规则", "工程内没有 BundleRuleAsset，无法构建。", "确定");
                return;
            }

            // 记录初始平台，构建完恢复
            var originTarget = EditorUserBuildSettings.activeBuildTarget;
            var queue = Platforms.Select(p => p.target).Where(_selected.Contains).ToList();

            try
            {
                foreach (var target in queue)
                {
                    // 构建目标平台与当前平台不一致时先切换（切平台触发重编译，Editor 会短暂卡顿）
                    if (EditorUserBuildSettings.activeBuildTarget != target)
                    {
                        if (!EditorUtility.DisplayDialog("切换平台",
                                $"构建 {target} 需要从 {originTarget} 切换平台，会触发脚本重编译。\n继续？",
                                "继续", "取消"))
                        {
                            return;
                        }

                        EditorUserBuildSettings.SwitchActiveBuildTarget(
                            BuildPipeline.GetBuildTargetGroup(target), target);
                        // SwitchActiveBuildTarget 是同步阻塞的（返回后即已切换完成）
                    }

                    var options = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode;
                    if (!_incremental)
                    {
                        options |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
                    }

                    EditorUtility.DisplayProgressBar("AB 打包", $"构建 {target} 中…", 0.5f);
                    var ok = BundleBuilder.Build(rules, target, _version.Trim(), null, options);
                    if (!ok)
                    {
                        Debug.LogError($"[BundleBuild] {target} 构建失败，中止后续平台");
                        return;
                    }
                }

                if (_openOutputAfterBuild && queue.Count > 0)
                {
                    EditorUtility.RevealInFinder(
                        Path.GetFullPath(BundleBuilder.StreamingOutputPath(queue[^1])));
                }

                EditorUtility.DisplayDialog("构建完成",
                    $"版本 {_version}\n平台: {string.Join("、", queue)}\n" +
                    $"输出: {BundleBuilder.StreamingOutputPath(queue[^1])}", "确定");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ── 产物说明 ─────────────────────────────────────────────

        private void DrawOutputSection()
        {
            EditorGUILayout.LabelField("产物说明", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "包内置: Assets/StreamingAssets/Bundles/<平台>/\n" +
                "CDN 产物: <工程根>/Bundles/<平台>/<版本>/（整个目录上传 CDN）\n" +
                "manifest.json / version.json 与 bundle 同目录散放。",
                MessageType.None);

            if (GUILayout.Button("打开 StreamingAssets/Bundles", EditorStyles.miniButton))
            {
                var full = Path.GetFullPath("Assets/StreamingAssets/Bundles");
                if (Directory.Exists(full)) EditorUtility.RevealInFinder(full);
                else Debug.Log($"[BundleBuild] 目录不存在: {full}");
            }
        }
    }
}
