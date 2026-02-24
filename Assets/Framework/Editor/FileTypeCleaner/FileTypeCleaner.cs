using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class FileTypeCleaner : EditorWindow
{
    private string selectedPath = "";
    private string filePattern = "*.meta";
    private List<string> deletedFiles = new List<string>();
    private Vector2 scrollPosition;
    private bool showDeletedFiles = false;
    private int totalDeleted = 0;
    private bool useRegex = false;
    private bool includeSubdirectories = true;
    
    // 预设文件类型（新增视频和bytes类型）
    private readonly Dictionary<string, string> fileTypePresets = new Dictionary<string, string>()
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
    private string selectedPreset = "Unity Meta Files";

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
        if (!string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath))
        {
            DrawFileStatistics();
        }

        // 显示已删除的文件
        if (totalDeleted > 0)
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
        GUILayout.TextField(selectedPath, GUILayout.ExpandWidth(true));
        
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Directory", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                selectedPath = path;
            }
        }
        
        if (GUILayout.Button("Paste Path", GUILayout.Width(80)))
        {
            selectedPath = EditorGUIUtility.systemCopyBuffer;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawFileTypeSettings()
    {
        EditorGUILayout.LabelField("File Type Pattern:", EditorStyles.boldLabel);
        
        // 预设选择
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Preset:", GUILayout.Width(50));
        
        List<string> presetNames = new List<string>(fileTypePresets.Keys);
        int selectedIndex = presetNames.IndexOf(selectedPreset);
        int newIndex = EditorGUILayout.Popup(selectedIndex, presetNames.ToArray());
        
        if (newIndex != selectedIndex)
        {
            selectedPreset = presetNames[newIndex];
            filePattern = fileTypePresets[selectedPreset];
            
            // 为新预设添加示例说明
            ShowPresetInfo(selectedPreset);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 自定义模式
        if (selectedPreset == "Custom Pattern")
        {
            EditorGUILayout.HelpBox("Enter file patterns separated by commas. Examples:\n*.tmp,*.temp\nlog_*.txt\nproject?.bak", MessageType.Info);
        }
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Pattern:", GUILayout.Width(50));
        filePattern = EditorGUILayout.TextField(filePattern);
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
        if (!string.IsNullOrEmpty(filePattern))
        {
            string[] patterns = filePattern.Split(',');
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
        
        includeSubdirectories = EditorGUILayout.Toggle("Include Subdirectories", includeSubdirectories);
        useRegex = EditorGUILayout.Toggle("Use Regex Pattern", useRegex);
        
        if (useRegex)
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
        List<string> matchedFiles = FindFiles(selectedPath);
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
                    $"Are you sure you want to delete {fileCount} files?\nTotal size: {sizeText}\nPattern: {filePattern}\n\nThis action cannot be undone!",
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
        EditorGUILayout.LabelField($"Deleted {totalDeleted} files", EditorStyles.boldLabel);
        
        showDeletedFiles = EditorGUILayout.Foldout(showDeletedFiles, "Show Deleted Files");
        if (showDeletedFiles && deletedFiles.Count > 0)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            foreach (var file in deletedFiles)
            {
                EditorGUILayout.LabelField(Path.GetFileName(file), EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }
        
        if (GUILayout.Button("Clear Log", GUILayout.Width(100)))
        {
            deletedFiles.Clear();
            totalDeleted = 0;
        }
    }

    private void DrawQuickActions()
    {
        EditorGUILayout.LabelField("Quick Actions:", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clean Video Files"))
        {
            selectedPath = Application.dataPath;
            filePattern = "*.mp4,*.avi,*.mov,*.wmv,*.flv,*.mkv";
            selectedPreset = "Video Files";
        }
        
        if (GUILayout.Button("Clean Cache Files"))
        {
            selectedPath = Application.temporaryCachePath;
            filePattern = "*.cache,*.tmp,*.temp";
            selectedPreset = "Temporary Files";
        }
        
        if (GUILayout.Button("Clean Bytes Files"))
        {
            selectedPath = Application.dataPath;
            filePattern = "*.bytes,*.bin,*.dat";
            selectedPreset = "Bytes/Cache Files";
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clean Logs"))
        {
            selectedPath = Application.persistentDataPath;
            filePattern = "*.log,*.txt";
            selectedPreset = "Log Files";
        }
        
        if (GUILayout.Button("Clean Backups"))
        {
            selectedPath = Application.dataPath.Replace("/Assets", "");
            filePattern = "*.bak,*.backup";
            selectedPreset = "Backup Files";
        }
        
        if (GUILayout.Button("Clean All Temp"))
        {
            selectedPath = Application.dataPath;
            filePattern = "*.tmp,*.temp,*.cache,*.bak,*.log";
            selectedPreset = "Custom Pattern";
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
                selectedPath = libraryPath;
                filePattern = "*.cache,*.temp,*.tmp,*.meta";
                selectedPreset = "Custom Pattern";
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
                selectedPath = streamingPath;
                filePattern = "*.mp4,*.avi,*.mov,*.wmv,*.flv";
                selectedPreset = "Video Files";
            }
        }
        
        // 清理Resources中的bytes文件
        if (GUILayout.Button("Clean Resources Bytes"))
        {
            string resourcesPath = Path.Combine(Application.dataPath, "Resources");
            if (Directory.Exists(resourcesPath))
            {
                selectedPath = resourcesPath;
                filePattern = "*.bytes,*.bin,*.dat";
                selectedPreset = "Bytes/Cache Files";
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
                selectedPath = packageCachePath;
                filePattern = "*.tmp,*.temp,*.cache,*.tar,*.gz";
                selectedPreset = "Custom Pattern";
            }
        }
        
        // 清理Build后的残留文件
        if (GUILayout.Button("Clean Build Output"))
        {
            string projectPath = Application.dataPath.Replace("/Assets", "");
            string buildPath = Path.Combine(projectPath, "Build");
            
            if (Directory.Exists(buildPath))
            {
                selectedPath = buildPath;
                filePattern = "*.exe,*.apk,*.ipa,*.app,*.dmg,*.pkg";
                selectedPreset = "Custom Pattern";
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
            SearchOption searchOption = includeSubdirectories ? 
                SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            if (useRegex)
            {
                // 使用正则表达式匹配
                string[] allFiles = Directory.GetFiles(path, "*", searchOption);
                Regex regex = new Regex(filePattern);
                
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
                string[] patterns = filePattern.Split(',');
                
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
        deletedFiles.Clear();
        totalDeleted = 0;
        
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
                deletedFiles.Add(file);
                successCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete {file}: {e.Message}");
                failCount++;
            }
        }
        
        totalDeleted = successCount;
        
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
        foreach (string file in deletedFiles)
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