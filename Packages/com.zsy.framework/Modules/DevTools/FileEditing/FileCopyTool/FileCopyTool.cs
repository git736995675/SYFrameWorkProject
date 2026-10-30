using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace ZSYFramework.DevTools
{
/// <summary>
/// 文件批量复制工具窗口。
/// </summary>
public class FileCopyTool : EditorWindow
{
private string _sourceFolder = "";
private string _targetFolder = "";
private bool _copyWithStructure = true;
private bool _overwriteFiles = false;

// 文件类型选择
private bool _includeImages = true;
private bool _includeVideos = true;
private bool _includeText = true;
private bool _includeBinary = true;
private bool _includeAnimations = true;
private bool _includePrefabs = true;
private bool _includeMaterials = true;
private bool _includeScenes = true;
private bool _includeScripts = true;
private bool _includeSounds = true;

// 自定义扩展名
private string _customExtensions = "";

// 进度显示
private int _filesCopied = 0;
private int _totalFiles = 0;
private bool _isCopying = false;

private Vector2 _scrollPosition;

[MenuItem("SY-Tool/开发工具/文件拷贝工具", false, 3)]
public static void ShowWindow()
{
    GetWindow<FileCopyTool>("文件拷贝工具");
}

private void SetInclude(bool isInclude)
{
    _includeImages = isInclude;
    _includeVideos = isInclude;
    _includeText = isInclude;
    _includeBinary = isInclude;
    _includeAnimations = isInclude;
    _includePrefabs = isInclude;
    _includeMaterials = isInclude;
    _includeScenes = isInclude;
    _includeScripts = isInclude;
    _includeSounds = isInclude;
}

private void OnGUI()
{
    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

    EditorGUILayout.Space(10);
    EditorGUILayout.LabelField("文件拷贝工具", EditorStyles.boldLabel);
    EditorGUILayout.Space(10);

    // 源文件夹选择
    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("源文件夹:", GUILayout.Width(80));
    _sourceFolder = EditorGUILayout.TextField(_sourceFolder);
    if (GUILayout.Button("浏览", GUILayout.Width(60)))
    {
        string path = EditorUtility.OpenFolderPanel("选择源文件夹", Application.dataPath, "");
        if (!string.IsNullOrEmpty(path))
        {
            _sourceFolder = path;
        }
    }

    EditorGUILayout.EndHorizontal();

    // 目标文件夹选择
    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("目标文件夹:", GUILayout.Width(80));
    _targetFolder = EditorGUILayout.TextField(_targetFolder);
    if (GUILayout.Button("浏览", GUILayout.Width(60)))
    {
        var path = EditorUtility.OpenFolderPanel("选择目标文件夹", Application.dataPath, "");
        if (!string.IsNullOrEmpty(path))
        {
            _targetFolder = path;
        }
    }

    EditorGUILayout.EndHorizontal();

    EditorGUILayout.Space(15);

    // 拷贝选项
    EditorGUILayout.LabelField("拷贝选项:", EditorStyles.boldLabel);
    _copyWithStructure = EditorGUILayout.Toggle("按原目录结构拷贝", _copyWithStructure, GUILayout.Width(500));
    _overwriteFiles = EditorGUILayout.Toggle("覆盖已存在文件", _overwriteFiles, GUILayout.Width(500));

    EditorGUILayout.Space(15);

    // 文件类型选择
    EditorGUILayout.LabelField("选择文件类型:", EditorStyles.boldLabel);

    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("图片文件 (.png, .jpg, .jpeg, .tga, .bmp, .psd)", GUILayout.Width(300));
    _includeImages = EditorGUILayout.Toggle(_includeImages);
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("音频文件 (.mp3, .ogg)", GUILayout.Width(300));
    _includeSounds = EditorGUILayout.Toggle(_includeSounds);
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("视频文件 (.mp4, .mov, .avi, .wmv)", GUILayout.Width(300));
    _includeVideos = EditorGUILayout.Toggle(_includeVideos);
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("文本文件 (.txt, .json, .xml, .csv)", GUILayout.Width(300));
    _includeText = EditorGUILayout.Toggle(_includeText);
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("二进制文件 (.bytes, .bin)", GUILayout.Width(300));
    _includeBinary = EditorGUILayout.Toggle(_includeBinary);
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("动画文件 (.anim, .fbx, .ma, .mb)", GUILayout.Width(300));
    _includeAnimations = EditorGUILayout.Toggle(_includeAnimations);
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("预制体 (.prefab)", GUILayout.Width(300));
    _includePrefabs = EditorGUILayout.Toggle(_includePrefabs);
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("材质 (.mat)", GUILayout.Width(300));
    _includeMaterials = EditorGUILayout.Toggle(_includeMaterials);
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("场景 (.unity)", GUILayout.Width(300));
    _includeScenes = EditorGUILayout.Toggle(_includeScenes);
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("脚本 (.cs)", GUILayout.Width(300));
    _includeScripts = EditorGUILayout.Toggle(_includeScripts);
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.Space(10);

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("ALL", GUILayout.Height(30)))
    {
        SetInclude(true);
    }

    if (GUILayout.Button("Clears", GUILayout.Height(30)))
    {
        SetInclude(false);
    }
    EditorGUILayout.EndHorizontal();

    // 自定义扩展名
    EditorGUILayout.LabelField("自定义扩展名 (用逗号分隔，例如: .asset,.aseprite)");
    _customExtensions = EditorGUILayout.TextField(_customExtensions);

    EditorGUILayout.Space(20);

    // 操作按钮
    EditorGUILayout.BeginHorizontal();

    GUI.enabled = !_isCopying && Directory.Exists(_sourceFolder) && !string.IsNullOrEmpty(_targetFolder);

    if (GUILayout.Button("开始拷贝", GUILayout.Height(30)))
    {
        StartCopy();
    }

    GUI.enabled = !_isCopying && Directory.Exists(_sourceFolder);

    if (GUILayout.Button("统计文件", GUILayout.Width(100), GUILayout.Height(30)))
    {
        CountFiles();
    }

    GUI.enabled = true;

    EditorGUILayout.EndHorizontal();

    // 进度显示
    if (_isCopying)
    {
        EditorGUILayout.Space(10);
        var rect = EditorGUILayout.GetControlRect(false, 20);
        EditorGUI.ProgressBar(rect, (float)_filesCopied / _totalFiles,
            $"正在拷贝: {_filesCopied}/{_totalFiles}");

        if (_filesCopied >= _totalFiles && _totalFiles > 0)
        {
            EditorGUILayout.HelpBox($"拷贝完成！共拷贝 {_filesCopied} 个文件。", MessageType.Info);
        }
    }
    else if (_totalFiles > 0)
    {
        EditorGUILayout.HelpBox($"找到 {_totalFiles} 个符合条件的文件", MessageType.Info);
    }

    // 快速操作区域
    EditorGUILayout.Space(20);
    EditorGUILayout.LabelField("快速操作:", EditorStyles.boldLabel);

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("选择Assets文件夹"))
    {
        _sourceFolder = Application.dataPath;
    }

    if (GUILayout.Button("选择Project根目录"))
    {
        _sourceFolder = Directory.GetParent(Application.dataPath).FullName;
    }

    if (GUILayout.Button("清空路径"))
    {
        _sourceFolder = "";
        _targetFolder = "";
    }

    EditorGUILayout.EndHorizontal();

    EditorGUILayout.EndScrollView();
}

