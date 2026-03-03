#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Framework.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.FileEditing.FileBatchRenameTool
{
    /// <summary>
    /// 文件批量重命名编辑器
    /// </summary>
    public class FileBatchRenameTool : EditorWindow
    {
        private string _targetDirectory = "";
        private string _searchPattern = "*.*";
        private bool _includeSubdirectories = true;
        private List<string> _files = new();
        private Vector2 _scrollPosition;

        // 修改方式
        private enum ModifyType
        {
            Add,
            Replace,
            Set
        }

        private ModifyType _modifyType = ModifyType.Add;

        // 添加模式设置
        private enum AddPosition
        {
            Prefix,
            Suffix
        }

        private AddPosition _addPosition = AddPosition.Prefix;
        private string _addText = "";
        private int _startIndex = 1;
        private int _increment = 1;
        private bool _useIndex = false;

        // 替换模式设置
        private string _oldText = "";
        private string _newText = "";

        // 设置模式设置
        private string _setText = "";
        private bool _useIndexInSet = false;

        // 文件过滤
        private string _filterText = "";
        private bool _caseSensitive = false;

        // 预览列表
        private readonly List<FilePreview> _previewList = new();
        private bool _showPreview = true;

        // 列宽设置
        private const float CheckboxWidth = 25f;
        private const float ArrowWidth = 40f;
        private const float PaddingWidth = 10f;
        
        private FileTypeFilterUtility.FileTypeFilter _filter = FileTypeFilterUtility.FileTypeFilter.All;


        [MenuItem("SY-Tool/文件编辑/文件批量重命名工具")]
        public static void ShowWindow()
        {
            FileBatchRenameTool window = GetWindow<FileBatchRenameTool>("文件批量重命名");
            window.minSize = new Vector2(900, 800);
            window.maxSize = new Vector2(1920, 1080);
        }

        private class FilePreview
        {
            public string OriginalPath;
            public string OriginalName;
            public string OriginalNameWithoutExt;
            public string Extension;
            public string NewName;
            public bool Selected = true;
            public int Index;
        }

        private void OnGUI()
        {
            // 使用EditorGUIUtility标准间距
            EditorGUIUtility.labelWidth = 120;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("文件批量重命名工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 目录选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("目标目录:", GUILayout.Width(80));
            _targetDirectory = EditorGUILayout.TextField(_targetDirectory);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择目录", _targetDirectory, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _targetDirectory = path;
                    RefreshFileList();
                }
            }
            EditorGUILayout.EndHorizontal();
           
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("文件类型:", GUILayout.Width(80));
            var newFilter = (FileTypeFilterUtility.FileTypeFilter)EditorGUILayout.EnumPopup(_filter,GUILayout.Width(500));
            if (newFilter != _filter)
            {
                _filter = newFilter;
                _searchPattern = FileTypeFilterUtility.GetFilterExtensionsByType(_filter);
            }

            EditorGUILayout.EndHorizontal();

            // 文件筛选
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("文件筛选:", GUILayout.Width(80));
            _searchPattern = EditorGUILayout.TextField(_searchPattern);
            EditorGUILayout.LabelField("(例: *.txt;*.cs)", GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();
            _includeSubdirectories = EditorGUILayout.Toggle("包含子目录", _includeSubdirectories);

            // 刷新按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新文件列表", GUILayout.Height(30), GUILayout.MinWidth(120)))
            {
                RefreshFileList();
            }
            EditorGUILayout.EndHorizontal();
            
            if (_files is { Count: > 0 })
            {
                // 文件过滤
                EditorGUILayout.LabelField("文件过滤:", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                _filterText = EditorGUILayout.TextField("包含文本:", _filterText);
                _caseSensitive = EditorGUILayout.Toggle("区分大小写", _caseSensitive, GUILayout.Width(120));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                // 文件计数和状态
                EditorGUILayout.HelpBox($"找到 {_files.Count} 个文件", MessageType.Info);

                // 修改方式选择
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("修改方式:", EditorStyles.boldLabel);
                _modifyType = (ModifyType)EditorGUILayout.EnumPopup("修改类型", _modifyType, GUILayout.MaxWidth(300));

                // 根据修改方式显示不同设置
                switch (_modifyType)
                {
                    case ModifyType.Add:
                        DrawAddSettings();
                        break;
                    case ModifyType.Replace:
                        DrawReplaceSettings();
                        break;
                    case ModifyType.Set:
                        DrawSetSettings();
                        break;
                }

                EditorGUILayout.Space();

                // 预览控制
                _showPreview = EditorGUILayout.Foldout(_showPreview, "预览");

                if (_showPreview)
                {
                    DrawPreviewList();
                }

                EditorGUILayout.Space();

                // 执行按钮
                GUI.enabled = _previewList.Count > 0 && _previewList.Any(p => p.Selected);
                if (GUILayout.Button("生成预览", GUILayout.Height(40)))
                {
                    GeneratePreview();
                }

                if (GUILayout.Button("执行重命名", GUILayout.Height(40)))
                {
                    ExecuteRename();
                }

                GUI.enabled = true;
            }
        }

        private void DrawAddSettings()
        {
            EditorGUILayout.BeginHorizontal();
            _addPosition = (AddPosition)EditorGUILayout.EnumPopup("添加位置", _addPosition, GUILayout.MaxWidth(200));
            _addText = EditorGUILayout.TextField("添加文本", _addText);
            EditorGUILayout.EndHorizontal();

            _useIndex = EditorGUILayout.Toggle("使用序号", _useIndex);
            if (_useIndex)
            {
                EditorGUILayout.BeginHorizontal();
                _startIndex = EditorGUILayout.IntField("起始序号", _startIndex, GUILayout.MaxWidth(200));
                _increment = EditorGUILayout.IntField("增量", _increment, GUILayout.MaxWidth(200));
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawReplaceSettings()
        {
            _oldText = EditorGUILayout.TextField("查找内容", _oldText);
            _newText = EditorGUILayout.TextField("替换为", _newText);
        }

        private void DrawSetSettings()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("占位符说明:", GUILayout.MaxWidth(80));
            EditorGUILayout.LabelField("{name} - 原文件名", GUILayout.MaxWidth(120));
            EditorGUILayout.LabelField("{index} - 序号", GUILayout.MaxWidth(100));
            EditorGUILayout.LabelField("{任意文本} - 直接写入花括号内的内容", GUILayout.MaxWidth(200));
            EditorGUILayout.EndHorizontal();

            _setText = EditorGUILayout.TextField("文件名格式", _setText);
            EditorGUILayout.HelpBox("示例: {name}_{自定义文本}_{index} 在花括号中直接写入想要添加的文本，如：{VIP}、{高清}、{2024}",
                MessageType.Info);

            EditorGUILayout.Space();
            _useIndexInSet = EditorGUILayout.Toggle("使用序号", _useIndexInSet);
            if (_useIndexInSet)
            {
                EditorGUILayout.BeginHorizontal();
                _startIndex = EditorGUILayout.IntField("起始序号", _startIndex, GUILayout.MaxWidth(200));
                _increment = EditorGUILayout.IntField("增量", _increment, GUILayout.MaxWidth(200));
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawPreviewList()
        {
            if (_previewList.Count == 0)
            {
                EditorGUILayout.HelpBox("请先生成预览", MessageType.Info);
                return;
            }

            // 计算列宽 - 使用EditorGUIUtility.currentViewWidth确保准确
            float totalWidth = EditorGUIUtility.currentViewWidth - 35f;
            float availableWidth = totalWidth - CheckboxWidth - ArrowWidth - PaddingWidth * 4;
            float columnWidth = availableWidth / 2;

            // 确保最小宽度
            columnWidth = Mathf.Max(columnWidth, 200f);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                // 列表头部
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                    // 全选复选框
                    bool allSelected = _previewList.Count > 0 && _previewList.All(p => p.Selected);
                    EditorGUI.showMixedValue =
                        _previewList.Count > 0 && _previewList.Any(p => p.Selected) && !allSelected;
                    bool newAllSelected = EditorGUILayout.Toggle(allSelected, GUILayout.Width(CheckboxWidth));
                    EditorGUI.showMixedValue = false;

                    if (newAllSelected != allSelected && _previewList.Count > 0)
                    {
                        foreach (var preview in _previewList)
                        {
                            preview.Selected = newAllSelected;
                        }
                    }

                    // 列标题
                    EditorGUILayout.LabelField("原文件名", GUILayout.Width(columnWidth));
                    EditorGUILayout.LabelField("", GUILayout.Width(ArrowWidth));
                    EditorGUILayout.LabelField("新文件名", GUILayout.Width(columnWidth));
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                // 列表内容
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(350));
                {
                    for (int i = 0; i < _previewList.Count; i++)
                    {
                        var preview = _previewList[i];

                        // 使用统一的背景样式
                        Color backgroundColor;
                        if (preview.Selected)
                        {
                            backgroundColor = i % 2 == 0
                                ? new Color(0.7f, 0.8f, 1.0f, 0.3f) // 选中行的偶数行：淡蓝色
                                : new Color(0.6f, 0.7f, 0.9f, 0.3f); // 选中行的奇数行：稍深一点的淡蓝色
                        }
                        else
                        {
                            backgroundColor = i % 2 == 0
                                ? new Color(0.9f, 0.9f, 0.9f, 0.2f) // 未选中行的偶数行：浅灰色
                                : new Color(0.8f, 0.8f, 0.8f, 0.2f); // 未选中行的奇数行：稍深一点的灰色
                        }

                        // 创建带背景色的行
                        var originalColor = GUI.backgroundColor;
                        GUI.backgroundColor = backgroundColor;

                        EditorGUILayout.BeginHorizontal(GUI.skin.box);
                        {
                            // 恢复背景色
                            GUI.backgroundColor = originalColor;

                            // 复选框
                            bool newSelected = EditorGUILayout.Toggle(preview.Selected, GUILayout.Width(CheckboxWidth));
                            if (newSelected != preview.Selected)
                            {
                                preview.Selected = newSelected;
                            }

                            // 原文件名
                            if (preview.OriginalName != preview.NewName)
                            {
                                EditorGUILayout.LabelField(preview.OriginalName, GUILayout.Width(columnWidth));
                            }
                            else
                            {
                                EditorGUILayout.LabelField(preview.OriginalName, EditorStyles.miniLabel,
                                    GUILayout.Width(columnWidth));
                            }

                            // 箭头
                            if (preview.OriginalName != preview.NewName)
                            {
                                EditorGUILayout.LabelField("→", EditorStyles.centeredGreyMiniLabel,
                                    GUILayout.Width(ArrowWidth));
                            }
                            else
                            {
                                EditorGUILayout.LabelField("", GUILayout.Width(ArrowWidth));
                            }

                            // 新文件名
                            if (preview.OriginalName != preview.NewName)
                            {
                                EditorGUILayout.LabelField(preview.NewName, EditorStyles.boldLabel,
                                    GUILayout.Width(columnWidth));
                            }
                            else
                            {
                                EditorGUILayout.LabelField(preview.OriginalName, EditorStyles.miniLabel,
                                    GUILayout.Width(columnWidth));
                            }

                            GUILayout.FlexibleSpace();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();

                // 底部工具栏
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                    if (GUILayout.Button("全选", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    {
                        foreach (var preview in _previewList)
                        {
                            preview.Selected = true;
                        }
                    }

                    if (GUILayout.Button("全不选", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    {
                        foreach (var preview in _previewList)
                        {
                            preview.Selected = false;
                        }
                    }

                    if (GUILayout.Button("反选", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    {
                        foreach (var preview in _previewList)
                        {
                            preview.Selected = !preview.Selected;
                        }
                    }

                    GUILayout.FlexibleSpace();

                    int selectedCount = _previewList.Count(p => p.Selected);
                    int changedCount = _previewList.Count(p => p.OriginalName != p.NewName && p.Selected);
                    EditorGUILayout.LabelField($"已选择: {selectedCount} | 将修改: {changedCount}", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void RefreshFileList()
        {
            _files.Clear();
            _previewList.Clear();

            if (string.IsNullOrEmpty(_targetDirectory) || !Directory.Exists(_targetDirectory))
            {
                Debug.LogWarning("请选择有效的目录");
                return;
            }

            string[] patterns = _searchPattern.Split(';');
            SearchOption searchOption =
                _includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (string pattern in patterns)
            {
                if (!string.IsNullOrEmpty(pattern))
                {
                    string[] foundFiles = Directory.GetFiles(_targetDirectory, pattern.Trim(), searchOption);
                    _files.AddRange(foundFiles);
                }
            }

            // 去重并排序
            _files = _files.Distinct().OrderBy(f => f).ToList();

            // 自动生成预览
            GeneratePreview();
        }

        private void GeneratePreview()
        {
            _previewList.Clear();

            var currentIndex = _startIndex;

            foreach (var file in _files)
            {
                var fileName = Path.GetFileName(file);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                var extension = Path.GetExtension(file);

                // 应用过滤
                if (!string.IsNullOrEmpty(_filterText))
                {
                    var compareFileName = _caseSensitive ? fileName : fileName.ToLower();
                    var compareFilter = _caseSensitive ? _filterText : _filterText.ToLower();

                    if (!compareFileName.Contains(compareFilter))
                    {
                        continue;
                    }
                }

                string newFileName = fileName;

                var preview = new FilePreview
                {
                    OriginalPath = file,
                    OriginalName = fileName,
                    OriginalNameWithoutExt = fileNameWithoutExt,
                    Extension = extension,
                    Index = currentIndex
                };

                switch (_modifyType)
                {
                    case ModifyType.Add:
                        newFileName = ApplyAdd(preview);
                        break;
                    case ModifyType.Replace:
                        newFileName = ApplyReplace(preview);
                        break;
                    case ModifyType.Set:
                        newFileName = ApplySet(preview);
                        break;
                }

                preview.NewName = newFileName;
                _previewList.Add(preview);

                if (_useIndex || _useIndexInSet)
                {
                    currentIndex += _increment;
                }
            }
        }

        private string ApplyAdd(FilePreview preview)
        {
            var addContent = _addText;

            if (_addPosition == AddPosition.Prefix)
            {
                return _useIndex
                    ? preview.Index + addContent + preview.OriginalName
                    : addContent + preview.OriginalName;
            }

            return _useIndex
                ? preview.OriginalNameWithoutExt + addContent + preview.Index + preview.Extension
                : preview.OriginalNameWithoutExt + addContent + preview.Extension;
        }

        private string ApplyReplace(FilePreview preview)
        {
            if (string.IsNullOrEmpty(_oldText))
            {
                return preview.OriginalName;
            }

            var newFileName = preview.OriginalName.Replace(_oldText, _newText);
            return newFileName;
        }

        private string ApplySet(FilePreview preview)
        {
            string result = _setText;

            // 首先处理特殊占位符 {name} 和 {index}
            result = result.Replace("{name}", preview.OriginalNameWithoutExt);

            if (_useIndexInSet)
            {
                result = result.Replace("{index}", preview.Index.ToString());
            }
            else
            {
                result = result.Replace("{index}", "");
            }

            // 处理自定义花括号内容：查找所有 {自定义内容} 并提取内容
            string pattern = @"\{([^}]+)\}";
            MatchCollection matches = Regex.Matches(result, pattern);

            foreach (Match match in matches)
            {
                string fullMatch = match.Value;
                string content = match.Groups[1].Value;

                // 跳过已经处理过的特殊占位符
                if (content == "name" || content == "index")
                {
                    continue;
                }

                // 将 {内容} 直接替换为 内容（去掉花括号）
                result = result.Replace(fullMatch, content);
            }

            // 清理多余的空格
            result = Regex.Replace(result, @"\s+", " ").Trim();

            // 如果结果为空，则使用原名称
            if (string.IsNullOrEmpty(result))
            {
                return preview.OriginalName;
            }

            return result + preview.Extension;
        }

        private void ExecuteRename()
        {
            int successCount = 0;
            int failCount = 0;

            foreach (var preview in _previewList)
            {
                if (!preview.Selected) continue;

                if (preview.OriginalName == preview.NewName) continue;

                string directory = Path.GetDirectoryName(preview.OriginalPath);
                string newPath = Path.Combine(directory, preview.NewName);

                // 检查目标文件是否已存在
                if (File.Exists(newPath))
                {
                    Debug.LogWarning($"目标文件已存在，跳过: {newPath}");
                    failCount++;
                    continue;
                }

                try
                {
                    File.Move(preview.OriginalPath, newPath);
                    successCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"重命名失败: {preview.OriginalPath} -> {preview.NewName}, 错误: {e.Message}");
                    failCount++;
                }
            }

            Debug.Log($"重命名完成: 成功 {successCount} 个, 失败 {failCount} 个");

            // 刷新文件列表
            RefreshFileList();

            if (failCount == 0)
            {
                EditorUtility.DisplayDialog("完成", $"成功重命名 {successCount} 个文件", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("完成", $"成功: {successCount}, 失败: {failCount}\n请查看控制台获取详细信息", "确定");
            }
        }
    }
}
#endif