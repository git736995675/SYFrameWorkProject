using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/**
 *Copyright(C) 2017 by 东爵
 *All rights reserved.
 *FileName:     BuyOperator.cs
 *Author:       jojo
 *Version:      1.0
 *UnityVersion：5.5.0f3
 *Date:         2017-01-16
 *Description:  快速创建文件夹
 *History:
*/
public class CreateFolder {
    private static string path = Application.dataPath + "/";
#if UNITY_EDITOR
    [MenuItem("Assets/CreatFolder/Scripts")]
    static void CreateScripts() {
        Directory.CreateDirectory(path + "Scripts");
        AssetDatabase.Refresh();
    }
    [MenuItem("Assets/CreatFolder/Scenes %_U")]
    static void CreateScenes() {
        Directory.CreateDirectory(path + "Scenes");
        AssetDatabase.Refresh();
    }
    [MenuItem("Assets/CreatFolder/Prefabs %_I")]
    static void CreatePrefabs() {
        Directory.CreateDirectory(path + "Prefabs");
        AssetDatabase.Refresh();
    }
    [MenuItem("Assets/CreatFolder/Resources %_G")]
    static void CreateResources() {
        Directory.CreateDirectory(path + "Resources");
        AssetDatabase.Refresh();
    }
    [MenuItem("Assets/CreatFolder/Materials %_M")]
    static void CreateMaterials() {
        Directory.CreateDirectory(path + "Materias");
        AssetDatabase.Refresh();
    }
    [MenuItem("Assets/CreatFolder/Textures %_T")]
    static void CreateTextures() {
        Directory.CreateDirectory(path + "Textures");
        AssetDatabase.Refresh();
    }
    [MenuItem("Assets/CreatFolder/StreamingAssets")]
    static void CreateStreamingAssets() {
        Directory.CreateDirectory(path + "StreamingAssets");
        AssetDatabase.Refresh();
    }
    [MenuItem("Assets/CreatFolder/Plugins")]
    static void CreatePlugins() {
        Directory.CreateDirectory(path + "Plugins");
        AssetDatabase.Refresh();
    }
    [MenuItem("Assets/CreatFolder/BaseFolder #%_O", false, 11)]
    static void CreateBaseFolder() {
        Directory.CreateDirectory(path + "Scripts");
        Directory.CreateDirectory(path + "Scenes");
        Directory.CreateDirectory(path + "Prefabs");
        Directory.CreateDirectory(path + "Materias");
        Directory.CreateDirectory(path + "Textures");
        AssetDatabase.Refresh();
    }
#endif
}
