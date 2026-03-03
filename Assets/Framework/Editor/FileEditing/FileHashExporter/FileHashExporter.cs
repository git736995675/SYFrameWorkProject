using UnityEngine;
using UnityEditor;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

public class FileHashExporter : EditorWindow
{
    private string _targetPath = "";
    private string _exportPath = "";
    private bool _includeSubfolders = true;
    private Vector2 _scrollPosition;
    private string _statusMessage = "";
    private MessageType _statusType = MessageType.Info;
    
    // 过滤器相关变量
    private string[] _filterNames;
    private string[] _filterPatterns;
    private int _selectedFilterIndex = 0;
    private string _customFilter = "*.*";
    private bool _useCustomFilter = false;

    public FileHashExporter(string[] filterNames)
    {
        this._filterNames = filterNames;
    }

    [MenuItem("Tools/文件哈希值导出工具")]
    public static void ShowWindow()
    {
        GetWindow<FileHashExporter>("文件哈希导出工具");
    }

    private void OnEnable()
    {
        // 初始化过滤器选项
        InitializeFilters();
    }

    private void InitializeFilters()
    {
        // 预定义的过滤器
        _filterNames = new string[]
        {
            "所有文件",
            "图片文件",
            "文档文件",
            "脚本文件",
            "Unity资源",
            "音频文件",
            "视频文件",
            "压缩文件",
            "可执行文件"
        };

        _filterPatterns = new string[]
        {
            "*.*",
            "*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.tga;*.psd",
            "*.txt;*.doc;*.docx;*.pdf;*.xls;*.xlsx;*.ppt;*.pptx",
            "*.cs;*.js;*.py;*.cpp;*.h;*.java;*.lua",
            "*.prefab;*.asset;*.unity;*.mat;*.fbx;*.obj",
            "*.mp3;*.wav;*.ogg;*.aif;*.flac",
            "*.mp4;*.avi;*.mov;*.wmv;*.mkv",
            "*.zip;*.rar;*.7z;*.tar;*.gz",
            "*.exe;*.dll;*.msi;*.bat;*.sh"
        };
    }

