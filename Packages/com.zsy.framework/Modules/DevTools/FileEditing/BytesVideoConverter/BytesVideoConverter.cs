using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using UnityEditor;
using UnityEngine;
using ZSYFramework.Core;


namespace ZSYFramework.DevTools
{
/// <summary>
/// 字节数组与视频转换工具。
/// </summary>
public class BytesVideoConverter : EditorWindow
{
private string _sourceFolderPath = "";
private string _targetFolderPath = "";
private ConversionType _conversionType = ConversionType.BytesToVideo;
private VideoFormat _targetFormat = VideoFormat.Mp4;
private bool _overwriteOriginal = false;
private bool _includeSubfolders = true;

private Vector2 _scrollPosition;
private bool _isConverting = false;
private string _statusMessage = "";
private MessageType _statusType = MessageType.None;

private readonly List<string> _supportedVideoExtensions = new List<string>
{
    ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm"
};

private enum ConversionType
{
    BytesToVideo,
    VideoToBytes
}

private enum VideoFormat
{
    Mp4,
    Avi,
    Mov,
    Webm
}

[MenuItem("SY-Tool/文件编辑/Bytes-Video互转")]
public static void ShowWindow()
{
    var window = GetWindow<BytesVideoConverter>("Bytes-Video Converter");
    window.minSize = new Vector2(400, 500);
    window.Show();
}

private void OnGUI()
{
    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

    EditorGUILayout.Space(10);
    EditorGUILayout.LabelField("Bytes ↔ Video Converter", EditorStyles.boldLabel);
    EditorGUILayout.Space(5);

    // Source folder selection
    EditorGUILayout.BeginHorizontal();
    _sourceFolderPath = EditorGUILayout.TextField("Source Folder", _sourceFolderPath);
    if (GUILayout.Button("Browse", GUILayout.Width(80)))
    {
        var path = EditorUtility.OpenFolderPanel("Select Source Folder", Application.dataPath, "");
        if (!string.IsNullOrEmpty(path))
        {
            _sourceFolderPath = path;
        }
    }

    EditorGUILayout.EndHorizontal();

    // Target folder selection (only for non-overwrite mode)
    if (!_overwriteOriginal)
    {
        EditorGUILayout.BeginHorizontal();
        _targetFolderPath = EditorGUILayout.TextField("Target Folder", _targetFolderPath);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Target Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                _targetFolderPath = path;
            }
        }

        EditorGUILayout.EndHorizontal();

        if (string.IsNullOrEmpty(_targetFolderPath))
        {
            EditorGUILayout.HelpBox("Target folder is required when not overwriting original files",
                MessageType.Warning);
        }
    }

    EditorGUILayout.Space(10);

    // Conversion settings
    _conversionType = (ConversionType)EditorGUILayout.EnumPopup("Conversion Type", _conversionType);

    if (_conversionType == ConversionType.BytesToVideo)
    {
        _targetFormat = (VideoFormat)EditorGUILayout.EnumPopup("Target Video Format", _targetFormat);
    }

    _overwriteOriginal = EditorGUILayout.Toggle("Overwrite Original", _overwriteOriginal);
    _includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", _includeSubfolders);

    EditorGUILayout.Space(20);

    // Status display
    if (!string.IsNullOrEmpty(_statusMessage))
    {
        EditorGUILayout.HelpBox(_statusMessage, _statusType);
    }

    // Convert button
    EditorGUI.BeginDisabledGroup(_isConverting || string.IsNullOrEmpty(_sourceFolderPath) ||
                                 (!_overwriteOriginal && string.IsNullOrEmpty(_targetFolderPath)));

    if (GUILayout.Button(_isConverting ? "Converting..." : "Start Conversion",
            GUILayout.Height(40)))
    {
        StartConversion();
    }

    EditorGUI.EndDisabledGroup();

    // Info section
    EditorGUILayout.Space(20);
    EditorGUILayout.LabelField("Information", EditorStyles.boldLabel);
    EditorGUILayout.HelpBox(
        "功能说明：\n" +
        "1. Bytes → Video: 将.bytes文件转换为视频文件\n" +
        "2. Video → Bytes: 将视频文件转换为.bytes文件\n" +
        "3. 支持覆盖原文件或保存到新目录\n" +
        "4. 支持子文件夹递归处理",
        MessageType.Info);

    EditorGUILayout.EndScrollView();
}

