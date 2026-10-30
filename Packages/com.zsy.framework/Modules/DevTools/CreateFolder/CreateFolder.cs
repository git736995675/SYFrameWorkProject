using System.IO;
using UnityEditor;
using UnityEngine;


namespace ZSYFramework.DevTools
{
/// <summary>
/// 文件夹批量创建工具。
/// </summary>
public abstract class CreateFolder
{
private static readonly string Path = Application.dataPath + "/";

[MenuItem("Assets/SY-Tool 创建文件夹/Scripts")]
private static void CreateScripts()
{
    Directory.CreateDirectory(Path + "Scripts");
    AssetDatabase.Refresh();
}

[MenuItem("Assets/SY-Tool 创建文件夹/Scenes %_U")]
private static void CreateScenes()
{
    Directory.CreateDirectory(Path + "Scenes");
    AssetDatabase.Refresh();
}

[MenuItem("Assets/SY-Tool 创建文件夹/Prefabs %_I")]
private static void CreatePrefabs()
{
    Directory.CreateDirectory(Path + "Prefabs");
    AssetDatabase.Refresh();
}

[MenuItem("Assets/SY-Tool 创建文件夹/Resources %_G")]
private static void CreateResources()
{
    Directory.CreateDirectory(Path + "Resources");
    AssetDatabase.Refresh();
}

[MenuItem("Assets/SY-Tool 创建文件夹/Materials %_M")]
private static void CreateMaterials()
{
    Directory.CreateDirectory(Path + "Materials");
    AssetDatabase.Refresh();
}

[MenuItem("Assets/SY-Tool 创建文件夹/Textures %_T")]
private static void CreateTextures()
{
    Directory.CreateDirectory(Path + "Textures");
    AssetDatabase.Refresh();
}

[MenuItem("Assets/SY-Tool 创建文件夹/StreamingAssets")]
private static void CreateStreamingAssets()
{
    Directory.CreateDirectory(Path + "StreamingAssets");
    AssetDatabase.Refresh();
}

[MenuItem("Assets/SY-Tool 创建文件夹/Plugins")]
private static void CreatePlugins()
{
    Directory.CreateDirectory(Path + "Plugins");
    AssetDatabase.Refresh();
}

[MenuItem("Assets/SY-Tool 创建文件夹/BaseFolder #%_O", false, 11)]
private static void CreateBaseFolder()
{
    Directory.CreateDirectory(Path + "Scripts");
    Directory.CreateDirectory(Path + "Scenes");
    Directory.CreateDirectory(Path + "Prefabs");
    Directory.CreateDirectory(Path + "Materials");
    Directory.CreateDirectory(Path + "Textures");
    AssetDatabase.Refresh();
}
}

}
