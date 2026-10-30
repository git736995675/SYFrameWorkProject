using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using ZSYFramework.Core.Json;

namespace ZSYFramework.DevTools
{
/// <summary>
/// 文件类型清理工具。按类型批量清理资源。
/// </summary>
public class FileTypeCleaner : EditorWindow
{
private string _selectedPath = "";
private string _filePattern = "*.meta";
private readonly List<string> _deletedFiles = new List<string>();
private Vector2 _scrollPosition;
private bool _showDeletedFiles = false;
private int _totalDeleted = 0;
private bool _useRegex = false;
private bool _includeSubdirectories = true;

// 预设文件类型（新增视频和bytes类型）
private readonly Dictionary<string, string> _fileTypePresets = new()
{
    { "Unity Meta Files", "*.meta" },
    { "Video Files", "*.mp4,*.avi,*.mov,*.wmv,*.flv,*.mkv,*.webm,*.m4v" },
    { "Bytes/Cache Files", "*.bytes,*.bin,*.cache,*.dat" },
    { "Temporary Files", "*.tmp,*.temp,~*" },
    { "Log Files", "*.log,*.txt" },
    { "Backup Files", "*.bak,*.backup" },
    { "Image Cache", "*.jpg,*.png,*.cache" },
    { "Unity Temporary", "*.unitypackage.temp,*.crash,*.stackdump" },
    { "Custom Pattern", "" }
};
private string _selectedPreset = "Unity Meta Files";

[MenuItem("Tools/GCZ/文件清理")]
public static void ShowWindow()
{
    GetWindow<FileTypeCleaner>("File Type Cleaner");
}

void OnGUI()
{
    GUILayout.Label("File Type Cleaner", EditorStyles.boldLabel);
    EditorGUILayout.Space(10);

    // 选择目录
    DrawDirectorySelector();
    EditorGUILayout.Space(10);

    // 文件类型设置
    DrawFileTypeSettings();
    EditorGUILayout.Space(10);

    // 高级选项
    DrawAdvancedOptions();
    EditorGUILayout.Space(20);

    // 显示文件统计
    if (!string.IsNullOrEmpty(_selectedPath) && Directory.Exists(_selectedPath))
    {
        DrawFileStatistics();
    }

    // 显示已删除的文件
    if (_totalDeleted > 0)
    {
        DrawDeletedFilesLog();
    }

    EditorGUILayout.Space(20);
    DrawQuickActions();

    // 新增：特殊目录清理
    EditorGUILayout.Space(20);
    DrawSpecialCleanupActions();
}

private void DrawDirectorySelector()
{
    EditorGUILayout.LabelField("Target Directory:", EditorStyles.boldLabel);
    EditorGUILayout.BeginHorizontal();
    GUILayout.Label("Path:", GUILayout.Width(40));
    GUILayout.TextField(_selectedPath, GUILayout.ExpandWidth(true));

    if (GUILayout.Button("Browse", GUILayout.Width(80)))
    {
        string path = EditorUtility.OpenFolderPanel("Select Directory", Application.dataPath, "");
        if (!string.IsNullOrEmpty(path))
        {
            _selectedPath = path;
        }
    }

    if (GUILayout.Button("Paste Path", GUILayout.Width(80)))
    {
        _selectedPath = EditorGUIUtility.systemCopyBuffer;
    }
    EditorGUILayout.EndHorizontal();
}

private void DrawFileTypeSettings()
{
    EditorGUILayout.LabelField("File Type Pattern:", EditorStyles.boldLabel);

    // 预设选择
    EditorGUILayout.BeginHorizontal();
    GUILayout.Label("Preset:", GUILayout.Width(50));

    List<string> presetNames = new List<string>(_fileTypePresets.Keys);
    int selectedIndex = presetNames.IndexOf(_selectedPreset);
    int newIndex = EditorGUILayout.Popup(selectedIndex, presetNames.ToArray());

    if (newIndex != selectedIndex)
    {
        _selectedPreset = presetNames[newIndex];
        _filePattern = _fileTypePresets[_selectedPreset];
    
        // 为新预设添加示例说明
        ShowPresetInfo(_selectedPreset);
    }
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.Space(5);

    // 自定义模式
    if (_selectedPreset == "Custom Pattern")
    {
        EditorGUILayout.HelpBox("Enter file patterns separated by commas. Examples:\n*.tmp,*.temp\nlog_*.txt\nproject?.bak", MessageType.Info);
    }

    EditorGUILayout.BeginHorizontal();
    GUILayout.Label("Pattern:", GUILayout.Width(50));
    _filePattern = EditorGUILayout.TextField(_filePattern);
    EditorGUILayout.EndHorizontal();

    // 新增：显示当前模式的说明
    DrawPatternInfo();
}

private void ShowPresetInfo(string presetName)
{
    switch(presetName)
    {
        case "Video Files":
            EditorGUILayout.HelpBox("常见视频格式：MP4, AVI, MOV, WMV, FLV, MKV, WebM, M4V", MessageType.Info);
            break;
        case "Bytes/Cache Files":
            EditorGUILayout.HelpBox("二进制/缓存文件：常用于资源加载、数据存储、缓存文件", MessageType.Info);
            break;
        case "Unity Temporary":
            EditorGUILayout.HelpBox("Unity临时文件：打包缓存、崩溃日志等", MessageType.Info);
            break;
    }
}

private void DrawPatternInfo()
{
    if (!string.IsNullOrEmpty(_filePattern))
    {
        string[] patterns = _filePattern.Split(',');
        List<string> extensions = new List<string>();
    
        foreach (string pattern in patterns)
        {
            string trimmed = pattern.Trim();
            if (trimmed.StartsWith("*.") && trimmed.Length > 2)
            {
                extensions.Add(trimmed.Substring(2));
            }
        }
    
        if (extensions.Count > 0)
        {
            string info = "将删除以下类型文件: " + string.Join(", ", extensions);
            EditorGUILayout.HelpBox(info, MessageType.None);
        }
    }
}

private void DrawAdvancedOptions()
{
    EditorGUILayout.LabelField("Options:", EditorStyles.boldLabel);

    _includeSubdirectories = EditorGUILayout.Toggle("Include Subdirectories", _includeSubdirectories);
    _useRegex = EditorGUILayout.Toggle("Use Regex Pattern", _useRegex);

    if (_useRegex)
    {
        EditorGUILayout.HelpBox("Regex Examples:\n^\\.meta$\n.*\\.(mp4|avi|mov)$\n^.*\\.bytes$", MessageType.Info);
    }

    // 新增：文件大小筛选
    EditorGUILayout.Space(5);
    DrawSizeFilter();
}

private long minFileSize = 0;
private long maxFileSize = 0;
private bool useSizeFilter = false;

private void DrawSizeFilter()
{
    useSizeFilter = EditorGUILayout.BeginToggleGroup("Filter by File Size", useSizeFilter);

    if (useSizeFilter)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Min Size (KB):", GUILayout.Width(80));
        minFileSize = EditorGUILayout.LongField(minFileSize);
        EditorGUILayout.EndHorizontal();
    
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Max Size (KB):", GUILayout.Width(80));
        maxFileSize = EditorGUILayout.LongField(maxFileSize);
        EditorGUILayout.EndHorizontal();
    
        if (maxFileSize > 0 && minFileSize > maxFileSize)
        {
            EditorGUILayout.HelpBox("最小尺寸不能大于最大尺寸", MessageType.Warning);
        }
    
        // 快速设置按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("> 10MB"))
        {
            minFileSize = 10240; // 10MB in KB
            maxFileSize = 0;
        }
        if (GUILayout.Button("> 100MB"))
        {
            minFileSize = 102400;
            maxFileSize = 0;
        }
        if (GUILayout.Button("< 1MB"))
        {
            minFileSize = 0;
            maxFileSize = 1024;
        }
        EditorGUILayout.EndHorizontal();
    }

