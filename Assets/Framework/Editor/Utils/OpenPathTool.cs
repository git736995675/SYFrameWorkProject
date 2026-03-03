#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.Utils
{
    public class OpenPathTool : EditorWindow
    {
        [MenuItem("SY_Tools/跳转目录", false, 999)]
        public static void ShowWindow()
        {
            GetWindow<OpenPathTool>("跳转目录");
        }


        private void OnGUI()
        {
            if (GUILayout.Button("打开项目根目录"))
            {
                Application.OpenURL(Application.dataPath);
            }

            if (GUILayout.Button("打开项目缓存路径"))
            {
                Application.OpenURL(Application.persistentDataPath);
            }
        }
    }
}

#endif