using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ZSYFramework.AssetBundle.Editor
{
    /// <summary>
    /// 文件夹 Inspector 的 AB 打包设置面板。
    /// 在 Project 窗口选中文件夹时，直接在 Inspector 里配置该目录是否参与打包、
    /// Bundle 命名方式、远程策略、排除规则与例外条目。
    /// 规则数据仍存储在 BundleRuleAsset（约定放在该文件夹内，命名 {目录名}_bundle_rule.asset），
    /// 打包管线零改动：BundleSystemWindow 可"自动收集全部规则"一键纳入构建。
    /// 注意：Unity 没有 FolderAsset 类型，文件夹就是 DefaultAsset；
    /// 用 [CustomEditor(typeof(DefaultAsset))] 接管后需自行排除非文件夹资产。
    /// 命名空间末段与 UnityEditor.Editor 类同名，基类必须写全限定名。
    /// </summary>
    [CustomEditor(typeof(DefaultAsset))]
    public sealed class BundleFolderInspector : UnityEditor.Editor
    {
        private string _folderPath;
        private BundleRuleAsset _rule;
        private BundleLayout _preview;
        private bool _foldout = true;

        public override void OnInspectorGUI()
        {
            var path = AssetDatabase.GetAssetPath(target);

            // DefaultAsset 也涵盖非文件夹资产(如无导入器的杂项文件), 非文件夹只画默认面板
            var isFolder = AssetDatabase.IsValidFolder(path);
            if (!isFolder)
            {
                DrawDefaultInspector();
                return;
            }

            // 文件夹本身没有默认 Inspector 内容; 历史怪癖: GUI 可能预置 disabled, 显式恢复
            GUI.enabled = true;

            // 切换选中对象时 Editor 实例可能复用, 路径变化即重置缓存
            if (path != _folderPath)
            {
                _folderPath = path;
                _rule = FindRule(path);
                _preview = null;
            }

            EditorGUILayout.Space(6);
            DrawBundleSection(path);
        }

        // ── 设置区块 ─────────────────────────────────────────────

        private void DrawBundleSection(string folderPath)
        {
            var header = _rule == null ? "AssetBundle 打包设置" : "AssetBundle 打包设置（已启用）";
            _foldout = EditorGUILayout.Foldout(_foldout, header, true, EditorStyles.foldoutHeader);
            if (!_foldout) return;

            // 折叠内容包进卡片容器: 统一内边距, 修复 HelpBox 缩进 26px / 按钮顶格 8px 的错位
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(2);

            if (_rule == null)
            {
                var underAssets = folderPath.StartsWith("Assets/");
                if (underAssets)
                {
                    EditorGUILayout.LabelField("该文件夹未参与 AB 打包。", EditorStyles.miniLabel);
                    EditorGUILayout.Space(2);
                    if (GUILayout.Button("启用 AB 打包"))
                    {
                        _rule = CreateRule(folderPath);
                        _preview = null;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("仅支持在 Assets/ 下的文件夹创建规则资产。", MessageType.Warning);
                }
            }
            else
            {
                GUILayout.Space(4);
                DrawRuleFields();

                EditorGUILayout.Space(6);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("预览 bundle 划分"))
                {
                    _preview = BundleRuleApplier.ComputeLayout(new[] { _rule });
                }
                if (GUILayout.Button("定位规则资产", GUILayout.Width(96)))
                {
                    EditorGUIUtility.PingObject(_rule);
                }
                EditorGUILayout.EndHorizontal();

                DrawPreviewResult();

                EditorGUILayout.Space(4);
                if (GUILayout.Button("移除打包配置", EditorStyles.miniButton))
                {
                    TryRemoveRule();
                }
            }

            GUILayout.Space(2);
            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }

        /// <summary>内嵌绘制规则字段（隐藏 targetDirectory：它就是当前文件夹本身）。</summary>
        private void DrawRuleFields()
        {
            var so = new SerializedObject(_rule);
            var naming = (BundleNamingMode)so.FindProperty("namingMode").enumValueIndex;

            var it = so.GetIterator();
            it.NextVisible(true); // m_Script
            while (it.NextVisible(false))
            {
                if (it.name == "targetDirectory") continue;
                // rootBundleName 仅 RootDir 模式有意义, 其余模式隐藏避免误配
                if (it.name == "rootBundleName" && naming != BundleNamingMode.RootDir) continue;
                EditorGUILayout.PropertyField(it, true);
            }

            so.ApplyModifiedProperties();
        }

        private void DrawPreviewResult()
        {
            if (_preview == null) return;

            if (!_preview.Success)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("存在 bundle 名冲突（详见 Console）", MessageType.Error);
                return;
            }

            const int maxLines = 20;
            var total = _preview.bundles.Count;
            var remote = _preview.bundles.Values.Count(b => b.isRemote);
            var lines = _preview.bundles.Values
                .OrderBy(b => b.name)
                .Take(maxLines)
                .Select(b => $"· {b.name}  ({b.assetPaths.Count})");

            var text = $"共 {total} 个 bundle（远程 {remote} 个）：\n" + string.Join("\n", lines);
            if (total > maxLines) text += $"\n… 其余 {total - maxLines} 个省略";

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(text, MessageType.Info);
        }

        private void TryRemoveRule()
        {
            if (!GUILayout.Button("移除打包配置")) return;

            var assetPath = AssetDatabase.GetAssetPath(_rule);
            if (!EditorUtility.DisplayDialog("移除打包配置",
                    $"删除 {assetPath} ？\n该操作只删除规则资产, 不修改文件夹内其他资源。",
                    "删除", "取消"))
            {
                return;
            }

            AssetDatabase.DeleteAsset(assetPath);
            _rule = null;
            _preview = null;
        }

        // ── 规则资产查找/创建 ────────────────────────────────────

        /// <summary>查找目标目录为 folderPath 的规则资产（约定一目录一规则）。</summary>
        private static BundleRuleAsset FindRule(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return null;

            foreach (var guid in AssetDatabase.FindAssets("t:BundleRuleAsset"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var rule = AssetDatabase.LoadAssetAtPath<BundleRuleAsset>(p);
                if (rule != null && rule.TargetAssetPath == folderPath) return rule;
            }

            return null;
        }

        /// <summary>在文件夹内创建规则资产并回填 targetDirectory。</summary>
        private static BundleRuleAsset CreateRule(string folderPath)
        {
            var rule = ScriptableObject.CreateInstance<BundleRuleAsset>();
            rule.targetDirectory = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);

            var folderName = folderPath[(folderPath.LastIndexOf('/') + 1)..];
            var assetPath = $"{folderPath}/{folderName}_bundle_rule.asset";

            AssetDatabase.CreateAsset(rule, assetPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(rule);

            Debug.Log($"[BundleSystem] 已创建规则资产: {assetPath}", rule);
            return rule;
        }
    }
}
