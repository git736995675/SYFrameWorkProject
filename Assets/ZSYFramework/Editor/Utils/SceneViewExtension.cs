#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ZSYFramework.Editor.Utils
{
    [InitializeOnLoad]
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
