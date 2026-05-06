#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ZSYFramework.Editor.FileEditing.FileInfoExporter
{
    /// <summary>
    /// 文件信息导出工具
    /// </summary>
    public class FileInfoExporter : EditorWindow
    {
        private string _targetPath = "";
        private string _exportPath = "";
        private bool _includeSubfolders = true;
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.Info;

        // 过滤器相关变量
        private FileEditUtility.FileTypeFilter _filter = FileEditUtility.FileTypeFilter.All;
        private string _searchPattern = "*.*";

        // 可导出的信息字段
        private Dictionary<string, bool> _exportOptions;
        private Vector2 _scrollPosition;

        [MenuItem("SY-Tool/文件编辑/文件信息导出工具")]
        public static void ShowWindow()
        {
            var window = GetWindow<FileInfoExporter>("文件信息导出");
            window.minSize = new Vector2(900, 800);
            window.maxSize = new Vector2(1920, 1080);
        }

        private void OnEnable()
        {
            InitializeExportOptions();
        }

        private void InitializeExportOptions()
        {
            _exportOptions = new Dictionary<string, bool>
            {
                { "文件名", true },
                { "相对路径", false },
                { "完整路径", false },
                { "文件扩展名", true },
                { "文件大小(字节)", false },
                { "文件大小(KB/MB/GB)", false },
                { "MD5哈希值", true },
                { "SHA1哈希值", false },
                { "SHA256哈希值", false },
                { "CRC32哈希值", false },
                { "创建时间", false },
                { "最后修改时间", false },
                { "最后访问时间", false },
                { "文件属性", false },
                { "是否为只读", false },
                { "是否为隐藏文件", false }
            };
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUIUtility.labelWidth = 120;
            // 目标路径选择
            DrawPathSelection();

            EditorGUILayout.Space(10);

            // 文件过滤器选择
            DrawFilterSelection();

            EditorGUILayout.Space(10);

            // 导出选项
            DrawExportOptions();

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
            EditorGUILayout.BeginHorizontal();
            var displayPath = string.IsNullOrEmpty(_targetPath) ? "请选择目标目录..." : _targetPath;

            EditorGUILayout.LabelField("目标目录:", GUILayout.Width(80));
            _targetPath = EditorGUILayout.TextField(displayPath);

            if (GUILayout.Button("浏览目录", GUILayout.Width(80)))
            {
                var selectedPath = EditorUtility.OpenFolderPanel("选择目标目录",
                    string.IsNullOrEmpty(_targetPath)
                        ? (Directory.Exists(_targetPath)
                            ? _targetPath
                            : Environment.GetFolderPath(Environment.SpecialFolder.Desktop))
                        : _targetPath, "");

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
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("文件类型:", GUILayout.Width(80));
            EditorGUI.BeginChangeCheck();
            var newFilter = (FileEditUtility.FileTypeFilter)EditorGUILayout.EnumPopup(_filter);
            if (EditorGUI.EndChangeCheck())
            {
                _filter = newFilter;
                _searchPattern = FileEditUtility.GetFilterExtensionsByType(_filter);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("文件筛选:", GUILayout.Width(80));
            _searchPattern = EditorGUILayout.TextField(_searchPattern);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("支持通配符，多个格式用分号分隔\n例如: *.txt;*.cs;*.json", MessageType.Info);

            // 包含子文件夹选项
            _includeSubfolders = EditorGUILayout.Toggle("包含子目录", _includeSubfolders);
        }

        private void DrawExportOptions()
        {
            GUILayout.Label("导出选项:", EditorStyles.label);

            // 全选/取消全选按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", GUILayout.Width(60)))
            {
                SetAllExportOptions(true);
            }

            if (GUILayout.Button("取消全选", GUILayout.Width(80)))
            {
                SetAllExportOptions(false);
            }

            if (GUILayout.Button("恢复默认", GUILayout.Width(80)))
            {
                InitializeExportOptions();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 滚动视图显示所有选项
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            foreach (var option in new List<string>(_exportOptions.Keys))
            {
                _exportOptions[option] = EditorGUILayout.Toggle(option, _exportOptions[option]);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawExportPathSelection()
        {
            GUILayout.Label("导出文件路径:", EditorStyles.label);
            EditorGUILayout.BeginHorizontal();

            _exportPath = EditorGUILayout.TextField(_exportPath);
            if (GUILayout.Button("浏览导出位置", GUILayout.Width(100)))
            {
                var defaultName = "file_info_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                var defaultDir = string.IsNullOrEmpty(_exportPath)
                    ? (string.IsNullOrEmpty(_targetPath)
                        ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                        : _targetPath)
                    : Path.GetDirectoryName(_exportPath);

                var selectedPath = EditorUtility.SaveFilePanel("选择导出位置", defaultDir, defaultName, "csv");

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    _exportPath = selectedPath;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawExportButton()
        {
            var hasSelectedOptions = _exportOptions.Values.Any(option => option);

            var canExport = !string.IsNullOrEmpty(_targetPath) &&
                            Directory.Exists(_targetPath) &&
                            !string.IsNullOrEmpty(_exportPath) &&
                            hasSelectedOptions;

            GUI.enabled = canExport;

            if (GUILayout.Button("开始导出文件信息", GUILayout.Height(40)))
            {
                var currentFilter = _searchPattern;
                ExportFileInfo(currentFilter);
            }

            GUI.enabled = true;

            if (canExport) return;
            if (string.IsNullOrEmpty(_targetPath) || !Directory.Exists(_targetPath))
            {
                EditorGUILayout.HelpBox("请选择有效的目标目录", MessageType.Info);
            }
            else if (!hasSelectedOptions)
            {
                EditorGUILayout.HelpBox("请至少选择一个要导出的信息字段", MessageType.Info);
            }
            else if (string.IsNullOrEmpty(_exportPath))
            {
                EditorGUILayout.HelpBox("请选择导出文件位置", MessageType.Info);
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
            if (string.IsNullOrEmpty(_targetPath) || !Directory.Exists(_targetPath)) return;
            EditorGUILayout.Space(10);
            GUILayout.Label("目录信息:", EditorStyles.boldLabel);

            try
            {
                var dirInfo = new DirectoryInfo(_targetPath);
                EditorGUILayout.LabelField("完整路径:", _targetPath);
                EditorGUILayout.LabelField("最后修改时间:", dirInfo.LastWriteTime.ToString());

                var currentFilter = _searchPattern;
                var option = _includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                try
                {
                    var filters = currentFilter.Split(';');
                    var totalCount = filters.Select(filter => filter.Trim()).Where(trimmedFilter => !string.IsNullOrEmpty(trimmedFilter)).Sum(trimmedFilter => Directory.GetFiles(_targetPath, trimmedFilter, option).Length);

                    EditorGUILayout.LabelField("匹配文件数量:", totalCount.ToString());
                }
                catch
                {
                    EditorGUILayout.LabelField("匹配文件数量:", "无法计算");
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox("读取目录信息时出错: " + ex.Message, MessageType.Warning);
            }
        }

        private void SetAllExportOptions(bool value)
        {
            var keys = new List<string>(_exportOptions.Keys);
            foreach (var key in keys)
            {
                _exportOptions[key] = value;
            }
        }

        private void ExportFileInfo(string filterPattern)
        {
            try
            {
                _statusMessage = "正在扫描文件...";
                _statusType = MessageType.Info;

                // 获取所有文件
                var filters = filterPattern.Split(';');
                var allFiles = new List<string>();

                foreach (var filter in filters)
                {
                    var trimmedFilter = filter.Trim();
                    if (string.IsNullOrEmpty(trimmedFilter)) continue;
                    try
                    {
                        var files = Directory.GetFiles(_targetPath, trimmedFilter,
                            _includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                        allFiles.AddRange(files);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"搜索模式 {trimmedFilter} 时出错: {ex.Message}");
                    }
                }

                var totalFiles = allFiles.Count;

                if (totalFiles == 0)
                {
                    _statusMessage = "没有找到匹配的文件！";
                    _statusType = MessageType.Warning;
                    return;
                }

                _statusMessage = $"找到 {totalFiles} 个文件，正在收集信息...";

                // 创建CSV文件
                using (var writer = new StreamWriter(_exportPath, false, Encoding.UTF8))
                {
                    // 写入表头
                    writer.WriteLine(GenerateHeader());

                    for (var i = 0; i < allFiles.Count; i++)
                    {
                        var filePath = allFiles[i];

                        // 更新进度
                        if (i % 10 == 0 || i == totalFiles - 1)
                        {
                            var progress = (float)i / totalFiles;
                            _statusMessage = $"正在处理: {i + 1}/{totalFiles} - {Path.GetFileName(filePath)}";
                            if (EditorUtility.DisplayCancelableProgressBar("收集文件信息", _statusMessage, progress))
                            {
                                EditorUtility.ClearProgressBar();
                                _statusMessage = "操作已取消";
                                _statusType = MessageType.Warning;
                                return;
                            }
                        }

                        try
                        {
                            // 收集文件信息
                            var fileInfo = CollectFileInfo(filePath);

                            // 生成CSV行
                            var csvLine = GenerateCsvLine(fileInfo);
                            writer.WriteLine(csvLine);
                        }
                        catch (Exception ex)
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
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                _statusMessage = "导出失败: " + ex.Message;
                _statusType = MessageType.Error;
                Debug.LogError(ex);
            }
        }

        private Dictionary<string, string> CollectFileInfo(string filePath)
        {
            var info = new Dictionary<string, string>();
            var fileInfo = new FileInfo(filePath);

            // 文件名
            if (_exportOptions["文件名"])
                info["文件名"] = EscapeCsvField(Path.GetFileNameWithoutExtension(filePath));

            // 相对路径
            if (_exportOptions["相对路径"])
                info["相对路径"] = EscapeCsvField(GetRelativePath(_targetPath, filePath));

            // 完整路径
            if (_exportOptions["完整路径"])
                info["完整路径"] = EscapeCsvField(filePath);

            // 文件扩展名
            if (_exportOptions["文件扩展名"])
                info["文件扩展名"] = EscapeCsvField(Path.GetExtension(filePath).TrimStart('.'));

            // 文件大小(字节)
            if (_exportOptions["文件大小(字节)"])
                info["文件大小(字节)"] = fileInfo.Length.ToString();

            // 文件大小(KB/MB/GB)
            if (_exportOptions["文件大小(KB/MB/GB)"])
                info["文件大小(KB/MB/GB)"] = FormatFileSize(fileInfo.Length);

            // MD5哈希值
            if (_exportOptions["MD5哈希值"])
                info["MD5哈希值"] = FileEditUtility.CalculateHash(filePath, FileEditUtility.HashAlgorithmType.MD5);

            // SHA1哈希值
            if (_exportOptions["SHA1哈希值"])
                info["SHA1哈希值"] = FileEditUtility.CalculateHash(filePath, FileEditUtility.HashAlgorithmType.SHA1);

            // SHA256哈希值
            if (_exportOptions["SHA256哈希值"])
                info["SHA256哈希值"] = FileEditUtility.CalculateHash(filePath, FileEditUtility.HashAlgorithmType.SHA256);

            // CRC32哈希值
            if (_exportOptions["CRC32哈希值"])
                info["CRC32哈希值"] = CalculateCRC32(filePath);

            // 创建时间
            if (_exportOptions["创建时间"])
                info["创建时间"] = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");

            // 最后修改时间
            if (_exportOptions["最后修改时间"])
                info["最后修改时间"] = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

            // 最后访问时间
            if (_exportOptions["最后访问时间"])
                info["最后访问时间"] = fileInfo.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss");

            // 文件属性
            if (_exportOptions["文件属性"])
                info["文件属性"] = fileInfo.Attributes.ToString();

            // 是否为只读
            if (_exportOptions["是否为只读"])
                info["是否为只读"] = fileInfo.IsReadOnly.ToString();

            // 是否为隐藏文件
            if (_exportOptions["是否为隐藏文件"])
                info["是否为隐藏文件"] = ((fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden).ToString();

            return info;
        }

        private string GenerateHeader()
        {
            var headers = new List<string>();

            if (_exportOptions["文件名"]) headers.Add("文件名");
            if (_exportOptions["相对路径"]) headers.Add("相对路径");
            if (_exportOptions["完整路径"]) headers.Add("完整路径");
            if (_exportOptions["文件扩展名"]) headers.Add("文件扩展名");
            if (_exportOptions["文件大小(字节)"]) headers.Add("文件大小(字节)");
            if (_exportOptions["文件大小(KB/MB/GB)"]) headers.Add("文件大小(格式化)");
            if (_exportOptions["MD5哈希值"]) headers.Add("MD5哈希值");
            if (_exportOptions["SHA1哈希值"]) headers.Add("SHA1哈希值");
            if (_exportOptions["SHA256哈希值"]) headers.Add("SHA256哈希值");
            if (_exportOptions["CRC32哈希值"]) headers.Add("CRC32哈希值");
            if (_exportOptions["创建时间"]) headers.Add("创建时间");
            if (_exportOptions["最后修改时间"]) headers.Add("最后修改时间");
            if (_exportOptions["最后访问时间"]) headers.Add("最后访问时间");
            if (_exportOptions["文件属性"]) headers.Add("文件属性");
            if (_exportOptions["是否为只读"]) headers.Add("是否为只读");
            if (_exportOptions["是否为隐藏文件"]) headers.Add("是否为隐藏文件");

            return string.Join(",", headers);
        }

        private string GenerateCsvLine(Dictionary<string, string> fileInfo)
        {
            var values = new List<string>();

            if (_exportOptions["文件名"]) values.Add(fileInfo.ContainsKey("文件名") ? fileInfo["文件名"] : "");
            if (_exportOptions["相对路径"]) values.Add(fileInfo.ContainsKey("相对路径") ? fileInfo["相对路径"] : "");
            if (_exportOptions["完整路径"]) values.Add(fileInfo.ContainsKey("完整路径") ? fileInfo["完整路径"] : "");
            if (_exportOptions["文件扩展名"]) values.Add(fileInfo.ContainsKey("文件扩展名") ? fileInfo["文件扩展名"] : "");
            if (_exportOptions["文件大小(字节)"]) values.Add(fileInfo.ContainsKey("文件大小(字节)") ? fileInfo["文件大小(字节)"] : "");
            if (_exportOptions["文件大小(KB/MB/GB)"]) values.Add(fileInfo.ContainsKey("文件大小(KB/MB/GB)") ? fileInfo["文件大小(KB/MB/GB)"] : "");
            if (_exportOptions["MD5哈希值"]) values.Add(fileInfo.ContainsKey("MD5哈希值") ? fileInfo["MD5哈希值"] : "");
            if (_exportOptions["SHA1哈希值"]) values.Add(fileInfo.ContainsKey("SHA1哈希值") ? fileInfo["SHA1哈希值"] : "");
            if (_exportOptions["SHA256哈希值"]) values.Add(fileInfo.ContainsKey("SHA256哈希值") ? fileInfo["SHA256哈希值"] : "");
            if (_exportOptions["CRC32哈希值"]) values.Add(fileInfo.ContainsKey("CRC32哈希值") ? fileInfo["CRC32哈希值"] : "");
            if (_exportOptions["创建时间"]) values.Add(fileInfo.ContainsKey("创建时间") ? fileInfo["创建时间"] : "");
            if (_exportOptions["最后修改时间"]) values.Add(fileInfo.ContainsKey("最后修改时间") ? fileInfo["最后修改时间"] : "");
            if (_exportOptions["最后访问时间"]) values.Add(fileInfo.ContainsKey("最后访问时间") ? fileInfo["最后访问时间"] : "");
            if (_exportOptions["文件属性"]) values.Add(fileInfo.ContainsKey("文件属性") ? fileInfo["文件属性"] : "");
            if (_exportOptions["是否为只读"]) values.Add(fileInfo.ContainsKey("是否为只读") ? fileInfo["是否为只读"] : "");
            if (_exportOptions["是否为隐藏文件"]) values.Add(fileInfo.ContainsKey("是否为隐藏文件") ? fileInfo["是否为隐藏文件"] : "");

            return string.Join(",", values);
        }


        private static string CalculateCRC32(string filePath)
        {
            var sb = new StringBuilder();
            try
            {
                using var stream = File.OpenRead(filePath);
                var crc32 = new Crc32();
                var hash = crc32.ComputeHash(stream);
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
            catch
            {
                return "Error";
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            var order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
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
            if (string.IsNullOrEmpty(field))
                return "";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }
    }

    // CRC32算法实现
    public class Crc32 : HashAlgorithm
    {
        private const uint DefaultPolynomial = 0xedb88320;
        private const uint DefaultSeed = 0xffffffff;

        private uint _hash;
        private readonly uint _seed;
        private readonly uint[] _table;
        private static uint[] _defaultTable;

        public Crc32() : this(DefaultPolynomial, DefaultSeed)
        {
        }

        private Crc32(uint polynomial, uint seed)
        {
            _table = InitializeTable(polynomial);
            this._seed = seed;
            Initialize();
        }

        public override void Initialize()
        {
            _hash = _seed;
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _hash = CalculateHash(_table, _hash, array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            var hashBuffer = UintToBigEndianBytes(~_hash);
            HashValue = hashBuffer;
            return hashBuffer;
        }

        public override int HashSize => 32;

        private static uint[] InitializeTable(uint polynomial)
        {
            if (polynomial == DefaultPolynomial && _defaultTable != null)
                return _defaultTable;

            var createTable = new uint[256];
            for (var i = 0; i < 256; i++)
            {
                var entry = (uint)i;
                for (var j = 0; j < 8; j++)
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ polynomial;
                    else
                        entry >>= 1;
                createTable[i] = entry;
            }

            if (polynomial == DefaultPolynomial)
                _defaultTable = createTable;

            return createTable;
        }

        private static uint CalculateHash(uint[] table, uint seed, byte[] buffer, int start, int size)
        {
            var crc = seed;
            for (var i = start; i < start + size; i++)
                crc = (crc >> 8) ^ table[(buffer[i] ^ crc) & 0xff];
            return crc;
        }

        private static byte[] UintToBigEndianBytes(uint x)
        {
            return new byte[]
            {
                (byte)((x >> 24) & 0xff),
                (byte)((x >> 16) & 0xff),
                (byte)((x >> 8) & 0xff),
                (byte)(x & 0xff)
            };
        }
    }
}

#endif