    EditorGUILayout.EndToggleGroup();
}

private void DrawFileStatistics()
{
    List<string> matchedFiles = FindFiles(_selectedPath);
    int fileCount = matchedFiles.Count;

    if (fileCount > 0)
    {
        // 计算总大小
        long totalSize = 0;
        foreach (string file in matchedFiles)
        {
            try
            {
                FileInfo info = new FileInfo(file);
                totalSize += info.Length;
            }
            catch { }
        }
    
        string sizeText = FormatFileSize(totalSize);
    
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
    
        EditorGUILayout.LabelField($"Found {fileCount} matching files ({sizeText}):", EditorStyles.boldLabel);
    
        // 按文件类型分组显示
        Dictionary<string, int> typeCount = new Dictionary<string, int>();
        Dictionary<string, long> typeSize = new Dictionary<string, long>();
    
        foreach (string file in matchedFiles)
        {
            string ext = Path.GetExtension(file).ToLower();
            if (string.IsNullOrEmpty(ext)) ext = "no extension";
        
            if (!typeCount.ContainsKey(ext)) typeCount[ext] = 0;
            typeCount[ext]++;
        
            try
            {
                FileInfo info = new FileInfo(file);
                if (!typeSize.ContainsKey(ext)) typeSize[ext] = 0;
                typeSize[ext] += info.Length;
            }
            catch { }
        }
    
        // 显示文件类型统计
        foreach (var kvp in typeCount)
        {
            string ext = kvp.Key;
            int count = kvp.Value;
            long size = typeSize.ContainsKey(ext) ? typeSize[ext] : 0;
        
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{ext}:", GUILayout.Width(60));
            GUILayout.Label($"{count} files", GUILayout.Width(80));
            GUILayout.Label(FormatFileSize(size), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
    
        EditorGUILayout.Space(10);
    
        // 显示前5个最大的文件
        if (matchedFiles.Count > 0)
        {
            List<string> sortedBySize = new List<string>(matchedFiles);
            sortedBySize.Sort((a, b) => 
            {
                try
                {
                    long sizeA = new FileInfo(a).Length;
                    long sizeB = new FileInfo(b).Length;
                    return sizeB.CompareTo(sizeA);
                }
                catch { return 0; }
            });
        
            int previewCount = Mathf.Min(sortedBySize.Count, 5);
            EditorGUILayout.LabelField("Largest files:", EditorStyles.miniBoldLabel);
        
            for (int i = 0; i < previewCount; i++)
            {
                try
                {
                    FileInfo info = new FileInfo(sortedBySize[i]);
                    string fileName = Path.GetFileName(sortedBySize[i]);
                    string fileSize = FormatFileSize(info.Length);
                
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"  • {fileName}", EditorStyles.miniLabel, GUILayout.Width(200));
                    GUILayout.Label(fileSize, EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }
                catch { }
            }
        }
    
        EditorGUILayout.Space(10);
    
        // 删除按钮
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button($"Delete {fileCount} Files ({sizeText})", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Confirm Delete",
                    $"Are you sure you want to delete {fileCount} files?\nTotal size: {sizeText}\nPattern: {_filePattern}\n\nThis action cannot be undone!",
                    "Delete", "Cancel"))
            {
                DeleteFiles(matchedFiles);
            }
        }
        GUI.backgroundColor = Color.white;
    
        EditorGUILayout.EndVertical();
    }
    else
    {
        EditorGUILayout.HelpBox("No files match the specified pattern.", MessageType.Info);
    }
}

private void DrawDeletedFilesLog()
{
    EditorGUILayout.Space(20);
    EditorGUILayout.LabelField($"Deleted {_totalDeleted} files", EditorStyles.boldLabel);

    _showDeletedFiles = EditorGUILayout.Foldout(_showDeletedFiles, "Show Deleted Files");
    if (_showDeletedFiles && _deletedFiles.Count > 0)
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
        foreach (var file in _deletedFiles)
        {
            EditorGUILayout.LabelField(Path.GetFileName(file), EditorStyles.miniLabel);
        }
        EditorGUILayout.EndScrollView();
    }

