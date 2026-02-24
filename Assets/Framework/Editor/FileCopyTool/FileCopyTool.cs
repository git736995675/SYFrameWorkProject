using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;

public class FileCopyTool : EditorWindow
{
    private string sourceFolder = "";
    private string targetFolder = "";
    private bool copyWithStructure = true;
    private bool overwriteFiles = false;

    // 文件类型选择
    private bool includeImages = true;
    private bool includeVideos = true;
    private bool includeText = true;
    private bool includeBinary = true;
    private bool includeAnimations = true;
    private bool includePrefabs = true;
    private bool includeMaterials = true;
    private bool includeScenes = true;
    private bool includeScripts = true;
    private bool includeSounds = true;

    // 自定义扩展名
    private string customExtensions = "";

    // 进度显示
    private int filesCopied = 0;
    private int totalFiles = 0;
    private bool isCopying = false;

    private Vector2 scrollPosition;

    [MenuItem("Tools/GCZ/文件拷贝工具")]
    public static void ShowWindow()
    {
        GetWindow<FileCopyTool>("文件拷贝工具");
    }

    private void SetInclude(bool isInclude)
    {
         includeImages = isInclude;
         includeVideos = isInclude;
         includeText = isInclude;
         includeBinary = isInclude;
         includeAnimations = isInclude;
         includePrefabs = isInclude;
         includeMaterials = isInclude;
         includeScenes = isInclude;
         includeScripts = isInclude;
         includeSounds = isInclude;
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("文件拷贝工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // 源文件夹选择
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("源文件夹:", GUILayout.Width(80));
        sourceFolder = EditorGUILayout.TextField(sourceFolder);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择源文件夹", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                sourceFolder = path;
            }
        }

        EditorGUILayout.EndHorizontal();

        // 目标文件夹选择
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("目标文件夹:", GUILayout.Width(80));
        targetFolder = EditorGUILayout.TextField(targetFolder);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择目标文件夹", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                targetFolder = path;
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(15);

        // 拷贝选项
        EditorGUILayout.LabelField("拷贝选项:", EditorStyles.boldLabel);
        copyWithStructure = EditorGUILayout.Toggle("按原目录结构拷贝", copyWithStructure, GUILayout.Width(500));
        overwriteFiles = EditorGUILayout.Toggle("覆盖已存在文件", overwriteFiles, GUILayout.Width(500));

        EditorGUILayout.Space(15);

