using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace EditorTools
{
    public class EnhancedBatchRenamer : EditorWindow
    {
        // 工具状态
        private enum ToolState
        {
            Ready,
            Modified,
            Reverted
        }

        // 文件类型筛选
        private enum FileTypeFilter
        {
            All,
            Images,
            Videos,
            Scripts,
            TextFiles,
            Table,
            Prefabs,
            Materials,
            Custom
        }

        // 改名规则
        private enum RenameRule
        {
            ReplaceText,
            AddPrefix,
            AddSuffix,
            RemoveText,
            ChangeCase,
            AddNumbering,
            RegexReplace
        }

        // 文件信息类
        private class FileInfo
        {
            public string OriginalPath { get; set; }
            public string CurrentPath { get; set; }
            public string OriginalName { get; set; }
            public string CurrentName { get; set; }
            public bool IsModified { get; set; }
            public bool IsSelected { get; set; }
            public string FileExtension { get; set; }
            public bool IsEditing { get; set; }  // 新增：是否正在编辑状态
            public string EditText { get; set; }  // 新增：编辑框文本
        }

        // 序列化数据类
        [System.Serializable]
        private class RenameData
        {
            public List<FileData> files = new List<FileData>();
            public string originalFolderPath;
        }

        [System.Serializable]
        private class FileData
        {
            public string originalPath;
            public string newPath;
        }

        // UI变量
        private string targetFolderPath = "";
        private FileTypeFilter currentFilter = FileTypeFilter.All;
        private string customExtension = "";
        private RenameRule currentRule = RenameRule.ReplaceText;
        
        // 规则参数
        private string searchText = "";
        private string replaceText = "";
        private string prefixText = "";
        private string suffixText = "";
        private string regexPattern = "";
        private string regexReplacement = "";
        private bool toUpperCase = true;
        private int startNumber = 1;
        private int numberPadding = 2;
        
        // 列表控制
        private bool selectAll = false;
        
        // 数据
        private List<FileInfo> fileList = new List<FileInfo>();
        private List<FileInfo> filteredList = new List<FileInfo>();
        private ToolState currentState = ToolState.Ready;
        private Vector2 scrollPosition;
        private RenameData renameHistory;
        private string historyPath;
        
        // 编辑状态
        private int editingFileIndex = -1;
        private string editingTempName = "";
        
        // 文件类型映射
        private Dictionary<FileTypeFilter, string[]> fileTypeExtensions = new Dictionary<FileTypeFilter, string[]>()
        {
            { FileTypeFilter.Images, new string[] { ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".tif", ".tiff", ".psd", ".gif" } },
            { FileTypeFilter.Videos, new string[] { ".mp4", ".mov", ".avi", ".mpg", ".mpeg", ".wmv", ".webm" } },
            { FileTypeFilter.Scripts, new string[] { ".cs", ".js", ".shader", ".cginc", ".hlsl" } },
            { FileTypeFilter.TextFiles, new string[] { ".txt", ".json", ".xml", ".yml", ".yaml", ".md", ".html", ".csv" } },
            { FileTypeFilter.Table, new string[] { ".csv", ".xlsx", ".xls" } },
            { FileTypeFilter.Prefabs, new string[] { ".prefab" } },
            { FileTypeFilter.Materials, new string[] { ".mat" } },
            { FileTypeFilter.All, new string[] { ".*" } },
            { FileTypeFilter.Custom, new string[] { } }
        };

        [MenuItem("Tools/GCZ/批量改名工具")]
        public static void ShowWindow()
        {
            EnhancedBatchRenamer window = GetWindow<EnhancedBatchRenamer>("批量改名工具");
            window.minSize = new Vector2(600, 700);
            window.LoadHistory();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // 标题
            EditorGUILayout.LabelField("批量改名工具 v1.6", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            // 目标目录选择
            DrawFolderSelection();
            EditorGUILayout.Space(15);
            
            // 文件筛选
            DrawFileFilter();
            EditorGUILayout.Space(15);
            
            // 改名规则
            DrawRenameRules();
            EditorGUILayout.Space(15);
            
            // 操作按钮
            DrawActionButtons();
            EditorGUILayout.Space(15);
            
            // 文件列表控制
            DrawListControls();
            EditorGUILayout.Space(10);
            
            // 文件列表
            DrawFileList();
            
            // 状态显示
            DrawStatusBar();
        }

        private void DrawFolderSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("目标目录", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            targetFolderPath = EditorGUILayout.TextField("目录路径", targetFolderPath);
            
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string newPath = EditorUtility.OpenFolderPanel("选择目标目录", targetFolderPath, "");
                if (!string.IsNullOrEmpty(newPath))
                {
                    // 移除Assets目录限制，可以操作任何目录
                    targetFolderPath = newPath;
                    ScanFiles();
                }
            }
            
            if (GUILayout.Button("扫描", GUILayout.Width(60)))
            {
                ScanFiles();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawFileFilter()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("文件筛选", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("文件类型:", GUILayout.Width(80));
            var newFilter = (FileTypeFilter)EditorGUILayout.EnumPopup(currentFilter);
            if (newFilter != currentFilter)
            {
                currentFilter = newFilter;
                ScanFiles();
            }
            EditorGUILayout.EndHorizontal();
            
            if (currentFilter == FileTypeFilter.Custom)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("自定义后缀:", GUILayout.Width(80));
                customExtension = EditorGUILayout.TextField(customExtension);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("请输入后缀名，如 .asset 或 .fbx，多个后缀用逗号分隔", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawRenameRules()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("改名规则", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("规则类型:", GUILayout.Width(80));
            currentRule = (RenameRule)EditorGUILayout.EnumPopup(currentRule);
            EditorGUILayout.EndHorizontal();
            
            switch (currentRule)
            {
                case RenameRule.ReplaceText:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("查找:", GUILayout.Width(60));
                    searchText = EditorGUILayout.TextField(searchText);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("替换为:", GUILayout.Width(60));
                    replaceText = EditorGUILayout.TextField(replaceText);
                    EditorGUILayout.EndHorizontal();
                    break;
                    
                case RenameRule.AddPrefix:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("前缀:", GUILayout.Width(60));
                    prefixText = EditorGUILayout.TextField(prefixText);
                    EditorGUILayout.EndHorizontal();
                    break;
                    
                case RenameRule.AddSuffix:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("后缀:", GUILayout.Width(60));
                    suffixText = EditorGUILayout.TextField(suffixText);
                    EditorGUILayout.EndHorizontal();
                    break;
                    
                case RenameRule.RemoveText:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("移除文本:", GUILayout.Width(80));
                    searchText = EditorGUILayout.TextField(searchText);
                    EditorGUILayout.EndHorizontal();
                    break;
                    
                case RenameRule.ChangeCase:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("转换为:", GUILayout.Width(80));
                    toUpperCase = EditorGUILayout.ToggleLeft("大写", toUpperCase, GUILayout.Width(80));
                    toUpperCase = !EditorGUILayout.ToggleLeft("小写", !toUpperCase, GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                    break;
                    
                case RenameRule.AddNumbering:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("起始数字:", GUILayout.Width(80));
                    startNumber = EditorGUILayout.IntField(startNumber);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("数字位数:", GUILayout.Width(80));
                    numberPadding = EditorGUILayout.IntSlider(numberPadding, 1, 5);
                    EditorGUILayout.EndHorizontal();
                    break;
                    
                case RenameRule.RegexReplace:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("正则表达式:", GUILayout.Width(80));
                    regexPattern = EditorGUILayout.TextField(regexPattern);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("替换内容:", GUILayout.Width(80));
                    regexReplacement = EditorGUILayout.TextField(regexReplacement);
                    EditorGUILayout.EndHorizontal();
                    
                    if (GUILayout.Button("测试正则表达式", GUILayout.Width(120)))
                    {
                        TestRegexPattern();
                    }
                    EditorGUILayout.HelpBox("支持标准正则表达式语法，如：\\\\d+ 匹配数字，[a-zA-Z] 匹配字母", MessageType.Info);
                    break;
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = filteredList.Count > 0;
            if (GUILayout.Button("预览改名", GUILayout.Height(30)))
            {
                PreviewRename();
            }
            
            GUI.enabled = currentState == ToolState.Modified;
            if (GUILayout.Button("应用改名", GUILayout.Height(30)))
            {
                ApplyRename();
            }
            
            GUI.enabled = currentState == ToolState.Modified || currentState == ToolState.Reverted;
            if (GUILayout.Button("恢复原状", GUILayout.Height(30)))
            {
                RevertChanges();
            }
            
            GUI.enabled = true;
            if (GUILayout.Button("清空列表", GUILayout.Height(30)))
            {
                ClearList();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawListControls()
        {
            if (filteredList.Count == 0) return;
            
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            // 全选/反选控制
            bool newSelectAll = EditorGUILayout.ToggleLeft("全选", selectAll);
            if (newSelectAll != selectAll)
            {
                selectAll = newSelectAll;
                foreach (var file in filteredList)
                {
                    file.IsSelected = selectAll;
                }
            }
            
            GUILayout.FlexibleSpace();
            
            int selectedCount = filteredList.Count(f => f.IsSelected);
            GUILayout.Label($"已选择: {selectedCount}/{filteredList.Count}", EditorStyles.miniBoldLabel);
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFileList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"文件列表 ({filteredList.Count} 个文件)", EditorStyles.boldLabel);
            
            if (filteredList.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到符合条件的文件，请先扫描目录", MessageType.Info);
            }
            else
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
                
                for (int i = 0; i < filteredList.Count; i++)
                {
                    var file = filteredList[i];
                    
                    // 固定高度的条目
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(60));
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        // 复选框
                        file.IsSelected = EditorGUILayout.Toggle(file.IsSelected, GUILayout.Width(20));
                        
                        // 文件图标
                        Texture2D icon = AssetDatabase.GetCachedIcon(file.OriginalPath) as Texture2D;
                        if (icon != null)
                        {
                            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                        }
                        
                        // 文件名称（可点击编辑）
                        if (editingFileIndex == i)
                        {
                            // 编辑模式
                            editingTempName = EditorGUILayout.TextField(editingTempName, GUILayout.Width(180));
                            
                            if (GUILayout.Button("✓", GUILayout.Width(20)))
                            {
                                // 确认修改
                                if (!string.IsNullOrEmpty(editingTempName))
                                {
                                    file.CurrentName = editingTempName;
                                    file.IsModified = true;
                                    file.CurrentPath = Path.Combine(Path.GetDirectoryName(file.OriginalPath), editingTempName + file.FileExtension);
                                    currentState = ToolState.Modified;
                                }
                                editingFileIndex = -1;
                            }
                            
                            if (GUILayout.Button("✗", GUILayout.Width(20)))
                            {
                                // 取消编辑
                                editingFileIndex = -1;
                            }
                        }
                        else
                        {
                            // 显示模式
                            string displayName = file.IsModified ? file.CurrentName : file.OriginalName;
                            
                            // 如果有修改，显示绿色
                            if (file.IsModified)
                            {
                                GUI.color = Color.green;
                            }
                            
                            // 点击文件名进入编辑模式
                            if (GUILayout.Button(displayName, EditorStyles.label, GUILayout.Width(200), GUILayout.Height(20)))
                            {
                                editingFileIndex = i;
                                editingTempName = file.IsModified ? file.CurrentName : file.OriginalName;
                            }
                            
                            GUI.color = Color.white;
                        }
                        
                        // 显示路径
                        string shortPath = Path.GetDirectoryName(file.OriginalPath);
                        if (shortPath.Length > 40)
                        {
                            shortPath = "..." + shortPath.Substring(shortPath.Length - 37);
                        }
                        GUILayout.Label(shortPath, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                        
                        // 显示修改状态
                        if (file.IsModified)
                        {
                            GUILayout.Label("已修改", EditorStyles.boldLabel, GUILayout.Width(60));
                        }
                        
                        EditorGUILayout.EndHorizontal();
                        
                        // 第二行：显示扩展信息和操作按钮
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(25); // 对齐复选框
                        
                        // 文件扩展名
                        GUILayout.Label($"扩展名: {file.FileExtension}", EditorStyles.miniLabel, GUILayout.Width(80));
                        
                        GUILayout.FlexibleSpace();
                        
                        // 操作按钮
                        GUI.enabled = file.IsModified;
                        if (GUILayout.Button("应用", GUILayout.Width(50)))
                        {
                            ApplySingleFileRename(file);
                        }
                        GUI.enabled = true;
                        
                        if (GUILayout.Button("定位", GUILayout.Width(50)))
                        {
                            HighlightFile(file);
                        }
                        
                        if (GUILayout.Button("删除", GUILayout.Width(50)))
                        {
                            RemoveFileFromList(file);
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            string statusText = "";
            Color statusColor = Color.white;
            
            switch (currentState)
            {
                case ToolState.Ready:
                    statusText = "就绪";
                    statusColor = Color.gray;
                    break;
                case ToolState.Modified:
                    statusText = "已修改 (等待应用)";
                    statusColor = Color.yellow;
                    break;
                case ToolState.Reverted:
                    statusText = "已恢复";
                    statusColor = Color.cyan;
                    break;
            }
            
            GUI.color = statusColor;
            GUILayout.Label($"状态: {statusText}", EditorStyles.boldLabel);
            GUI.color = Color.white;
            
            GUILayout.FlexibleSpace();
            
            int modifiedCount = filteredList.Count(f => f.IsModified);
            int selectedCount = filteredList.Count(f => f.IsSelected);
            GUILayout.Label($"修改: {modifiedCount} | 选中: {selectedCount} | 总数: {filteredList.Count}", EditorStyles.boldLabel);
            
            EditorGUILayout.EndHorizontal();
        }

        private void ScanFiles()
        {
            if (!Directory.Exists(targetFolderPath))
            {
                EditorUtility.DisplayDialog("错误", "目录不存在，请选择有效目录", "确定");
                return;
            }
            
            fileList.Clear();
            editingFileIndex = -1; // 重置编辑状态
            
            try
            {
                // 获取所有文件（不限制Assets目录）
                string[] allFiles = Directory.GetFiles(targetFolderPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => !f.EndsWith(".meta")).ToArray();
                
                // 根据筛选条件过滤
                string[] extensions = GetFilteredExtensions();
                
                foreach (string filePath in allFiles)
                {
                    string extension = Path.GetExtension(filePath).ToLower();
                    
                    if (ShouldIncludeFile(extension, extensions))
                    {
                        FileInfo fileInfo = new FileInfo
                        {
                            OriginalPath = filePath,
                            CurrentPath = filePath,
                            OriginalName = Path.GetFileNameWithoutExtension(filePath),
                            CurrentName = Path.GetFileNameWithoutExtension(filePath),
                            FileExtension = extension,
                            IsModified = false,
                            IsSelected = false
                        };
                        
                        fileList.Add(fileInfo);
                    }
                }
                
                filteredList = new List<FileInfo>(fileList);
                currentState = ToolState.Ready;
                selectAll = false;
                Repaint();
                
                Debug.Log($"扫描完成，找到 {fileList.Count} 个文件");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("扫描错误", $"扫描文件时出错: {e.Message}", "确定");
            }
        }

        private string[] GetFilteredExtensions()
        {
            if (currentFilter == FileTypeFilter.Custom && !string.IsNullOrEmpty(customExtension))
            {
                return customExtension.Split(',')
                    .Select(e => e.Trim().ToLower())
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToArray();
            }
            
            if (fileTypeExtensions.ContainsKey(currentFilter))
            {
                return fileTypeExtensions[currentFilter];
            }
            
            return new string[] { ".*" };
        }

        private bool ShouldIncludeFile(string fileExtension, string[] allowedExtensions)
        {
            if (allowedExtensions.Contains(".*"))
            {
                return true;
            }
            
            return allowedExtensions.Contains(fileExtension);
        }

        private void TestRegexPattern()
        {
            if (string.IsNullOrEmpty(regexPattern))
            {
                EditorUtility.DisplayDialog("提示", "请输入正则表达式", "确定");
                return;
            }
            
            try
            {
                Regex regex = new Regex(regexPattern);
                
                // 测试第一个文件作为示例
                if (filteredList.Count > 0)
                {
                    var testFile = filteredList[0];
                    string result = Regex.Replace(testFile.OriginalName, regexPattern, regexReplacement);
                    EditorUtility.DisplayDialog("正则测试", 
                        $"测试文件: {testFile.OriginalName}\n" +
                        $"正则表达式: {regexPattern}\n" +
                        $"替换内容: {regexReplacement}\n" +
                        $"结果: {result}", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请先扫描文件", "确定");
                }
            }
            catch (ArgumentException ex)
            {
                EditorUtility.DisplayDialog("正则表达式错误", 
                    $"正则表达式语法错误:\n{ex.Message}", "确定");
            }
        }

        private void PreviewRename()
        {
            if (filteredList.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有文件可以重命名", "确定");
                return;
            }
            
            // 验证规则参数
            if (!ValidateRuleParameters())
            {
                return;
            }
            
            // 获取要操作的文件（选中的文件，如果没有选中则操作全部）
            var filesToOperate = filteredList.Where(f => f.IsSelected).ToList();
            if (filesToOperate.Count == 0)
            {
                filesToOperate = filteredList;
            }
            
            // 保存历史状态
            SaveCurrentState();
            
            int counter = startNumber;
            int modifiedCount = 0;
            
            foreach (var file in filesToOperate)
            {
                string newName = file.OriginalName;
                bool isModified = false;
                
                switch (currentRule)
                {
                    case RenameRule.ReplaceText:
                        if (!string.IsNullOrEmpty(searchText))
                        {
                            newName = newName.Replace(searchText, replaceText);
                            isModified = newName != file.OriginalName;
                        }
                        break;
                        
                    case RenameRule.AddPrefix:
                        if (!string.IsNullOrEmpty(prefixText))
                        {
                            newName = prefixText + newName;
                            isModified = true;
                        }
                        break;
                        
                    case RenameRule.AddSuffix:
                        if (!string.IsNullOrEmpty(suffixText))
                        {
                            newName = newName + suffixText;
                            isModified = true;
                        }
                        break;
                        
                    case RenameRule.RemoveText:
                        if (!string.IsNullOrEmpty(searchText))
                        {
                            newName = newName.Replace(searchText, "");
                            isModified = newName != file.OriginalName;
                        }
                        break;
                        
                    case RenameRule.ChangeCase:
                        if (!string.IsNullOrEmpty(newName))
                        {
                            newName = toUpperCase ? newName.ToUpper() : newName.ToLower();
                            isModified = newName != file.OriginalName;
                        }
                        break;
                        
                    case RenameRule.AddNumbering:
                        string numberFormat = $"D{numberPadding}";
                        newName = $"{counter.ToString(numberFormat)}_{newName}";
                        isModified = true;
                        counter++;
                        break;
                        
                    case RenameRule.RegexReplace:
                        if (!string.IsNullOrEmpty(regexPattern))
                        {
                            try
                            {
                                string originalName = newName;
                                newName = Regex.Replace(newName, regexPattern, regexReplacement);
                                isModified = newName != originalName;
                            }
                            catch (RegexMatchTimeoutException)
                            {
                                Debug.LogWarning($"正则表达式超时: {file.OriginalName}");
                            }
                            catch (ArgumentException ex)
                            {
                                Debug.LogError($"正则表达式错误: {ex.Message}");
                            }
                        }
                        break;
                }
                
                if (isModified)
                {
                    file.CurrentName = newName;
                    file.IsModified = true;
                    
                    // 更新路径
                    string directory = Path.GetDirectoryName(file.OriginalPath);
                    file.CurrentPath = Path.Combine(directory, newName + file.FileExtension);
                    modifiedCount++;
                }
                else
                {
                    file.IsModified = false;
                }
            }
            
            currentState = ToolState.Modified;
            editingFileIndex = -1; // 重置编辑状态
            Repaint();
        }

        private bool ValidateRuleParameters()
        {
            switch (currentRule)
            {
                case RenameRule.RegexReplace:
                    if (string.IsNullOrEmpty(regexPattern))
                    {
                        EditorUtility.DisplayDialog("错误", "请输入正则表达式", "确定");
                        return false;
                    }
                    
                    // 验证正则表达式语法
                    try
                    {
                        Regex regex = new Regex(regexPattern);
                    }
                    catch (ArgumentException ex)
                    {
                        EditorUtility.DisplayDialog("正则表达式错误", 
                            $"正则表达式语法错误:\n{ex.Message}", "确定");
                        return false;
                    }
                    break;
            }
            
            return true;
        }

        private void ApplyRename()
        {
            // 获取要操作的文件（选中的已修改文件）
            var filesToRename = filteredList.Where(f => f.IsModified && f.IsSelected).ToList();
            if (filesToRename.Count == 0)
            {
                filesToRename = filteredList.Where(f => f.IsModified).ToList();
            }
            
            if (filesToRename.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有需要应用修改的文件", "确定");
                return;
            }
            
            // 记录重命名操作
            renameHistory = new RenameData
            {
                originalFolderPath = targetFolderPath
            };
            
            bool hasError = false;
            List<string> errorMessages = new List<string>();
            int successCount = 0;
            
            foreach (var file in filesToRename)
            {
                try
                {
                    // 检查目标文件是否已存在
                    if (File.Exists(file.CurrentPath) && file.CurrentPath != file.OriginalPath)
                    {
                        errorMessages.Add($"文件已存在: {Path.GetFileName(file.CurrentPath)}");
                        continue;
                    }
                    
                    // 重命名文件
                    File.Move(file.OriginalPath, file.CurrentPath);
                    
                    // 如果存在对应的meta文件，也一起重命名
                    string metaFile = file.OriginalPath + ".meta";
                    if (File.Exists(metaFile))
                    {
                        File.Move(metaFile, file.CurrentPath + ".meta");
                    }
                    
                    // 记录成功重命名的文件
                    renameHistory.files.Add(new FileData
                    {
                        originalPath = file.OriginalPath,
                        newPath = file.CurrentPath
                    });
                    
                    // 更新文件信息
                    file.OriginalPath = file.CurrentPath;
                    file.OriginalName = file.CurrentName;
                    file.IsModified = false;
                    successCount++;
                }
                catch (System.Exception e)
                {
                    errorMessages.Add($"{file.OriginalName}: {e.Message}");
                    hasError = true;
                }
            }
            
            // 刷新AssetDatabase
            AssetDatabase.Refresh();
            
            // 保存历史记录
            SaveHistory();
            
            if (hasError)
            {
                string errorMessage = $"成功重命名 {successCount} 个文件，但有 {errorMessages.Count} 个错误:\n" + 
                    string.Join("\n", errorMessages.Take(5));
                
                if (errorMessages.Count > 5)
                {
                    errorMessage += $"\n...还有 {errorMessages.Count - 5} 个错误";
                }
                
                EditorUtility.DisplayDialog("部分错误", errorMessage, "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("成功", $"所有文件重命名完成 ({successCount} 个文件)", "确定");
            }
            
            currentState = ToolState.Ready;
            selectAll = false;
            editingFileIndex = -1;
            Repaint();
        }

        private void ApplySingleFileRename(FileInfo file)
        {
            if (!file.IsModified)
            {
                EditorUtility.DisplayDialog("提示", "此文件没有修改", "确定");
                return;
            }
            
            if (!EditorUtility.DisplayDialog("确认", 
                $"确定要重命名文件吗？\n{file.OriginalName} → {file.CurrentName}", 
                "确定", "取消"))
            {
                return;
            }
            
            try
            {
                // 重命名文件
                File.Move(file.OriginalPath, file.CurrentPath);
                
                // 如果存在对应的meta文件，也一起重命名
                string metaFile = file.OriginalPath + ".meta";
                if (File.Exists(metaFile))
                {
                    File.Move(metaFile, file.CurrentPath + ".meta");
                }
                
                file.OriginalPath = file.CurrentPath;
                file.OriginalName = file.CurrentName;
                file.IsModified = false;
                
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("成功", "文件重命名成功", "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"重命名失败: {e.Message}", "确定");
            }
            
            editingFileIndex = -1;
            Repaint();
        }

        private void RevertChanges()
        {
            // 获取要恢复的文件（选中的已修改文件）
            var filesToRevert = filteredList.Where(f => f.IsModified && f.IsSelected).ToList();
            if (filesToRevert.Count == 0)
            {
                filesToRevert = filteredList.Where(f => f.IsModified).ToList();
            }
            
            if (filesToRevert.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有需要恢复的文件", "确定");
                return;
            }
            
            foreach (var file in filesToRevert)
            {
                file.CurrentName = file.OriginalName;
                file.CurrentPath = file.OriginalPath;
                file.IsModified = false;
            }
            
            currentState = ToolState.Reverted;
            editingFileIndex = -1;
            Repaint();
            
            EditorUtility.DisplayDialog("已恢复", $"已恢复 {filesToRevert.Count} 个文件的原始名称", "确定");
        }

        private void ClearList()
        {
            if (filteredList.Count > 0 && 
                EditorUtility.DisplayDialog("确认", "清空文件列表吗？", "确定", "取消"))
            {
                fileList.Clear();
                filteredList.Clear();
                currentState = ToolState.Ready;
                selectAll = false;
                editingFileIndex = -1;
                Repaint();
            }
        }

        private void HighlightFile(FileInfo file)
        {
            try
            {
                // 尝试加载并定位文件
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file.OriginalPath);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
                else
                {
                    // 如果不在Assets目录下，显示文件位置
                    EditorUtility.RevealInFinder(file.OriginalPath);
                }
            }
            catch
            {
                // 显示文件位置
                EditorUtility.RevealInFinder(file.OriginalPath);
            }
        }

        private void RemoveFileFromList(FileInfo file)
        {
            if (EditorUtility.DisplayDialog("确认", 
                $"从列表中移除文件: {file.OriginalName}？", "确定", "取消"))
            {
                fileList.Remove(file);
                filteredList.Remove(file);
                Repaint();
            }
        }

        private void SaveCurrentState()
        {
            // 这里可以添加保存当前状态的功能
        }

        private void SaveHistory()
        {
            if (renameHistory == null || renameHistory.files.Count == 0)
                return;
                
            try
            {
                historyPath = Path.Combine(Application.dataPath, "../ProjectSettings/RenameHistory.json");
                string json = JsonUtility.ToJson(renameHistory, true);
                File.WriteAllText(historyPath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"保存历史记录失败: {e.Message}");
            }
        }

        private void LoadHistory()
        {
            try
            {
                historyPath = Path.Combine(Application.dataPath, "../ProjectSettings/RenameHistory.json");
                if (File.Exists(historyPath))
                {
                    string json = File.ReadAllText(historyPath);
                    renameHistory = JsonUtility.FromJson<RenameData>(json);
                }
                else
                {
                    renameHistory = new RenameData();
                }
            }
            catch
            {
                renameHistory = new RenameData();
            }
        }
    }
}