    if (GUILayout.Button("Clear Log", GUILayout.Width(100)))
    {
        _deletedFiles.Clear();
        _totalDeleted = 0;
    }
}

private void DrawQuickActions()
{
    EditorGUILayout.LabelField("Quick Actions:", EditorStyles.boldLabel);

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Clean Video Files"))
    {
        _selectedPath = Application.dataPath;
        _filePattern = "*.mp4,*.avi,*.mov,*.wmv,*.flv,*.mkv";
        _selectedPreset = "Video Files";
    }

    if (GUILayout.Button("Clean Cache Files"))
    {
        _selectedPath = Application.temporaryCachePath;
        _filePattern = "*.cache,*.tmp,*.temp";
        _selectedPreset = "Temporary Files";
    }

    if (GUILayout.Button("Clean Bytes Files"))
    {
        _selectedPath = Application.dataPath;
        _filePattern = "*.bytes,*.bin,*.dat";
        _selectedPreset = "Bytes/Cache Files";
    }
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Clean Logs"))
    {
        _selectedPath = Application.persistentDataPath;
        _filePattern = "*.log,*.txt";
        _selectedPreset = "Log Files";
    }

    if (GUILayout.Button("Clean Backups"))
    {
        _selectedPath = Application.dataPath.Replace("/Assets", "");
        _filePattern = "*.bak,*.backup";
        _selectedPreset = "Backup Files";
    }

    if (GUILayout.Button("Clean All Temp"))
    {
        _selectedPath = Application.dataPath;
        _filePattern = "*.tmp,*.temp,*.cache,*.bak,*.log";
        _selectedPreset = "Custom Pattern";
    }
    EditorGUILayout.EndHorizontal();
}