private async void StartConversion()
{
    if (_isConverting) return;

    _isConverting = true;
    _statusMessage = "Starting conversion...";
    _statusType = MessageType.Info;
    Repaint();

    try
    {
        SearchOption searchOption =
            _includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        string[] files;

        if (_conversionType == ConversionType.BytesToVideo)
        {
            files = Directory.GetFiles(_sourceFolderPath, "*.bytes", searchOption);
            _statusMessage = $"Found {files.Length} .bytes files";
        }
        else
        {
            files = GetVideoFiles(_sourceFolderPath, searchOption);
            _statusMessage = $"Found {files.Length} video files";
        }

        Repaint();

        if (files.Length == 0)
        {
            _statusMessage = "No files found for conversion";
            _statusType = MessageType.Warning;
            _isConverting = false;
            Repaint();
            return;
        }

        // Process files
        int successCount = 0;
        int failCount = 0;

        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            string progressInfo = $"Processing {Path.GetFileName(file)} ({i + 1}/{files.Length})";

            if (EditorUtility.DisplayCancelableProgressBar(
                    "Converting Files",
                    progressInfo,
                    (float)i / files.Length))
            {
                _statusMessage = "Conversion cancelled by user";
                _statusType = MessageType.Warning;
                break;
            }

            try
            {
                bool result = await ConvertFileAsync(file);

                if (result)
                {
                    successCount++;
                    Debug.Log($"Successfully converted: {file}");
                }
                else
                {
                    failCount++;
                    Debug.LogWarning($"Failed to convert: {file}");
                }
            }
            catch (System.Exception e)
            {
                failCount++;
                Debug.LogError($"Error converting {file}: {e.Message}");
            }
        }

        EditorUtility.ClearProgressBar();

        _statusMessage = $"Conversion completed! Success: {successCount}, Failed: {failCount}";
        _statusType = successCount > 0 ? MessageType.Info : MessageType.Error;

        // Refresh asset database if in Unity project folder
        if (_sourceFolderPath.Contains(Application.dataPath) ||
            (!_overwriteOriginal && _targetFolderPath.Contains(Application.dataPath)))
        {
            AssetDatabase.Refresh();
        }
    }
    catch (System.Exception e)
    {
        _statusMessage = $"Error: {e.Message}";
        _statusType = MessageType.Error;
        Debug.LogError(e);
    }
    finally
    {
        _isConverting = false;
        Repaint();
    }
}

private string[] GetVideoFiles(string folderPath, SearchOption searchOption)
{
    List<string> videoFiles = new List<string>();

    foreach (string extension in _supportedVideoExtensions)
    {
        string[] files = Directory.GetFiles(folderPath, "*" + extension, searchOption);
        videoFiles.AddRange(files);
    }

    return videoFiles.ToArray();
}

private async Task<bool> ConvertFileAsync(string sourceFilePath)
{
    return await Task.Run(() => ConvertFile(sourceFilePath));
}

private bool ConvertFile(string sourceFilePath)
{
    try
    {
        string targetFilePath;
        string targetDirectory;

        // Determine target path
        if (_overwriteOriginal)
        {
            targetDirectory = Path.GetDirectoryName(sourceFilePath);
        }
        else
        {
            // Preserve directory structure relative to source folder
            string relativePath = GetRelativePath(sourceFilePath, _sourceFolderPath);
            targetDirectory = Path.Combine(_targetFolderPath, Path.GetDirectoryName(relativePath));

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }
        }

        if (_conversionType == ConversionType.BytesToVideo)
        {
            // Get target extension
            string extension = GetVideoExtension(_targetFormat);

            // Remove .bytes extension and add video extension
            string fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            targetFilePath = Path.Combine(targetDirectory, fileName + extension);

            // Read bytes and write as video file
            byte[] fileData = File.ReadAllBytes(sourceFilePath);
            File.WriteAllBytes(targetFilePath, fileData);

            Debug.Log($"Converted: {sourceFilePath} -> {targetFilePath}");
        }
        else // VideoToBytes
        {
            // Add .bytes extension
            string fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            targetFilePath = Path.Combine(targetDirectory, fileName + ".bytes");

            // Read video file and write as .bytes
            byte[] fileData = File.ReadAllBytes(sourceFilePath);
            File.WriteAllBytes(targetFilePath, fileData);

            Debug.Log($"Converted: {sourceFilePath} -> {targetFilePath}");
        }

        return true;
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Error converting {sourceFilePath}: {e.Message}");
        return false;
    }
}

private string GetVideoExtension(VideoFormat format)
{
    switch (format)
    {
        case VideoFormat.Mp4: return ".mp4";
        case VideoFormat.Avi: return ".avi";
        case VideoFormat.Mov: return ".mov";
        case VideoFormat.Webm: return ".webm";
        default: return ".mp4";
    }
}

private string GetRelativePath(string fullPath, string basePath)
{
    if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
    {
        basePath += Path.DirectorySeparatorChar;
    }

    Uri baseUri = new Uri(basePath);
    Uri fullUri = new Uri(fullPath);

    return Uri.UnescapeDataString(
        baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
}
}

}
