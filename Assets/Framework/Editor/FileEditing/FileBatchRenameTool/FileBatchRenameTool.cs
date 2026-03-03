using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class FileBatchRenameTool : EditorWindow
{
    private string targetDirectory = "";
    private string searchPattern = "*.*";
    private bool includeSubdirectories = false;
    private List<string> files = new List<string>();
    private Vector2 scrollPosition;

    // 修改方式
    private enum ModifyType
    {
        Add,
        Replace,
        Set
    }

    private ModifyType modifyType = ModifyType.Add;

    // 添加模式设置
    private enum AddPosition
    {
        Prefix,
        Suffix
    }

    private AddPosition addPosition = AddPosition.Prefix;
    private string addText = "";
    private int startIndex = 1;
    private int increment = 1;
    private bool useIndex = false;

    // 替换模式设置
    private string oldText = "";
    private string newText = "";

    // 设置模式设置
    private string setText = "";
    private bool useIndexInSet = false;

    // 文件过滤
    private string filterText = "";
    private bool caseSensitive = false;

    // 预览列表
    private List<FilePreview> previewList = new List<FilePreview>();
    private bool showPreview = true;

    // 列宽设置
    private float checkboxWidth = 25f;
    private float arrowWidth = 40f;
    private float paddingWidth = 10f;

    [MenuItem("Tools/文件批量重命名工具")]
    public static void ShowWindow()
    {
        FileBatchRenameTool window = GetWindow<FileBatchRenameTool>("文件批量重命名");
        window.minSize = new Vector2(900, 600);
    }

    private class FilePreview
    {
        public string originalPath;
        public string originalName;
        public string originalNameWithoutExt;
        public string extension;
        public string newName;
        public bool selected = true;
        public int index;
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
        targetDirectory = EditorGUILayout.TextField(targetDirectory);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择目录", targetDirectory, "");
            if (!string.IsNullOrEmpty(path))
            {
                targetDirectory = path;
                RefreshFileList();
            }
        }

        EditorGUILayout.EndHorizontal();

        // 文件筛选
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("文件筛选:", GUILayout.Width(80));
        searchPattern = EditorGUILayout.TextField(searchPattern);
        EditorGUILayout.LabelField("(例: *.txt;*.cs)", GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();
        includeSubdirectories = EditorGUILayout.Toggle("包含子目录", includeSubdirectories);


        if (files.Count > 0)
        {
            // 文件过滤
            EditorGUILayout.LabelField("文件过滤:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            filterText = EditorGUILayout.TextField("包含文本:", filterText);
            caseSensitive = EditorGUILayout.Toggle("区分大小写", caseSensitive, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            // 文件计数和状态
            EditorGUILayout.HelpBox($"找到 {files.Count} 个文件", MessageType.Info);

            // 刷新按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新文件列表", GUILayout.Height(30), GUILayout.MinWidth(120)))
            {
                RefreshFileList();
            }

            EditorGUILayout.EndHorizontal();

            // 修改方式选择
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("修改方式:", EditorStyles.boldLabel);
            modifyType = (ModifyType)EditorGUILayout.EnumPopup("修改类型", modifyType, GUILayout.MaxWidth(300));

            // 根据修改方式显示不同设置
            switch (modifyType)
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
            showPreview = EditorGUILayout.Foldout(showPreview, "预览");

            if (showPreview)
            {
                DrawPreviewList();
            }

            EditorGUILayout.Space();

            // 执行按钮
            GUI.enabled = previewList.Count > 0 && previewList.Any(p => p.selected);
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
        addPosition = (AddPosition)EditorGUILayout.EnumPopup("添加位置", addPosition, GUILayout.MaxWidth(200));
        addText = EditorGUILayout.TextField("添加文本", addText);
        EditorGUILayout.EndHorizontal();

        useIndex = EditorGUILayout.Toggle("使用序号", useIndex);
        if (useIndex)
        {
            EditorGUILayout.BeginHorizontal();
            startIndex = EditorGUILayout.IntField("起始序号", startIndex, GUILayout.MaxWidth(200));
            increment = EditorGUILayout.IntField("增量", increment, GUILayout.MaxWidth(200));
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawReplaceSettings()
    {
        oldText = EditorGUILayout.TextField("查找内容", oldText);
        newText = EditorGUILayout.TextField("替换为", newText);
    }

    private void DrawSetSettings()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("占位符说明:", GUILayout.MaxWidth(80));
        EditorGUILayout.LabelField("{name} - 原文件名", GUILayout.MaxWidth(120));
        EditorGUILayout.LabelField("{index} - 序号", GUILayout.MaxWidth(100));
        EditorGUILayout.LabelField("{任意文本} - 直接写入花括号内的内容", GUILayout.MaxWidth(200));
        EditorGUILayout.EndHorizontal();

        setText = EditorGUILayout.TextField("文件名格式", setText);
        EditorGUILayout.HelpBox("示例: {name}_{自定义文本}_{index} 在花括号中直接写入想要添加的文本，如：{VIP}、{高清}、{2024}", MessageType.Info);

        EditorGUILayout.Space();
        useIndexInSet = EditorGUILayout.Toggle("使用序号", useIndexInSet);
        if (useIndexInSet)
        {
            EditorGUILayout.BeginHorizontal();
            startIndex = EditorGUILayout.IntField("起始序号", startIndex, GUILayout.MaxWidth(200));
            increment = EditorGUILayout.IntField("增量", increment, GUILayout.MaxWidth(200));
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawPreviewList()
    {
        if (previewList.Count == 0)
        {
            EditorGUILayout.HelpBox("请先生成预览", MessageType.Info);
            return;
        }

        // 计算列宽 - 使用EditorGUIUtility.currentViewWidth确保准确
        float totalWidth = EditorGUIUtility.currentViewWidth - 35f;
        float availableWidth = totalWidth - checkboxWidth - arrowWidth - paddingWidth * 4;
        float columnWidth = availableWidth / 2;

        // 确保最小宽度
        columnWidth = Mathf.Max(columnWidth, 200f);

        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            // 列表头部
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                // 全选复选框
                bool allSelected = previewList.Count > 0 && previewList.All(p => p.selected);
                EditorGUI.showMixedValue = previewList.Count > 0 && previewList.Any(p => p.selected) && !allSelected;
                bool newAllSelected = EditorGUILayout.Toggle(allSelected, GUILayout.Width(checkboxWidth));
                EditorGUI.showMixedValue = false;

                if (newAllSelected != allSelected && previewList.Count > 0)
                {
                    foreach (var preview in previewList)
                    {
                        preview.selected = newAllSelected;
                    }
                }

                // 列标题
                EditorGUILayout.LabelField("原文件名", GUILayout.Width(columnWidth));
                EditorGUILayout.LabelField("", GUILayout.Width(arrowWidth));
                EditorGUILayout.LabelField("新文件名", GUILayout.Width(columnWidth));
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            // 列表内容
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(350));
            {
                for (int i = 0; i < previewList.Count; i++)
                {
                    var preview = previewList[i];

                    // 使用统一的背景样式
                    Color backgroundColor;
                    if (preview.selected)
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
                        bool newSelected = EditorGUILayout.Toggle(preview.selected, GUILayout.Width(checkboxWidth));
                        if (newSelected != preview.selected)
                        {
                            preview.selected = newSelected;
                        }

                        // 原文件名
                        if (preview.originalName != preview.newName)
                        {
                            EditorGUILayout.LabelField(preview.originalName, GUILayout.Width(columnWidth));
                        }
                        else
                        {
                            EditorGUILayout.LabelField(preview.originalName, EditorStyles.miniLabel,
                                GUILayout.Width(columnWidth));
                        }

                        // 箭头
                        if (preview.originalName != preview.newName)
                        {
                            EditorGUILayout.LabelField("→", EditorStyles.centeredGreyMiniLabel,
                                GUILayout.Width(arrowWidth));
                        }
                        else
                        {
                            EditorGUILayout.LabelField("", GUILayout.Width(arrowWidth));
                        }

                        // 新文件名
                        if (preview.originalName != preview.newName)
                        {
                            EditorGUILayout.LabelField(preview.newName, EditorStyles.boldLabel,
                                GUILayout.Width(columnWidth));
                        }
                        else
                        {
                            EditorGUILayout.LabelField(preview.originalName, EditorStyles.miniLabel,
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
                    foreach (var preview in previewList)
                    {
                        preview.selected = true;
                    }
                }

                if (GUILayout.Button("全不选", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    foreach (var preview in previewList)
                    {
                        preview.selected = false;
                    }
                }

                if (GUILayout.Button("反选", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    foreach (var preview in previewList)
                    {
                        preview.selected = !preview.selected;
                    }
                }

                GUILayout.FlexibleSpace();

                int selectedCount = previewList.Count(p => p.selected);
                int changedCount = previewList.Count(p => p.originalName != p.newName && p.selected);
                EditorGUILayout.LabelField($"已选择: {selectedCount} | 将修改: {changedCount}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void RefreshFileList()
    {
        files.Clear();
        previewList.Clear();

        if (string.IsNullOrEmpty(targetDirectory) || !Directory.Exists(targetDirectory))
        {
            Debug.LogWarning("请选择有效的目录");
            return;
        }

        string[] patterns = searchPattern.Split(';');
        SearchOption searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (string pattern in patterns)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                string[] foundFiles = Directory.GetFiles(targetDirectory, pattern.Trim(), searchOption);
                files.AddRange(foundFiles);
            }
        }

        // 去重并排序
        files = files.Distinct().OrderBy(f => f).ToList();

        // 自动生成预览
        GeneratePreview();
    }

    private void GeneratePreview()
    {
        previewList.Clear();

        int currentIndex = startIndex;

        foreach (string file in files)
        {
            var fileName = Path.GetFileName(file);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
            var extension = Path.GetExtension(file);

            // 应用过滤
            if (!string.IsNullOrEmpty(filterText))
            {
                string compareFileName = caseSensitive ? fileName : fileName.ToLower();
                string compareFilter = caseSensitive ? filterText : filterText.ToLower();

                if (!compareFileName.Contains(compareFilter))
                {
                    continue;
                }
            }

            string newFileName = fileName;

            var preview = new FilePreview
            {
                originalPath = file,
                originalName = fileName,
                originalNameWithoutExt = fileNameWithoutExt,
                extension = extension,
                index = currentIndex
            };

            switch (modifyType)
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

            preview.newName = newFileName;
            previewList.Add(preview);

            if (useIndex || useIndexInSet)
            {
                currentIndex += increment;
            }
        }
    }

    private string ApplyAdd(FilePreview preview)
    {
        var addContent = addText;

        if (addPosition == AddPosition.Prefix)
        {
            return useIndex ? preview.index + addContent + preview.originalName : addContent + preview.originalName;
        }

        return useIndex ? preview.originalNameWithoutExt + addContent + preview.index + preview.extension : preview.originalNameWithoutExt + addContent + preview.extension;
    }

    private string ApplyReplace(FilePreview preview)
    {
        string newFileName = preview.originalName.Replace(oldText, newText);
        return newFileName;
    }

    private string ApplySet(FilePreview preview)
    {
        string result = setText;

        // 首先处理特殊占位符 {name} 和 {index}
        result = result.Replace("{name}", preview.originalNameWithoutExt);

        if (useIndexInSet)
        {
            result = result.Replace("{index}", preview.index.ToString());
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
            return preview.originalName;
        }

        return result + preview.extension;
    }

    private void ExecuteRename()
    {
        int successCount = 0;
        int failCount = 0;

        foreach (var preview in previewList)
        {
            if (!preview.selected) continue;

            if (preview.originalName == preview.newName) continue;

            string directory = Path.GetDirectoryName(preview.originalPath);
            string newPath = Path.Combine(directory, preview.newName);

            // 检查目标文件是否已存在
            if (File.Exists(newPath))
            {
                Debug.LogWarning($"目标文件已存在，跳过: {newPath}");
                failCount++;
                continue;
            }

            try
            {
                File.Move(preview.originalPath, newPath);
                successCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"重命名失败: {preview.originalPath} -> {preview.newName}, 错误: {e.Message}");
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