        // 文件类型选择
        EditorGUILayout.LabelField("选择文件类型:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("图片文件 (.png, .jpg, .jpeg, .tga, .bmp, .psd)", GUILayout.Width(300));
        includeImages = EditorGUILayout.Toggle(includeImages);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("音频文件 (.mp3, .ogg)", GUILayout.Width(300));
        includeSounds = EditorGUILayout.Toggle(includeSounds);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("视频文件 (.mp4, .mov, .avi, .wmv)", GUILayout.Width(300));
        includeVideos = EditorGUILayout.Toggle(includeVideos);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("文本文件 (.txt, .json, .xml, .csv)", GUILayout.Width(300));
        includeText = EditorGUILayout.Toggle(includeText);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("二进制文件 (.bytes, .bin)", GUILayout.Width(300));
        includeBinary = EditorGUILayout.Toggle(includeBinary);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("动画文件 (.anim, .fbx, .ma, .mb)", GUILayout.Width(300));
        includeAnimations = EditorGUILayout.Toggle(includeAnimations);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("预制体 (.prefab)", GUILayout.Width(300));
        includePrefabs = EditorGUILayout.Toggle(includePrefabs);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("材质 (.mat)", GUILayout.Width(300));
        includeMaterials = EditorGUILayout.Toggle(includeMaterials);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("场景 (.unity)", GUILayout.Width(300));
        includeScenes = EditorGUILayout.Toggle(includeScenes);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("脚本 (.cs)", GUILayout.Width(300));
        includeScripts = EditorGUILayout.Toggle(includeScripts);
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
        customExtensions = EditorGUILayout.TextField(customExtensions);

        EditorGUILayout.Space(20);

        // 操作按钮
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = !isCopying && Directory.Exists(sourceFolder) && !string.IsNullOrEmpty(targetFolder);

        if (GUILayout.Button("开始拷贝", GUILayout.Height(30)))
        {
            StartCopy();
        }

        GUI.enabled = !isCopying && Directory.Exists(sourceFolder);

        if (GUILayout.Button("统计文件", GUILayout.Width(100), GUILayout.Height(30)))
        {
            CountFiles();
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        // 进度显示
        if (isCopying)
        {
            EditorGUILayout.Space(10);
            Rect rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(rect, (float)filesCopied / totalFiles,
                $"正在拷贝: {filesCopied}/{totalFiles}");

            if (filesCopied >= totalFiles && totalFiles > 0)
            {
                EditorGUILayout.HelpBox($"拷贝完成！共拷贝 {filesCopied} 个文件。", MessageType.Info);
            }
        }
        else if (totalFiles > 0)
        {
            EditorGUILayout.HelpBox($"找到 {totalFiles} 个符合条件的文件", MessageType.Info);
        }

        // 快速操作区域
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("快速操作:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("选择Assets文件夹"))
        {
            sourceFolder = Application.dataPath;
        }

        if (GUILayout.Button("选择Project根目录"))
        {
            sourceFolder = Directory.GetParent(Application.dataPath).FullName;
        }

        if (GUILayout.Button("清空路径"))
        {
            sourceFolder = "";
            targetFolder = "";
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    private void StartCopy()
    {
        if (!Directory.Exists(targetFolder))
        {
            try
            {
                Directory.CreateDirectory(targetFolder);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"无法创建目标文件夹: {e.Message}", "确定");
                return;
            }
        }

        List<string> extensions = GetSelectedExtensions();

        if (extensions.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "请至少选择一种文件类型", "确定");
            return;
        }

        // 开始拷贝
        filesCopied = 0;
        isCopying = true;

        // 在编辑器中异步执行拷贝操作
        EditorApplication.update += CopyFilesAsync;
    }

    private void CopyFilesAsync()
    {
        try
        {
            List<string> extensions = GetSelectedExtensions();
            string[] allFiles = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);
            List<string> filteredFiles = new List<string>();

            // 过滤文件
            foreach (string file in allFiles)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (extensions.Contains(ext))
                {
                    filteredFiles.Add(file);
                }
            }

            totalFiles = filteredFiles.Count;

            if (filesCopied < totalFiles)
            {
                string sourceFile = filteredFiles[filesCopied];
                string relativePath = "";

                if (copyWithStructure)
                {
                    relativePath = sourceFile.Substring(sourceFolder.Length);
                }
                else
                {
                    relativePath = "/" + Path.GetFileName(sourceFile);
                }

                string targetFile = targetFolder + relativePath;
                string targetDir = Path.GetDirectoryName(targetFile);

                // 创建目标目录
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // 检查是否覆盖
                if (File.Exists(targetFile) && !overwriteFiles)
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
                File.Copy(sourceFile, targetFile, overwriteFiles);
                filesCopied++;

                // 更新进度显示
                Repaint();
            }
            else
            {
                // 拷贝完成
                isCopying = false;
                EditorApplication.update -= CopyFilesAsync;

                // 刷新AssetDatabase
                AssetDatabase.Refresh();

                Debug.Log($"拷贝完成！共拷贝 {filesCopied} 个文件到 {targetFolder}");
            }
        }
        catch (Exception e)
        {
            isCopying = false;
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

        string[] allFiles = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);
        int count = 0;

        foreach (string file in allFiles)
        {
            string ext = Path.GetExtension(file).ToLower();
            if (extensions.Contains(ext))
            {
                count++;
            }
        }

        totalFiles = count;
        filesCopied = 0;
        isCopying = false;

        Repaint();
    }

    private List<string> GetSelectedExtensions()
    {
        List<string> extensions = new List<string>();

        if (includeImages)
        {
            extensions.AddRange(new[] { ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".psd", ".tiff", ".exr" });
        }
        
        if (includeSounds)
        {
            extensions.AddRange(new[] { ".mp3", ".pgg" });
        }

        if (includeVideos)
        {
            extensions.AddRange(new[] { ".mp4", ".mov", ".avi", ".wmv", ".m4v", ".mpg", ".mpeg" });
        }

        if (includeText)
        {
            extensions.AddRange(new[] { ".txt", ".json", ".xml", ".csv", ".yaml", ".yml", ".md" });
        }

        if (includeBinary)
        {
            extensions.AddRange(new[] { ".bytes", ".bin", ".dat" });
        }

        if (includeAnimations)
        {
            extensions.AddRange(new[] { ".anim", ".fbx", ".ma", ".mb", ".blend", ".max" });
        }

        if (includePrefabs)
        {
            extensions.AddRange(new[] { ".prefab" });
        }

        if (includeMaterials)
        {
            extensions.AddRange(new[] { ".mat" });
        }

        if (includeScenes)
        {
            extensions.AddRange(new[] { ".unity" });
        }

        if (includeScripts)
        {
            extensions.AddRange(new[] { ".cs", ".js", ".shader", ".cginc", ".hlsl" });
        }

        // 添加自定义扩展名
        if (!string.IsNullOrEmpty(customExtensions))
        {
            string[] customExts = customExtensions.Split(',');
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