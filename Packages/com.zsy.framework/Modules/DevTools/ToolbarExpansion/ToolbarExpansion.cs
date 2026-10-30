using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using ZSYFramework.AssetBundle.Editor;
using ZSYFramework.AssetBundle;
using ZSYFramework.DevTools.Utils;
using ZSYFramework.Table.Editor;


namespace ZSYFramework.DevTools
{
[InitializeOnLoad]
/// <summary>
/// 编辑器工具栏扩展。在播放按钮右侧注入框架快捷按钮（AB模式/表/场景/显隐等）。
/// </summary>
public static class ToolbarExpansion
{
private static ScriptableObject _toolbar;

static ToolbarExpansion()
{
    // 每100ms检测一次GUI变化（确保按钮持续存在）
    EditorApplication.update += OnUpdate;
}

private static void OnUpdate()
{
    if (_toolbar != null) return;
    var toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
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
    GUI.enabled = true;
    DrawSamplesVisibility();
    GUILayout.EndHorizontal();
}

/// <summary>
/// Samples~ 显隐切换按钮（管理工程专用：应用项目里 Samples~ 目录不存在，
/// 点击只会日志提示"未找到目录"，无任何副作用）
/// </summary>
private static void DrawSamplesVisibility()
{
    var visible = SamplesVisibilityTool.IsVisible;
    var content = new GUIContent
    {
        text = visible ? "Samples√" : "Samples",
        tooltip = visible
            ? "Samples 当前显示中（可直接调试 Demo/插件）。\n点击隐藏，恢复面板 Import 形态。\n提交推送前请务必隐藏！"
            : "Samples 当前隐藏（面板 Import 形态）。\n点击显示，临时去掉 ~ 直接调试 Demo/插件。"
    };
    var oldColor = GUI.backgroundColor;
    if (visible) GUI.backgroundColor = new Color(1f, 0.76f, 0.3f); // 显示态橙色警示：提交前要切回
    if (GUILayout.Button(content))
    {
        SamplesVisibilityTool.Toggle();
    }
    GUI.backgroundColor = oldColor;
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