    private void OnGUI()
    {
        GUILayout.Label("文件哈希值导出工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // 目标路径选择
        DrawPathSelection();

        EditorGUILayout.Space(10);

        // 文件过滤器选择
        DrawFilterSelection();

        EditorGUILayout.Space(10);

        // 选项设置
        DrawOptions();

        EditorGUILayout.Space(10);

        // 导出路径选择
        DrawExportPathSelection();

        EditorGUILayout.Space(20);

        // 导出按钮
        DrawExportButton();

        EditorGUILayout.Space(10);

        // 状态信息显示
        DrawStatusMessage();

        // 目录信息显示
        DrawDirectoryInfo();
    }

    private void DrawPathSelection()
    {
        GUILayout.Label("目标目录:", EditorStyles.label);
        EditorGUILayout.BeginHorizontal();
        
        string displayPath = string.IsNullOrEmpty(_targetPath) ? "请选择要扫描的目录..." : _targetPath;
        if (displayPath.Length > 50)
        {
            displayPath = "..." + displayPath.Substring(displayPath.Length - 47);
        }
        
        EditorGUILayout.LabelField(displayPath, EditorStyles.textField, GUILayout.Height(20));
        
        if (GUILayout.Button("浏览目录", GUILayout.Width(80)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择目标目录", 
                string.IsNullOrEmpty(_targetPath) ? 
                (Directory.Exists(_targetPath) ? _targetPath : System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)) : 
                _targetPath, "");
            
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _targetPath = selectedPath;
                _statusMessage = "";
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawFilterSelection()
    {
        GUILayout.Label("文件过滤器:", EditorStyles.label);
        
        // 过滤器类型选择
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("使用预设:", GUILayout.Width(70));
        
        EditorGUI.BeginChangeCheck();
        bool newUseCustomFilter = EditorGUILayout.Toggle(_useCustomFilter, GUILayout.Width(20));
        if (EditorGUI.EndChangeCheck())
        {
            _useCustomFilter = newUseCustomFilter;
        }
        
        EditorGUILayout.LabelField(_useCustomFilter ? "自定义" : "预设过滤器", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        if (!_useCustomFilter)
        {
            // 预设过滤器下拉菜单
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("选择类型:", GUILayout.Width(70));
            
            EditorGUI.BeginChangeCheck();
            int newSelectedIndex = EditorGUILayout.Popup(_selectedFilterIndex, _filterNames);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedFilterIndex = newSelectedIndex;
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 显示当前选择的过滤器模式
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("当前模式:", GUILayout.Width(70));
            EditorGUILayout.LabelField(_filterPatterns[_selectedFilterIndex], EditorStyles.textField);
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            // 自定义过滤器输入
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("自定义模式:", GUILayout.Width(70));
            _customFilter = EditorGUILayout.TextField(_customFilter);
            
            if (GUILayout.Button("重置", GUILayout.Width(50)))
            {
                _customFilter = "*.*";
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("支持通配符，多个格式用分号分隔\n例如: *.txt;*.cs;*.json", MessageType.Info);
        }
    }

    private void DrawOptions()
    {
        GUILayout.Label("选项:", EditorStyles.label);
        _includeSubfolders = EditorGUILayout.Toggle("包含子文件夹", _includeSubfolders);
    }

    private void DrawExportPathSelection()
    {
        GUILayout.Label("导出文件路径:", EditorStyles.label);
        EditorGUILayout.BeginHorizontal();
        
        string displayExportPath = string.IsNullOrEmpty(_exportPath) ? "请选择导出文件位置..." : _exportPath;
        if (displayExportPath.Length > 50)
        {
            displayExportPath = "..." + displayExportPath.Substring(displayExportPath.Length - 47);
        }
        
        EditorGUILayout.LabelField(displayExportPath, EditorStyles.textField, GUILayout.Height(20));
        
        if (GUILayout.Button("浏览导出位置", GUILayout.Width(100)))
        {
            string defaultName = "file_hashes_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
            string defaultDir = string.IsNullOrEmpty(_exportPath) ? 
                (string.IsNullOrEmpty(_targetPath) ? System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) : _targetPath) : 
                Path.GetDirectoryName(_exportPath);
            
            string selectedPath = EditorUtility.SaveFilePanel("选择导出位置", 
                defaultDir, defaultName, "csv");
                
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _exportPath = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawExportButton()
    {
        bool canExport = !string.IsNullOrEmpty(_targetPath) && 
                        Directory.Exists(_targetPath) && 
                        !string.IsNullOrEmpty(_exportPath);
        
        GUI.enabled = canExport;
        
        if (GUILayout.Button("开始导出哈希值", GUILayout.Height(40)))
        {
            string currentFilter = _useCustomFilter ? _customFilter : _filterPatterns[_selectedFilterIndex];
            ExportFileHashes(currentFilter);
        }
        
        GUI.enabled = true;

        if (!canExport)
        {
            if (string.IsNullOrEmpty(_targetPath) || !Directory.Exists(_targetPath))
            {
                EditorGUILayout.HelpBox("请选择有效的目标目录", MessageType.Info);
            }
            else if (string.IsNullOrEmpty(_exportPath))
            {
                EditorGUILayout.HelpBox("请选择导出文件位置", MessageType.Info);
            }
        }
    }

    private void DrawStatusMessage()
    {
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            EditorGUILayout.HelpBox(_statusMessage, _statusType);
        }
    }

    private void DrawDirectoryInfo()
    {
        if (!string.IsNullOrEmpty(_targetPath) && Directory.Exists(_targetPath))
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("目录信息:", EditorStyles.boldLabel);
            
            try
            {
                var dirInfo = new DirectoryInfo(_targetPath);
                EditorGUILayout.LabelField("完整路径:", _targetPath);
                EditorGUILayout.LabelField("最后修改时间:", dirInfo.LastWriteTime.ToString());
                
                // 预览文件数量
                string currentFilter = _useCustomFilter ? _customFilter : _filterPatterns[_selectedFilterIndex];
                SearchOption option = _includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                
                try
                {
                    string[] filters = currentFilter.Split(';');
                    int totalCount = 0;
                    
                    foreach (string filter in filters)
                    {
                        string trimmedFilter = filter.Trim();
                        if (!string.IsNullOrEmpty(trimmedFilter))
                        {
                            totalCount += Directory.GetFiles(_targetPath, trimmedFilter, option).Length;
                        }
                    }
                    
                    EditorGUILayout.LabelField("匹配文件数量:", totalCount.ToString());
                }
                catch
                {
                    EditorGUILayout.LabelField("匹配文件数量:", "无法计算");
                }
            }
            catch (System.Exception ex)
            {
                EditorGUILayout.HelpBox("读取目录信息时出错: " + ex.Message, MessageType.Warning);
            }
        }
    }

    private void ExportFileHashes(string filterPattern)
    {
        try
        {
            _statusMessage = "正在扫描文件...";
            _statusType = MessageType.Info;
            
            // 获取所有文件
            var filters = filterPattern.Split(';');
            var allFiles = new List<string>();
            
            foreach (string filter in filters)
            {
                string trimmedFilter = filter.Trim();
                if (!string.IsNullOrEmpty(trimmedFilter))
                {
                    try
                    {
                        string[] files = Directory.GetFiles(_targetPath, trimmedFilter, 
                            _includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                        allFiles.AddRange(files);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"搜索模式 {trimmedFilter} 时出错: {ex.Message}");
                    }
                }
            }

            int totalFiles = allFiles.Count;
            
            if (totalFiles == 0)
            {
                _statusMessage = "没有找到匹配的文件！";
                _statusType = MessageType.Warning;
                return;
            }

            _statusMessage = $"找到 {totalFiles} 个文件，正在计算哈希值...";
            
            // 创建CSV文件
            using (StreamWriter writer = new StreamWriter(_exportPath, false, Encoding.UTF8))
            {
                // 写入表头
                writer.WriteLine("文件名,相对路径,完整路径,MD5哈希值,文件大小(字节),最后修改时间");
                
                for (int i = 0; i < allFiles.Count; i++)
                {
                    string filePath = allFiles[i];
                    
                    // 更新进度
                    if (i % 10 == 0 || i == totalFiles - 1)
                    {
                        float progress = (float)i / totalFiles;
                        _statusMessage = $"正在处理: {i + 1}/{totalFiles} - {Path.GetFileName(filePath)}";
                        if (EditorUtility.DisplayCancelableProgressBar("计算哈希值", _statusMessage, progress))
                        {
                            EditorUtility.ClearProgressBar();
                            _statusMessage = "操作已取消";
                            _statusType = MessageType.Warning;
                            return;
                        }
                    }

                    try
                    {
                        // 计算MD5哈希值
                        string hash = CalculateMD5(filePath);
                        FileInfo fileInfo = new FileInfo(filePath);
                        
                        // 获取相对路径（相对于目标目录）
                        string relativePath = GetRelativePath(_targetPath, filePath);
                        
                        // 转义CSV中的逗号
                        string fileName = EscapeCsvField(Path.GetFileName(filePath));
                        string escapedRelativePath = EscapeCsvField(relativePath);
                        string escapedFullPath = EscapeCsvField(filePath);
                        
                        // 写入数据
                        writer.WriteLine($"{fileName},{escapedRelativePath},{escapedFullPath},{hash},{fileInfo.Length},{fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"无法处理文件 {filePath}: {ex.Message}");
                    }
                }
            }

            EditorUtility.ClearProgressBar();
            
            _statusMessage = $"导出完成！共处理 {totalFiles} 个文件。\n文件保存至: {_exportPath}";
            _statusType = MessageType.Info;
            
            // 在资源管理器中显示文件
            EditorUtility.RevealInFinder(_exportPath);
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            _statusMessage = "导出失败: " + ex.Message;
            _statusType = MessageType.Error;
            Debug.LogError(ex);
        }
    }

    private string CalculateMD5(string filename)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filename))
            {
                byte[] hash = md5.ComputeHash(stream);
                StringBuilder sb = new StringBuilder();
                
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                
                return sb.ToString();
            }
        }
    }

    private string GetRelativePath(string basePath, string fullPath)
    {
        if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            basePath += Path.DirectorySeparatorChar;
        }
        
        return fullPath.Replace(basePath, "");
    }

    private string EscapeCsvField(string field)
    {
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
        return field;
    }
}