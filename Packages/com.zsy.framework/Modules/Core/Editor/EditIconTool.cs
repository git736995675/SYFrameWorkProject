using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZSYFramework.Core.Editor.Utils
{
/// <summary>
/// unity内嵌图标获取（待补充）
/// </summary>
public abstract class EditIconTool
{
private static readonly Dictionary<string, GUIContent> FileIcons = new();

public static GUIContent GetIcon(string key)
{
    if(FileIcons==null || FileIcons.Count == 0)
    {
        InitIcons();
    }
    return FileIcons != null && FileIcons.TryGetValue(key, out var forNode)
        ? forNode
        : EditorGUIUtility.IconContent("TextAsset Icon");
}

private static void InitIcons()
{
    // 初始化文件图标字典
    FileIcons[".cs"] = EditorGUIUtility.IconContent("cs Script Icon");
    FileIcons[".unity"] = EditorGUIUtility.IconContent("SceneAsset Icon");
    FileIcons[".prefab"] = EditorGUIUtility.IconContent("Prefab Icon");
    FileIcons[".mat"] = EditorGUIUtility.IconContent("Material Icon");
    FileIcons[".png"] = EditorGUIUtility.IconContent("Texture Icon");
    FileIcons[".jpg"] = EditorGUIUtility.IconContent("Texture Icon");
    FileIcons[".jpeg"] = EditorGUIUtility.IconContent("Texture Icon");
    FileIcons[".tga"] = EditorGUIUtility.IconContent("Texture Icon");
    FileIcons[".json"] = EditorGUIUtility.IconContent("TextAsset Icon");
    FileIcons[".txt"] = EditorGUIUtility.IconContent("TextAsset Icon");
    FileIcons[".xlsx"] = EditorGUIUtility.IconContent("d_TableView@2x"); // Excel文件图标
    FileIcons[".config"] = EditorGUIUtility.IconContent("Settings Icon");
    FileIcons[".mp3"] = EditorGUIUtility.IconContent("AudioClip Icon");
    FileIcons[".ogg"] = EditorGUIUtility.IconContent("AudioClip Icon");
    FileIcons[".ttf"] = EditorGUIUtility.IconContent("Font Icon");
    FileIcons[".shader"] = EditorGUIUtility.IconContent("Shader Icon");
    FileIcons[".asset"] = EditorGUIUtility.IconContent("ScriptableObject Icon");
    FileIcons[".mp4"] = EditorGUIUtility.IconContent("VideoClip Icon");

    FileIcons["Folder"] = EditorGUIUtility.IconContent("Folder Icon");
    FileIcons["FolderOpened"] = EditorGUIUtility.IconContent("FolderOpened Icon");
    FileIcons["FolderEmpty"] = EditorGUIUtility.IconContent("FolderEmpty Icon");
}

}
}
