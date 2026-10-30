#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ZSYFramework.Table.Editor
{
/// <summary>
/// 表格编辑器
/// </summary>
public partial class TableEditor
{
private static bool _isCompression = false;

private const bool ErrorIsEncryption = false;

private static string TablePath => Path.Combine(Application.streamingAssetsPath, "TableIgnore.txt");

private static List<string> _paths;

private static List<string> _ignores;

[MenuItem("SY_Tools/表格管理", false, 201)]
public static void ShowWindow()
{
    var window = GetWindow(typeof(TableEditor));
    window.titleContent = new GUIContent("配置表格管理窗口");
    _paths = null;
    _ignores = null;
    ReadPath();
}

private void OnGUI()
{
    DrawButton();
}

/// <summary>
/// 绘制按钮
/// </summary>
private void DrawButton()
{
    EditorGUILayout.Space(5);
    _paths?.ForEach(DrawPath);

    EditorGUILayout.Space(10);
    _isCompression = GUILayout.Toggle(_isCompression, "Json是否进行压缩");
    //_isEncryption = GUILayout.Toggle(_isEncryption, "Json是否进行加密");

    GUILayout.Space(10);
    if (GUILayout.Button("BuildSelected"))
    {
        SelectionRead();
        AssetDatabase.Refresh();
    }

    GUILayout.Space(10);
    if (!GUILayout.Button("BuildAll")) return;
    ReadAllExcel();
    AssetDatabase.Refresh();
}

private static void ReadPath()
{
    string[] extensions = { ".csv", ".xlsx" };
    _ignores ??= File.Exists(TablePath) ? File.ReadAllLines(TablePath).ToList() : new List<string>();
    // 获取所有匹配文件

    _paths = AssetDatabase.FindAssets("t:DefaultAsset").Select(AssetDatabase.GUIDToAssetPath)
        .Where(path => extensions.Any(path.EndsWith)
                       && !_ignores.Contains(path)
                       && !path.Contains("~$")
                       && path.Contains("Assets/")
        )
        .ToList();
}

private static void DrawPath(string path)
{
    GUILayout.BeginHorizontal();
    GUILayout.Label($"表格路径 : {path}", EditorStyles.boldLabel);
    if (GUILayout.Button("忽略", GUILayout.Width(100)))
    {
        _ignores ??= File.Exists(TablePath) ? File.ReadAllLines(TablePath).ToList() : new List<string>();
        if (_ignores.Contains(path)) return;
        _ignores.Add(path);
        Directory.CreateDirectory(Path.GetDirectoryName(TablePath) ?? string.Empty);
        File.WriteAllLines(TablePath, _ignores, Encoding.UTF8);
        ReadPath();
        AssetDatabase.Refresh();
    }

    GUILayout.EndHorizontal();
}

private void AddPath(string outputFilePath)
{
    if (_paths == null || _paths.Contains(outputFilePath)) return;
    _paths.Add(outputFilePath);
    Directory.CreateDirectory(Path.GetDirectoryName(TablePath) ?? string.Empty);
    File.WriteAllLines(TablePath, _paths, Encoding.UTF8);
}
}

}
#endif
