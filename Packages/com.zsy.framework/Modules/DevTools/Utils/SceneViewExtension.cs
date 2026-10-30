using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace ZSYFramework.DevTools.Utils
{
[InitializeOnLoad]
/// <summary>
/// Scene 视图扩展。在场景视图中注入自定义绘制与快捷操作。
/// </summary>
public class SceneViewExtension
{
static void OnSceneGUI(SceneView sceneView)
{
    Handles.BeginGUI();

    // 将按钮定位到右上角（靠近播放按钮）
    GUILayout.BeginArea(new Rect(Screen.width - 120, 5, 110, 25));
    if (GUILayout.Button("▶ Custom", EditorStyles.toolbarButton))
    {
        Debug.Log("Scene Toolbar Button Clicked");
    }
    GUILayout.EndArea();

    Handles.EndGUI();
}
}
}
#endif
