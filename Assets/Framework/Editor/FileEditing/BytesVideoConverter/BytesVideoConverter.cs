using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;

public class BytesVideoConverter : EditorWindow
{
    private string sourceFolderPath = "";
    private string targetFolderPath = "";
    private ConversionType conversionType = ConversionType.BytesToVideo;
    private VideoFormat targetFormat = VideoFormat.MP4;
    private bool overwriteOriginal = false;
    private bool includeSubfolders = true;
    
    private Vector2 scrollPosition;
    private bool isConverting = false;
    private string statusMessage = "";
    private MessageType statusType = MessageType.None;
    
    private List<string> supportedVideoExtensions = new List<string> 
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
        MP4,
        AVI,
        MOV,
        WEBM
    }
    
    [MenuItem("Tools/GCZ/Bytes-Video互转")]
    public static void ShowWindow()
    {
        GetWindow<BytesVideoConverter>("Bytes-Video Converter");
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Bytes ↔ Video Converter", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // Source folder selection
        EditorGUILayout.BeginHorizontal();
        sourceFolderPath = EditorGUILayout.TextField("Source Folder", sourceFolderPath);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Source Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                sourceFolderPath = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // Target folder selection (only for non-overwrite mode)
        if (!overwriteOriginal)
        {
            EditorGUILayout.BeginHorizontal();
            targetFolderPath = EditorGUILayout.TextField("Target Folder", targetFolderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Target Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    targetFolderPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (string.IsNullOrEmpty(targetFolderPath))
            {
                EditorGUILayout.HelpBox("Target folder is required when not overwriting original files", 
                    MessageType.Warning);
            }
        }
        
        EditorGUILayout.Space(10);
        
        // Conversion settings
        conversionType = (ConversionType)EditorGUILayout.EnumPopup("Conversion Type", conversionType);
        
        if (conversionType == ConversionType.BytesToVideo)
        {
            targetFormat = (VideoFormat)EditorGUILayout.EnumPopup("Target Video Format", targetFormat);
        }
        
        overwriteOriginal = EditorGUILayout.Toggle("Overwrite Original", overwriteOriginal);
        includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);
        
        EditorGUILayout.Space(20);
        
        // Status display
        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, statusType);
        }
        
        // Convert button
        EditorGUI.BeginDisabledGroup(isConverting || string.IsNullOrEmpty(sourceFolderPath) || 
            (!overwriteOriginal && string.IsNullOrEmpty(targetFolderPath)));
        
        if (GUILayout.Button(isConverting ? "Converting..." : "Start Conversion", 
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
        if (isConverting) return;
        
        isConverting = true;
        statusMessage = "Starting conversion...";
        statusType = MessageType.Info;
        Repaint();
        
        try
        {
            SearchOption searchOption = includeSubfolders ? 
                SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            
            string[] files;
            
            if (conversionType == ConversionType.BytesToVideo)
            {
                files = Directory.GetFiles(sourceFolderPath, "*.bytes", searchOption);
                statusMessage = $"Found {files.Length} .bytes files";
            }
            else
            {
                files = GetVideoFiles(sourceFolderPath, searchOption);
                statusMessage = $"Found {files.Length} video files";
            }
            
            Repaint();
            
            if (files.Length == 0)
            {
                statusMessage = "No files found for conversion";
                statusType = MessageType.Warning;
                isConverting = false;
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
                    statusMessage = "Conversion cancelled by user";
                    statusType = MessageType.Warning;
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
            
            statusMessage = $"Conversion completed! Success: {successCount}, Failed: {failCount}";
            statusType = successCount > 0 ? MessageType.Info : MessageType.Error;
            
            // Refresh asset database if in Unity project folder
            if (sourceFolderPath.Contains(Application.dataPath) || 
                (!overwriteOriginal && targetFolderPath.Contains(Application.dataPath)))
            {
                AssetDatabase.Refresh();
            }
        }
        catch (System.Exception e)
        {
            statusMessage = $"Error: {e.Message}";
            statusType = MessageType.Error;
            Debug.LogError(e);
        }
        finally
        {
            isConverting = false;
            Repaint();
        }
    }
    
    private string[] GetVideoFiles(string folderPath, SearchOption searchOption)
    {
        List<string> videoFiles = new List<string>();
        
        foreach (string extension in supportedVideoExtensions)
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
            if (overwriteOriginal)
            {
                targetDirectory = Path.GetDirectoryName(sourceFilePath);
            }
            else
            {
                // Preserve directory structure relative to source folder
                string relativePath = GetRelativePath(sourceFilePath, sourceFolderPath);
                targetDirectory = Path.Combine(targetFolderPath, Path.GetDirectoryName(relativePath));
                
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }
            }
            
            if (conversionType == ConversionType.BytesToVideo)
            {
                // Get target extension
                string extension = GetVideoExtension(targetFormat);
                
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
            case VideoFormat.MP4: return ".mp4";
            case VideoFormat.AVI: return ".avi";
            case VideoFormat.MOV: return ".mov";
            case VideoFormat.WEBM: return ".webm";
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