private void StartCopy()
{
    if (!Directory.Exists(_targetFolder))
    {
        try
        {
            Directory.CreateDirectory(_targetFolder);
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"无法创建目标文件夹: {e.Message}", "确定");
            return;
        }
    }

    var extensions = GetSelectedExtensions();

    if (extensions.Count == 0)
    {
        EditorUtility.DisplayDialog("警告", "请至少选择一种文件类型", "确定");
        return;
    }

    // 开始拷贝
    _filesCopied = 0;
    _isCopying = true;

    // 在编辑器中异步执行拷贝操作
    EditorApplication.update += CopyFilesAsync;
}

private void CopyFilesAsync()
{
    try
    {
        var extensions = GetSelectedExtensions();
        var allFiles = Directory.GetFiles(_sourceFolder, "*.*", SearchOption.AllDirectories);
        var filteredFiles = new List<string>();

        // 过滤文件
        foreach (var file in allFiles)
        {
            var ext = Path.GetExtension(file).ToLower();
            if (extensions.Contains(ext))
            {
                filteredFiles.Add(file);
            }
        }

        _totalFiles = filteredFiles.Count;

        if (_filesCopied < _totalFiles)
        {
            var sourceFile = filteredFiles[_filesCopied];
            var relativePath = "";

            if (_copyWithStructure)
            {
                relativePath = sourceFile.Substring(_sourceFolder.Length);
            }
            else
            {
                relativePath = "/" + Path.GetFileName(sourceFile);
            }

            string targetFile = _targetFolder + relativePath;
            string targetDir = Path.GetDirectoryName(targetFile);

            // 创建目标目录
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // 检查是否覆盖
            if (File.Exists(targetFile) && !_overwriteFiles)
            {
                // 生成新文件名
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(targetFile);
                string extension = Path.GetExtension(targetFile);
                string directory = Path.GetDirectoryName(targetFile);

                int counter = 1;
                while (File.Exists(targetFile))
                {
                    targetFile = Path.Combine(directory, $"{fileNameWithoutExt}_{counter}{extension}");
                    counter++;
                }
            }

            // 拷贝文件
            File.Copy(sourceFile, targetFile, _overwriteFiles);
            _filesCopied++;

            // 更新进度显示
            Repaint();
        }
        else
        {
            // 拷贝完成
            _isCopying = false;
            EditorApplication.update -= CopyFilesAsync;

            // 刷新AssetDatabase
            AssetDatabase.Refresh();

            Debug.Log($"拷贝完成！共拷贝 {_filesCopied} 个文件到 {_targetFolder}");
        }
    }
    catch (Exception e)
    {
        _isCopying = false;
        EditorApplication.update -= CopyFilesAsync;
        EditorUtility.DisplayDialog("错误", $"拷贝过程中发生错误: {e.Message}", "确定");
    }
}

