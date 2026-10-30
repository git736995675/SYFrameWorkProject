using UnityEditor;
using UnityEngine;
using ZSYFramework.Core.Json;
using ZSYFramework.Core;

#if UNITY_EDITOR

namespace ZSYFramework.DevTools;
/// <summary>
/// 自动赋值工具的编辑器入口。提供菜单触发自动绑定。
/// </summary>
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
#endif