private void DrawSpecialCleanupActions()
{
    EditorGUILayout.LabelField("Special Cleanup:", EditorStyles.boldLabel);

    EditorGUILayout.HelpBox("These actions clean specific Unity-related directories", MessageType.Info);

    EditorGUILayout.BeginHorizontal();

    // 清理Library目录
    if (GUILayout.Button("Clean Library Cache"))
    {
        string projectPath = Application.dataPath.Replace("/Assets", "");
        string libraryPath = Path.Combine(projectPath, "Library");
    
        if (Directory.Exists(libraryPath))
        {
            _selectedPath = libraryPath;
            _filePattern = "*.cache,*.temp,*.tmp,*.meta";
            _selectedPreset = "Custom Pattern";
            EditorUtility.DisplayDialog("Library Selected", 
                "Library folder selected. This contains Unity's cache files.\nBe careful when deleting!", "OK");
        }
    }

    // 清理StreamingAssets中的视频
    if (GUILayout.Button("Clean StreamingAssets Videos"))
    {
        string streamingPath = Path.Combine(Application.dataPath, "StreamingAssets");
        if (Directory.Exists(streamingPath))
        {
            _selectedPath = streamingPath;
            _filePattern = "*.mp4,*.avi,*.mov,*.wmv,*.flv";
            _selectedPreset = "Video Files";
        }
    }

    // 清理Resources中的bytes文件
    if (GUILayout.Button("Clean Resources Bytes"))
    {
        string resourcesPath = Path.Combine(Application.dataPath, "Resources");
        if (Directory.Exists(resourcesPath))
        {
            _selectedPath = resourcesPath;
            _filePattern = "*.bytes,*.bin,*.dat";
            _selectedPreset = "Bytes/Cache Files";
        }
    }

    EditorGUILayout.EndHorizontal();

    // 新增：Unity Package Manager缓存清理
    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("Clean Package Cache"))
    {
        string packageCachePath = "";
#if UNITY_EDITOR_WIN
    packageCachePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Unity", "cache");
#elif UNITY_EDITOR_OSX
        packageCachePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Library", "Unity", "cache");
#elif UNITY_EDITOR_LINUX
    packageCachePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), ".config", "unity3d", "cache");
#endif
    
        if (Directory.Exists(packageCachePath))
        {
            _selectedPath = packageCachePath;
            _filePattern = "*.tmp,*.temp,*.cache,*.tar,*.gz";
            _selectedPreset = "Custom Pattern";
        }
    }

    // 清理Build后的残留文件
    if (GUILayout.Button("Clean Build Output"))
    {
        string projectPath = Application.dataPath.Replace("/Assets", "");
        string buildPath = Path.Combine(projectPath, "Build");
    
        if (Directory.Exists(buildPath))
        {
            _selectedPath = buildPath;
            _filePattern = "*.exe,*.apk,*.ipa,*.app,*.dmg,*.pkg";
            _selectedPreset = "Custom Pattern";
        }
    }
    EditorGUILayout.EndHorizontal();
}