private void CountFiles()
{
    List<string> extensions = GetSelectedExtensions();

    if (extensions.Count == 0)
    {
        EditorUtility.DisplayDialog("警告", "请至少选择一种文件类型", "确定");
        return;
    }

    string[] allFiles = Directory.GetFiles(_sourceFolder, "*.*", SearchOption.AllDirectories);
    int count = 0;

    foreach (string file in allFiles)
    {
        string ext = Path.GetExtension(file).ToLower();
        if (extensions.Contains(ext))
        {
            count++;
        }
    }

    _totalFiles = count;
    _filesCopied = 0;
    _isCopying = false;

    Repaint();
}

private List<string> GetSelectedExtensions()
{
    List<string> extensions = new List<string>();

    if (_includeImages)
    {
        extensions.AddRange(new[] { ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".psd", ".tiff", ".exr" });
    }

    if (_includeSounds)
    {
        extensions.AddRange(new[] { ".mp3", ".pgg" });
    }

    if (_includeVideos)
    {
        extensions.AddRange(new[] { ".mp4", ".mov", ".avi", ".wmv", ".m4v", ".mpg", ".mpeg" });
    }

    if (_includeText)
    {
        extensions.AddRange(new[] { ".txt", ".json", ".xml", ".csv", ".yaml", ".yml", ".md" });
    }

    if (_includeBinary)
    {
        extensions.AddRange(new[] { ".bytes", ".bin", ".dat" });
    }

    if (_includeAnimations)
    {
        extensions.AddRange(new[] { ".anim", ".fbx", ".ma", ".mb", ".blend", ".max" });
    }

    if (_includePrefabs)
    {
        extensions.AddRange(new[] { ".prefab" });
    }

    if (_includeMaterials)
    {
        extensions.AddRange(new[] { ".mat" });
    }

    if (_includeScenes)
    {
        extensions.AddRange(new[] { ".unity" });
    }

    if (_includeScripts)
    {
        extensions.AddRange(new[] { ".cs", ".js", ".shader", ".cginc", ".hlsl" });
    }

    // 添加自定义扩展名
    if (!string.IsNullOrEmpty(_customExtensions))
    {
        string[] customExts = _customExtensions.Split(',');
        foreach (string ext in customExts)
        {
            string trimmedExt = ext.Trim().ToLower();
            if (!string.IsNullOrEmpty(trimmedExt))
            {
                if (!trimmedExt.StartsWith("."))
                {
                    trimmedExt = "." + trimmedExt;
                }

                if (!extensions.Contains(trimmedExt))
                {
                    extensions.Add(trimmedExt);
                }
            }
        }
    }

    return extensions;
}
}
}
