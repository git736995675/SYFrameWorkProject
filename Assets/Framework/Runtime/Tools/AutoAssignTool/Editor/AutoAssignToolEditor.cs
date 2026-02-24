#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace SY
{
    public class AutoAssignToolEditor : EditorWindow
    {
        [MenuItem("SY_Tools/脚本变量分配")]
        public static void ShowWindow()
        {
            GetWindow<AutoAssignToolEditor>("脚本变量分配");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("为选中的物体分配变量"))
            {
                ProcessSelectedPrefab();
            }
        }

        [MenuItem("GameObject/自动分配变量", false, 0)]
        private static void ProcessSelectedPrefab()
        {
            var selectionGameObjs = Selection.gameObjects;
            foreach (var prefab in selectionGameObjs)
            {
                if (prefab == null)
                {
                    Debug.LogError("No prefab selected!");
                    continue;
                }

                AutoAssignTool.ProcessSelectedPrefab(prefab);
            }
        }
    }
}
#endif