private List<string> FindFiles(string path)
{
    List<string> result = new List<string>();

    if (!Directory.Exists(path))
        return result;

    try
    {
        SearchOption searchOption = _includeSubdirectories ? 
            SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        if (_useRegex)
        {
            // 使用正则表达式匹配
            string[] allFiles = Directory.GetFiles(path, "*", searchOption);
            Regex regex = new Regex(_filePattern);
        
            foreach (string file in allFiles)
            {
                if (PassSizeFilter(file))
                {
                    string fileName = Path.GetFileName(file);
                    if (regex.IsMatch(fileName))
                    {
                        result.Add(file);
                    }
                }
            }
        }
        else
        {
            // 使用通配符模式
            string[] patterns = _filePattern.Split(',');
        
            foreach (string pattern in patterns)
            {
                string trimmedPattern = pattern.Trim();
                if (!string.IsNullOrEmpty(trimmedPattern))
                {
                    string[] files = Directory.GetFiles(path, trimmedPattern, searchOption);
                
                    // 应用大小过滤
                    foreach (string file in files)
                    {
                        if (PassSizeFilter(file))
                        {
                            result.Add(file);
                        }
                    }
                }
            }
        
            // 去重
            HashSet<string> uniqueFiles = new HashSet<string>(result);
            result = new List<string>(uniqueFiles);
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Error searching files: {e.Message}");
    }

    return result;
}

private bool PassSizeFilter(string filePath)
{
    if (!useSizeFilter)
        return true;
    
    try
    {
        FileInfo info = new FileInfo(filePath);
        long fileSizeKB = info.Length / 1024;
    
        if (minFileSize > 0 && fileSizeKB < minFileSize)
            return false;
        
        if (maxFileSize > 0 && fileSizeKB > maxFileSize)
            return false;
        
        return true;
    }
    catch
    {
        return false;
    }
}

private void DeleteFiles(List<string> filesToDelete)
{
    _deletedFiles.Clear();
    _totalDeleted = 0;

    int successCount = 0;
    int failCount = 0;
    long totalSize = 0;

    foreach (string file in filesToDelete)
    {
        try
        {
            FileInfo info = new FileInfo(file);
            totalSize += info.Length;
        
            File.Delete(file);
            _deletedFiles.Add(file);
            successCount++;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to delete {file}: {e.Message}");
            failCount++;
        }
    }

    _totalDeleted = successCount;

    string message = $"Deleted {successCount} files successfully.\n";
    message += $"Freed space: {FormatFileSize(totalSize)}";

    if (failCount > 0)
    {
        message += $"\nFailed to delete {failCount} files.";
    }

    Debug.Log(message);

    // 显示详细的统计信息
    ShowDeletionSummary(filesToDelete, successCount, failCount, totalSize);

    Repaint();
}

private void ShowDeletionSummary(List<string> allFiles, int success, int fail, long totalSize)
{
    string summary = $"=== Deletion Summary ===\n";
    summary += $"Success: {success} files\n";
    summary += $"Failed: {fail} files\n";
    summary += $"Space freed: {FormatFileSize(totalSize)}\n\n";

    // 按文件类型统计
    Dictionary<string, int> deletedByType = new Dictionary<string, int>();
    foreach (string file in _deletedFiles)
    {
        string ext = Path.GetExtension(file).ToLower();
        if (string.IsNullOrEmpty(ext)) ext = "no extension";
    
        if (!deletedByType.ContainsKey(ext)) deletedByType[ext] = 0;
        deletedByType[ext]++;
    }

    summary += "Deleted by type:\n";
    foreach (var kvp in deletedByType)
    {
        summary += $"  {kvp.Key}: {kvp.Value} files\n";
    }

    Debug.Log(summary);
    EditorUtility.DisplayDialog("Deletion Complete", 
        $"Deleted {success} files\nFreed {FormatFileSize(totalSize)}", "OK");
}

private string FormatFileSize(long bytes)
{
    if (bytes >= 1024 * 1024 * 1024) // GB
        return $"{(bytes / (1024f * 1024f * 1024f)):F2} GB";
    else if (bytes >= 1024 * 1024) // MB
        return $"{(bytes / (1024f * 1024f)):F2} MB";
    else if (bytes >= 1024) // KB
        return $"{(bytes / 1024f):F2} KB";
    else
        return $"{bytes} bytes";
}
}
}
