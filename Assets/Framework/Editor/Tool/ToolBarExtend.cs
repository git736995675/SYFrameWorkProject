#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace SY
{
    [InitializeOnLoad]
    public static class ToolBarExtend
    {
        private static ScriptableObject _toolbar;

        static ToolBarExtend()
        {
            // 每100ms检测一次GUI变化（确保按钮持续存在）
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (_toolbar != null) return;
            var toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
            var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            _toolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            if (_toolbar == null) return;
            var root = _toolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(_toolbar);
            var toolbarZone = ((VisualElement)root).Q("ToolbarZonePlayMode");

            var parent = new VisualElement()
            {
                style = { flexGrow = 1, flexDirection = FlexDirection.Row }
            };
            var container = new IMGUIContainer();
            container.onGUIHandler += OnGuiBody;
            parent.Add(container);
            toolbarZone.Add(parent);
        }

        private static void OnGuiBody()
        {
            GUILayout.BeginHorizontal();
            DrawLoadModeMenu();
            DrawTableMenu();
            GUI.enabled = !EditorApplication.isPlaying; // 根据运行状态禁用按钮
            DrawSceneMenu();
            DrawRefreshMenu();
            GUILayout.EndHorizontal();
        }

        private static void DrawLoadModeMenu()
        {
            var menu = new GenericMenu();
            var content = new GUIContent
            {
                text = ABLoadManager.LoadModeSetting == LoadMode.AssetBundle ? "当前Ab" : "当前Editor",
                tooltip = "切换资源加载模式"
            };
            if (!GUILayout.Button(content)) return;
            ABLoadManager.LoadModeSetting = ABLoadManager.LoadModeSetting == LoadMode.AssetBundle
                ? LoadMode.Editor
                : LoadMode.AssetBundle;
            menu.ShowAsContext();
        }

        private static void DrawRefreshMenu()
        {
            var menu = new GenericMenu();
            var content = new GUIContent
            {
                text = "刷新",
                tooltip = "刷新资源信息表"
            };
            if (!GUILayout.Button(content)) return;
            TableEditor.ReadAllExcel();
            ResourceBuilderWindow.GenerateInfoListFile();
            menu.ShowAsContext();
            AssetDatabase.Refresh();
        }

        private static void DrawTableMenu()
        {
            var menu = new GenericMenu();
            var content = new GUIContent
            {
                text = "表",
                tooltip = "打开表目录"
            };

            if (!GUILayout.Button(content)) return;
            // 定义支持的表格类型扩展名
            string[] extensions = { ".csv", ".xlsx" };
            // 获取所有匹配文件
            var paths = AssetDatabase.FindAssets("t:DefaultAsset").Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => extensions.Any(path.EndsWith));
            foreach (var path in paths)
            {
                if (path.Contains("~$")) continue;
                var sceneName = Path.GetFileName(path);
                menu.AddItem(new GUIContent($"Open {sceneName}"), false, () => OpenExcel(path));
            }

            menu.ShowAsContext();
        }

        private static void DrawSceneMenu()
        {
            var sceneMenu = new GenericMenu();
            var content = new GUIContent
            {
                image = EditorGUIUtility.IconContent("d_animationvisibilitytoggleon").image,
                text = "场景",
                tooltip = "切换场景"
            };
            if (!GUILayout.Button(content)) return;
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            if (sceneGuids.Length == 0) return;
            foreach (var guid in sceneGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("Assets/")) continue;
                var sceneName = Path.GetFileName(path);
                sceneMenu.AddItem(new GUIContent($"Switch to {sceneName}"), false, () => LoadScene(path));
            }

            sceneMenu.ShowAsContext();
        }

        private static void LoadScene(string scenePath)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null) return;
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(sceneAsset));
        }

        private static void OpenExcel(string path)
        {
            var fullPath = Path.GetFullPath(path);

            // 跨平台打开方式
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                System.Diagnostics.Process.Start("open", $"\"{fullPath}\"");
            }
        }
    }
}

#endif