using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using UnityEditor.Animations;
using Object = UnityEngine.Object;

namespace ResourceManagerTool
{
    public class EnhancedResourceManagerWindow : EditorWindow
    {
        // 窗口配置
        private const int THUMBNAIL_SIZE = 48; // 缩小缩略图
        private const int ROW_HEIGHT = 60; // 减小行高
        private const int PADDING = 5;
        
        // 配置保存
        private const string CONFIG_PATH = "Assets/Editor/ResourceManagerConfig.json";
        private const string RECORDS_PATH = "Assets/Editor/ResourceChangeRecords.json";
        private ResourceManagerConfig config;
        
        // 状态变量
        private string searchFilter = "";
        private List<ResourceItem> allResources = new List<ResourceItem>();
        private List<ResourceItem> filteredResources = new List<ResourceItem>();
        private Vector2 scrollPosition;
        private Type selectedType = typeof(Texture2D);
        private Dictionary<Type, string> typeDisplayNames = new Dictionary<Type, string>();
        private string exportPath = "Assets/";
        
        // 目录设置
        private string targetDirectory = "Assets/";
        private bool includeSubdirectories = true;
        
        // 筛选条件
        private bool showAdvancedFilters = false;
        private float minFileSize = 0;
        private float maxFileSize = 10000;
        private DateTime minDate = DateTime.MinValue;
        private DateTime maxDate = DateTime.MaxValue;
        private HashSet<ResourceItem> selectedItems = new HashSet<ResourceItem>();
        
        // 修改记录和撤销栈
        private List<ResourceChangeRecord> changeRecords = new List<ResourceChangeRecord>();
        private Stack<ResourceChangeRecord> undoStack = new Stack<ResourceChangeRecord>();
        private Stack<ResourceChangeRecord> redoStack = new Stack<ResourceChangeRecord>();
        
        // 批量操作
        private string batchRenamePattern = "{name}_{index}";
        private int batchStartIndex = 1;
        private bool selectAllState = false;
        
        // 批量替换功能
        private string batchReplaceFind = "";
        private string batchReplaceWith = "";
        private bool batchReplaceCaseSensitive = false;
        private bool batchReplaceUseRegex = false;
        private bool batchReplacePreviewMode = true;
        private Dictionary<ResourceItem, string> replacementPreviews = new Dictionary<ResourceItem, string>();
        
        // 编辑状态
        private ResourceItem editingItem = null;
        private string tempEditName = "";
        
        [MenuItem("Tools/GCZ/资源改名")]
        public static void ShowWindow()
        {
            var window = GetWindow<EnhancedResourceManagerWindow>("Resource Manager");
            window.minSize = new Vector2(1000, 600);
            window.RefreshResources();
        }
        
        private void OnEnable()
        {
            InitializeTypeDisplayNames();
            LoadConfig();
            LoadChangeRecords();
            RefreshResources();
        }
        
        private void OnDisable()
        {
            SaveConfig();
            SaveChangeRecords();
        }
        
        private void InitializeTypeDisplayNames()
        {
            typeDisplayNames = new Dictionary<Type, string>
            {
                { typeof(Texture2D), "Texture" },
                { typeof(Sprite), "Sprite" },
                { typeof(Material), "Material" },
                { typeof(GameObject), "Prefab" },
                { typeof(AudioClip), "Audio" },
                { typeof(Shader), "Shader" },
                { typeof(Font), "Font" },
                { typeof(AnimationClip), "Animation" },
                { typeof(AnimatorController), "Animator Controller" },
                { typeof(TextAsset), "Text Asset" },
                { typeof(ScriptableObject), "Scriptable Object" },
                { typeof(MonoScript), "Script" },
                { typeof(Mesh), "Mesh" },
                { typeof(RenderTexture), "Render Texture" },
                { typeof(PhysicMaterial), "Physic Material" },
                { typeof(PhysicsMaterial2D), "Physics Material 2D" }
            };
        }
        
        private void LoadConfig()
        {
            try
            {
                if (File.Exists(CONFIG_PATH))
                {
                    string json = File.ReadAllText(CONFIG_PATH);
                    config = JsonUtility.FromJson<ResourceManagerConfig>(json);
                    
                    // 应用配置
                    targetDirectory = config.LastDirectory;
                    includeSubdirectories = config.IncludeSubdirectories;
                    selectedType = Type.GetType(config.LastType) ?? typeof(Texture2D);
                    searchFilter = config.LastSearchFilter;
                    batchRenamePattern = config.BatchRenamePattern;
                    exportPath = config.ExportPath;
                    minFileSize = config.MinFileSize;
                    maxFileSize = config.MaxFileSize;
                    batchReplaceFind = config.BatchReplaceFind;
                    batchReplaceWith = config.BatchReplaceWith;
                    batchReplaceCaseSensitive = config.BatchReplaceCaseSensitive;
                    batchReplaceUseRegex = config.BatchReplaceUseRegex;
                }
                else
                {
                    config = new ResourceManagerConfig();
                    // 确保配置文件目录存在
                    string directory = Path.GetDirectoryName(CONFIG_PATH);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    SaveConfig();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load config: {e.Message}");
                config = new ResourceManagerConfig();
            }
        }
        
        private void SaveConfig()
        {
            try
            {
                if (config == null)
                    config = new ResourceManagerConfig();
                
                // 更新配置
                config.LastDirectory = targetDirectory;
                config.IncludeSubdirectories = includeSubdirectories;
                config.LastType = selectedType.AssemblyQualifiedName;
                config.LastSearchFilter = searchFilter;
                config.BatchRenamePattern = batchRenamePattern;
                config.ExportPath = exportPath;
                config.MinFileSize = minFileSize;
                config.MaxFileSize = maxFileSize;
                config.BatchReplaceFind = batchReplaceFind;
                config.BatchReplaceWith = batchReplaceWith;
                config.BatchReplaceCaseSensitive = batchReplaceCaseSensitive;
                config.BatchReplaceUseRegex = batchReplaceUseRegex;
                config.LastOpenTime = DateTime.Now;
                
                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(CONFIG_PATH, json);
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to save config: {e.Message}");
            }
        }
        
        private void LoadChangeRecords()
        {
            try
            {
                if (File.Exists(RECORDS_PATH))
                {
                    string json = File.ReadAllText(RECORDS_PATH);
                    var container = JsonUtility.FromJson<ChangeRecordsContainer>(json);
                    if (container != null)
                    {
                        changeRecords = container.Records ?? new List<ResourceChangeRecord>();
                        
                        // 重建撤销栈（仅保留最近的操作）
                        undoStack.Clear();
                        foreach (var record in changeRecords.TakeLast(20).Reverse())
                        {
                            undoStack.Push(record);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load change records: {e.Message}");
            }
        }
        
        private void SaveChangeRecords()
        {
            try
            {
                var container = new ChangeRecordsContainer { Records = changeRecords };
                string json = JsonUtility.ToJson(container, true);
                
                string directory = Path.GetDirectoryName(RECORDS_PATH);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(RECORDS_PATH, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to save change records: {e.Message}");
            }
        }
        
        private void OnGUI()
        {
            try
            {
                DrawToolbar();
                
                EditorGUILayout.Space(2);
                
                DrawDirectorySection();
                
                EditorGUILayout.Space(2);
                
                DrawFilterSection();
                
                EditorGUILayout.Space(2);
                
                DrawBatchOperations();
                
                EditorGUILayout.Space(2);
                
                DrawBatchReplaceSection();
                
                EditorGUILayout.Space(2);
                
                DrawResourceList();
                
                EditorGUILayout.Space(2);
                
                DrawExportSection();
            }
            catch (Exception e)
            {
                EditorGUILayout.HelpBox($"Error: {e.Message}", MessageType.Error);
                if (GUILayout.Button("Clear Config and Restart"))
                {
                    ClearConfig();
                    RefreshResources();
                }
            }
        }
        
        private void ClearConfig()
        {
            try
            {
                if (File.Exists(CONFIG_PATH))
                    File.Delete(CONFIG_PATH);
                if (File.Exists(RECORDS_PATH))
                    File.Delete(RECORDS_PATH);
                    
                AssetDatabase.Refresh();
                config = new ResourceManagerConfig();
                changeRecords.Clear();
                undoStack.Clear();
                redoStack.Clear();
                replacementPreviews.Clear();
                SaveConfig();
                SaveChangeRecords();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to clear config: {e.Message}");
            }
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                RefreshResources();
            }
            
            // 撤销/重做按钮
            EditorGUI.BeginDisabledGroup(undoStack.Count == 0);
            if (GUILayout.Button("Undo", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                UndoLastChange();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(redoStack.Count == 0);
            if (GUILayout.Button("Redo", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RedoLastChange();
            }
            EditorGUI.EndDisabledGroup();
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.LabelField($"{selectedItems.Count} selected / {filteredResources.Count} filtered / {allResources.Count} total", EditorStyles.miniLabel);
            
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                SaveConfig();
                SaveChangeRecords();
                EditorUtility.DisplayDialog("Saved", "All settings and changes have been saved.", "OK");
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawDirectorySection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📁 Target Directory", EditorStyles.boldLabel, GUILayout.Width(120));
            
            // 显示当前目录信息
            string displayPath = targetDirectory;
            if (displayPath.Length > 50)
            {
                displayPath = "..." + displayPath.Substring(displayPath.Length - 47);
            }
            EditorGUILayout.LabelField(displayPath, GUILayout.Width(300));
            
            GUILayout.FlexibleSpace();
            
            // 快速目录按钮
            if (GUILayout.Button("Assets", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                targetDirectory = "Assets";
                SaveConfig();
                RefreshResources();
            }
            
            if (GUILayout.Button("Select...", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Target Directory", targetDirectory, "");
                if (!string.IsNullOrEmpty(path))
                {
                    // 转换为Unity相对路径
                    if (path.StartsWith(Application.dataPath))
                    {
                        targetDirectory = "Assets" + path.Substring(Application.dataPath.Length);
                        if (!targetDirectory.EndsWith("/"))
                            targetDirectory += "/";
                    }
                    else
                    {
                        targetDirectory = path + "/";
                    }
                    SaveConfig();
                    RefreshResources();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            includeSubdirectories = EditorGUILayout.ToggleLeft("📂 Include Subdirectories", includeSubdirectories, GUILayout.Width(150));
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Reload", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                LoadConfig();
                LoadChangeRecords();
                RefreshResources();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawFilterSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.BeginHorizontal();
            
            // 类型选择
            EditorGUILayout.LabelField("Type:", GUILayout.Width(40));
            var currentIndex = typeDisplayNames.Keys.ToList().IndexOf(selectedType);
            var displayNames = typeDisplayNames.Values.ToArray();
            var newIndex = EditorGUILayout.Popup(currentIndex, displayNames, GUILayout.Width(120));
            if (newIndex != currentIndex)
            {
                selectedType = typeDisplayNames.Keys.ToList()[newIndex];
                SaveConfig();
                RefreshResources();
            }
            
            GUILayout.Space(10);
            
            // 搜索框
            EditorGUILayout.LabelField("Search:", GUILayout.Width(45));
            string newSearch = EditorGUILayout.TextField(searchFilter, GUILayout.Width(150));
            if (newSearch != searchFilter)
            {
                searchFilter = newSearch;
                SaveConfig();
                FilterResources();
            }
            
            GUILayout.Space(10);
            
            // 修改状态筛选
            EditorGUILayout.LabelField("Status:", GUILayout.Width(45));
            EditorGUILayout.BeginHorizontal(GUILayout.Width(200));
            if (GUILayout.Button("All", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                FilterResources();
            }
            if (GUILayout.Button("Modified", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                filteredResources = allResources.Where(r => r.IsRenamed).ToList();
                selectedItems.Clear();
                selectAllState = false;
            }
            if (GUILayout.Button("Original", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                filteredResources = allResources.Where(r => !r.IsRenamed).ToList();
                selectedItems.Clear();
                selectAllState = false;
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
            
            // 高级筛选开关
            showAdvancedFilters = EditorGUILayout.Foldout(showAdvancedFilters, "Advanced");
            
            EditorGUILayout.EndHorizontal();
            
            // 高级筛选选项
            if (showAdvancedFilters)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                EditorGUILayout.BeginHorizontal();
                
                // 文件大小筛选
                EditorGUILayout.BeginVertical(GUILayout.Width(200));
                EditorGUILayout.LabelField("File Size (KB)", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                float newMinSize = EditorGUILayout.FloatField(minFileSize, GUILayout.Width(60));
                EditorGUILayout.MinMaxSlider(ref minFileSize, ref maxFileSize, 0, 10000);
                float newMaxSize = EditorGUILayout.FloatField(maxFileSize, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
                
                // 检查是否需要更新并保存
                if (newMinSize != minFileSize || newMaxSize != maxFileSize)
                {
                    minFileSize = newMinSize;
                    maxFileSize = newMaxSize;
                    SaveConfig();
                    FilterResources();
                }
                EditorGUILayout.EndVertical();
                
                // 快速大小筛选
                EditorGUILayout.BeginVertical(GUILayout.Width(150));
                EditorGUILayout.LabelField("Quick Filters", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("< 100KB", EditorStyles.miniButton))
                {
                    minFileSize = 0;
                    maxFileSize = 100;
                    SaveConfig();
                    FilterResources();
                }
                if (GUILayout.Button("Clear", EditorStyles.miniButton))
                {
                    minFileSize = 0;
                    maxFileSize = 10000;
                    SaveConfig();
                    FilterResources();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
                
                // 应用筛选按钮
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply Filters", GUILayout.Width(100)))
                {
                    FilterResources();
                }
                if (GUILayout.Button("Clear All Filters", GUILayout.Width(120)))
                {
                    ClearFilters();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawBatchOperations()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.LabelField("⚙️ Batch Operations", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // 全选/取消全选
            bool newSelectAll = EditorGUILayout.ToggleLeft("Select All", selectAllState, GUILayout.Width(80));
            if (newSelectAll != selectAllState)
            {
                selectAllState = newSelectAll;
                if (selectAllState)
                {
                    selectedItems.Clear();
                    foreach (var item in filteredResources)
                    {
                        selectedItems.Add(item);
                    }
                }
                else
                {
                    selectedItems.Clear();
                }
            }
            
            EditorGUILayout.LabelField($"Selected: {selectedItems.Count}", GUILayout.Width(90));
            
            // 批量重命名
            EditorGUILayout.LabelField("Pattern:", GUILayout.Width(50));
            string newPattern = EditorGUILayout.TextField(batchRenamePattern, GUILayout.Width(150));
            if (newPattern != batchRenamePattern)
            {
                batchRenamePattern = newPattern;
                SaveConfig();
            }
            
            EditorGUILayout.LabelField("Start:", GUILayout.Width(40));
            batchStartIndex = EditorGUILayout.IntField(batchStartIndex, GUILayout.Width(40));
            
            EditorGUI.BeginDisabledGroup(selectedItems.Count == 0);
            if (GUILayout.Button("Rename", GUILayout.Width(60)))
            {
                BatchRenameSelected();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(selectedItems.Count == 0);
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Delete Selected Resources",
                    $"Are you sure you want to delete {selectedItems.Count} resources?", "Delete", "Cancel"))
                {
                    BatchDeleteSelected();
                }
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(!selectedItems.Any(i => i.IsRenamed));
            if (GUILayout.Button("Reset Names", GUILayout.Width(80)))
            {
                BatchResetNames();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndHorizontal();
            
            // 批量操作说明
            EditorGUILayout.HelpBox(
                "Pattern variables: {name}=original name, {index}=sequence number, {type}=resource type\n" +
                "Example: {name}_{index} becomes Texture_1, Texture_2, etc.",
                MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawBatchReplaceSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.LabelField("🔧 Batch Replace", EditorStyles.boldLabel);
            
            // 查找和替换字段
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Find:", GUILayout.Width(40));
            string newFind = EditorGUILayout.TextField(batchReplaceFind, GUILayout.Width(150));
            if (newFind != batchReplaceFind)
            {
                batchReplaceFind = newFind;
                SaveConfig();
                UpdateReplacePreviews();
            }
            
            EditorGUILayout.LabelField("Replace:", GUILayout.Width(60));
            string newReplace = EditorGUILayout.TextField(batchReplaceWith, GUILayout.Width(150));
            if (newReplace != batchReplaceWith)
            {
                batchReplaceWith = newReplace;
                SaveConfig();
                UpdateReplacePreviews();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 替换选项
            EditorGUILayout.BeginHorizontal();
            bool newCaseSensitive = EditorGUILayout.ToggleLeft("Case Sensitive", batchReplaceCaseSensitive, GUILayout.Width(100));
            if (newCaseSensitive != batchReplaceCaseSensitive)
            {
                batchReplaceCaseSensitive = newCaseSensitive;
                SaveConfig();
                UpdateReplacePreviews();
            }
            
            bool newUseRegex = EditorGUILayout.ToggleLeft("Use Regex", batchReplaceUseRegex, GUILayout.Width(80));
            if (newUseRegex != batchReplaceUseRegex)
            {
                batchReplaceUseRegex = newUseRegex;
                SaveConfig();
                UpdateReplacePreviews();
            }
            
            bool newPreviewMode = EditorGUILayout.ToggleLeft("Preview Mode", batchReplacePreviewMode, GUILayout.Width(100));
            if (newPreviewMode != batchReplacePreviewMode)
            {
                batchReplacePreviewMode = newPreviewMode;
                UpdateReplacePreviews();
            }
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
            
            // 替换操作按钮
            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(batchReplaceFind) || selectedItems.Count == 0);
            if (GUILayout.Button("Preview Selected", GUILayout.Width(120)))
            {
                UpdateReplacePreviewsForSelected();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(batchReplaceFind) || selectedItems.Count == 0);
            if (GUILayout.Button("Replace Selected", GUILayout.Width(120)))
            {
                if (EditorUtility.DisplayDialog("Batch Replace", 
                    $"Replace '{batchReplaceFind}' with '{batchReplaceWith}' in {selectedItems.Count} selected resources?", 
                    "Replace", "Cancel"))
                {
                    BatchReplaceSelected();
                }
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(batchReplaceFind));
            if (GUILayout.Button("Replace All Filtered", GUILayout.Width(120)))
            {
                int affectedCount = GetAffectedResourcesCount();
                if (EditorUtility.DisplayDialog("Batch Replace All", 
                    $"Replace '{batchReplaceFind}' with '{batchReplaceWith}' in {affectedCount} filtered resources?", 
                    "Replace", "Cancel"))
                {
                    BatchReplaceAllFiltered();
                }
            }
            EditorGUI.EndDisabledGroup();
            
            if (GUILayout.Button("Clear Preview", GUILayout.Width(100)))
            {
                replacementPreviews.Clear();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 替换统计信息
            if (!string.IsNullOrEmpty(batchReplaceFind) && filteredResources.Count > 0)
            {
                int affectedCount = GetAffectedResourcesCount();
                int selectedAffectedCount = GetSelectedAffectedResourcesCount();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Affected: {affectedCount} filtered, {selectedAffectedCount} selected", EditorStyles.miniLabel);
                
                if (batchReplacePreviewMode && replacementPreviews.Count > 0)
                {
                    EditorGUILayout.LabelField($"Previewing: {replacementPreviews.Count} items", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();
            }
            
            // 正则表达式帮助
            if (batchReplaceUseRegex)
            {
                EditorGUILayout.HelpBox(
                    "Regex examples:\n" +
                    "• 'old' → 'new' : Simple replacement\n" +
                    "• '^prefix_' : Remove prefix\n" +
                    "• '_suffix$' : Remove suffix\n" +
                    "• '\\d+' : Replace all numbers",
                    MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawResourceList()
        {
            EditorGUILayout.BeginVertical();
            
            // 列表标题
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📋 Resources ({filteredResources.Count})", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (changeRecords.Count > 0)
            {
                int modifiedCount = allResources.Count(r => r.IsRenamed);
                EditorGUILayout.LabelField($"Modified: {modifiedCount}", EditorStyles.miniLabel, GUILayout.Width(80));
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (filteredResources.Count == 0)
            {
                EditorGUILayout.HelpBox("No resources found. Try changing your search criteria or directory.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }
            
            // 列表内容
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // 绘制列表头
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            EditorGUILayout.LabelField("", GUILayout.Width(25)); // 选择框
            EditorGUILayout.LabelField("", GUILayout.Width(15)); // 修改标记
            EditorGUILayout.LabelField("Preview", GUILayout.Width(THUMBNAIL_SIZE));
            EditorGUILayout.LabelField("Name", GUILayout.Width(200));
            EditorGUILayout.LabelField("Original", GUILayout.Width(150));
            EditorGUILayout.LabelField("Path", GUILayout.Width(250));
            EditorGUILayout.LabelField("Size", GUILayout.Width(70));
            EditorGUILayout.LabelField("Date", GUILayout.Width(100));
            EditorGUILayout.LabelField("Actions", GUILayout.Width(130));
            EditorGUILayout.EndHorizontal();
            
            foreach (var item in filteredResources)
            {
                DrawResourceRow(item);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawResourceRow(ResourceItem item)
        {
            // 检查是否有替换预览
            bool hasReplacementPreview = replacementPreviews.ContainsKey(item);
            string previewName = hasReplacementPreview ? replacementPreviews[item] : null;
            
            // 根据状态设置不同的背景色
            Color originalColor = GUI.backgroundColor;
            if (item.IsRenamed)
            {
                GUI.backgroundColor = new Color(0.8f, 0.9f, 1f, 0.3f); // 已修改的浅蓝色
            }
            else if (hasReplacementPreview)
            {
                GUI.backgroundColor = new Color(1f, 0.9f, 0.8f, 0.3f); // 替换预览的浅橙色
            }
            
            EditorGUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Height(ROW_HEIGHT));
            
            // 恢复默认背景色
            GUI.backgroundColor = originalColor;
            
            // 选择框
            bool isSelected = selectedItems.Contains(item);
            bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(25));
            if (newSelected != isSelected)
            {
                if (newSelected)
                    selectedItems.Add(item);
                else
                    selectedItems.Remove(item);
            }
            
            // 修改标记（更紧凑）
            EditorGUILayout.BeginVertical(GUILayout.Width(15));
            if (item.IsRenamed)
            {
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12
                };
                EditorGUILayout.LabelField("●", style, GUILayout.Width(15));
                var rect = GUILayoutUtility.GetLastRect();
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.6f, 1f, 0.8f)); // 蓝色圆点
                GUI.Label(rect, "●", style);
            }
            else if (hasReplacementPreview)
            {
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12
                };
                EditorGUILayout.LabelField("✏", style, GUILayout.Width(15));
                var rect = GUILayoutUtility.GetLastRect();
                EditorGUI.DrawRect(rect, new Color(1f, 0.6f, 0.2f, 0.8f)); // 橙色铅笔图标
                GUI.Label(rect, "✏", style);
            }
            EditorGUILayout.EndVertical();
            
            // 缩略图（缩小）
            EditorGUILayout.BeginVertical(GUILayout.Width(THUMBNAIL_SIZE));
            Rect thumbnailRect = GUILayoutUtility.GetRect(THUMBNAIL_SIZE, THUMBNAIL_SIZE);
            Texture2D thumbnail = AssetPreview.GetAssetPreview(item.Object);
            if (thumbnail == null)
            {
                thumbnail = AssetPreview.GetMiniThumbnail(item.Object);
            }
            
            if (thumbnail != null)
            {
                GUI.DrawTexture(thumbnailRect, thumbnail, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(thumbnailRect, new Color(0.2f, 0.2f, 0.2f));
                GUI.Label(thumbnailRect, "N/A", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.EndVertical();
            
            // 当前名称（或预览名称）
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            
            if (editingItem == item)
            {
                // 编辑模式
                tempEditName = EditorGUILayout.TextField(tempEditName);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("✓", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    ApplyNameChange(item, tempEditName);
                    editingItem = null;
                }
                
                if (GUILayout.Button("✗", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    editingItem = null;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // 显示模式
                string displayText = hasReplacementPreview ? previewName : item.DisplayName;
                bool showPreview = hasReplacementPreview && batchReplacePreviewMode;
                
                GUIStyle nameStyle;
                if (item.IsRenamed)
                {
                    nameStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.1f, 0.3f, 0.8f) } };
                }
                else if (showPreview)
                {
                    nameStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.8f, 0.4f, 0.1f) } };
                }
                else
                {
                    nameStyle = EditorStyles.label;
                }
                    
                EditorGUILayout.LabelField(displayText, nameStyle);
                
                // 状态标签
                if (item.IsRenamed)
                {
                    EditorGUILayout.LabelField("(Modified)", EditorStyles.miniLabel);
                }
                else if (showPreview)
                {
                    EditorGUILayout.LabelField("(Preview)", EditorStyles.miniLabel);
                }
                
                // 点击名称开始编辑
                var nameRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && nameRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.clickCount == 2)
                    {
                        editingItem = item;
                        tempEditName = item.DisplayName;
                        Event.current.Use();
                    }
                }
                
                // 显示原始名称（如果有替换）
                if (hasReplacementPreview && batchReplacePreviewMode)
                {
                    EditorGUILayout.LabelField($"From: {item.DisplayName}", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndVertical();
            
            // 原始名称
            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            if (item.IsRenamed)
            {
                EditorGUILayout.LabelField("Original:", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(item.OriginalName, EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                EditorGUILayout.LabelField(item.OriginalName, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndVertical();
            
            // 路径信息（更紧凑）
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            string displayPath = item.RelativePath;
            if (displayPath.Length > 40)
            {
                displayPath = "..." + displayPath.Substring(displayPath.Length - 37);
            }
            
            EditorGUILayout.LabelField(displayPath, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
            
            // 文件大小
            EditorGUILayout.BeginVertical(GUILayout.Width(70));
            EditorGUILayout.LabelField(FormatFileSize(item.FileSize), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            // 修改日期
            EditorGUILayout.BeginVertical(GUILayout.Width(100));
            EditorGUILayout.LabelField(item.LastModified.ToString("MM-dd HH:mm"), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            // 操作按钮（更紧凑）
            EditorGUILayout.BeginVertical(GUILayout.Width(130));
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(35)))
            {
                editingItem = item;
                tempEditName = item.DisplayName;
            }
            
            if (GUILayout.Button("Sel", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                Selection.activeObject = item.Object;
                EditorGUIUtility.PingObject(item.Object);
            }
            
            if (GUILayout.Button("Del", EditorStyles.miniButton, GUILayout.Width(30)))
            {
                DeleteResource(item);
            }
            
            if (item.IsRenamed && GUILayout.Button("Reset", EditorStyles.miniButton, GUILayout.Width(35)))
            {
                ResetResourceName(item);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 替换预览操作
            if (hasReplacementPreview && batchReplacePreviewMode)
            {
                if (GUILayout.Button("Apply Replace", EditorStyles.miniButton))
                {
                    ApplyNameChange(item, previewName);
                    replacementPreviews.Remove(item);
                }
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawExportSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📤 Export Path:", GUILayout.Width(80));
            string newExportPath = EditorGUILayout.TextField(exportPath, GUILayout.Width(200));
            if (newExportPath != exportPath)
            {
                exportPath = newExportPath;
                SaveConfig();
            }
            
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.SaveFolderPanel("Select Export Directory", exportPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    exportPath = path;
                    SaveConfig();
                }
            }
            
            GUILayout.Space(20);
            
            EditorGUI.BeginDisabledGroup(changeRecords.Count == 0);
            if (GUILayout.Button("Export Changes", GUILayout.Width(100)))
            {
                ExportChangesToCSV();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(!allResources.Any(r => r.IsRenamed));
            if (GUILayout.Button("Export Modified", GUILayout.Width(100)))
            {
                ExportModifiedResources();
            }
            EditorGUI.EndDisabledGroup();
            
            if (GUILayout.Button("Clear History", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Clear History", 
                    "Are you sure you want to clear all change records?", "Clear", "Cancel"))
                {
                    changeRecords.Clear();
                    undoStack.Clear();
                    redoStack.Clear();
                    SaveChangeRecords();
                    RefreshResources();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 简化的操作历史
            if (changeRecords.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"📝 Last {Mathf.Min(3, changeRecords.Count)} Changes:", EditorStyles.miniBoldLabel);
                
                for (int i = changeRecords.Count - 1; i >= Mathf.Max(0, changeRecords.Count - 3); i--)
                {
                    var record = changeRecords[i];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{record.Timestamp:HH:mm}", GUILayout.Width(40));
                    EditorGUILayout.LabelField(record.ChangeType, GUILayout.Width(50));
                    EditorGUILayout.LabelField(record.OriginalName, EditorStyles.miniLabel);
                    
                    if (record.ChangeType == "Rename")
                    {
                        EditorGUILayout.LabelField("→", GUILayout.Width(10));
                        EditorGUILayout.LabelField(record.NewValue, EditorStyles.miniLabel);
                    }
                    
                    if (GUILayout.Button("↶", EditorStyles.miniButton, GUILayout.Width(20)))
                    {
                        UndoChange(record);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void RefreshResources()
        {
            string searchPattern = $"t:{selectedType.Name}";
            
            if (!string.IsNullOrEmpty(targetDirectory) && AssetDatabase.IsValidFolder(targetDirectory))
            {
                // 如果指定了目录，只搜索该目录下的资源
                string[] guids;
                if (includeSubdirectories)
                {
                    guids = AssetDatabase.FindAssets(searchPattern, new[] { targetDirectory });
                }
                else
                {
                    // 仅搜索指定目录，不包含子目录
                    guids = AssetDatabase.FindAssets(searchPattern);
                }
                
                ProcessGuids(guids);
            }
            else
            {
                // 搜索整个项目
                string[] guids = AssetDatabase.FindAssets(searchPattern);
                ProcessGuids(guids);
            }
            
            FilterResources();
            UpdateReplacePreviews();
        }
        
        private void ProcessGuids(string[] guids)
        {
            allResources = new List<ResourceItem>();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                
                if (obj != null)
                {
                    // 如果指定了目录且不包含子目录，检查路径是否在指定目录下
                    if (!string.IsNullOrEmpty(targetDirectory) && 
                        AssetDatabase.IsValidFolder(targetDirectory) &&
                        !includeSubdirectories)
                    {
                        string parentPath = Path.GetDirectoryName(path).Replace("\\", "/");
                        if (parentPath != targetDirectory.TrimEnd('/'))
                        {
                            continue;
                        }
                    }
                    
                    string originalName = Path.GetFileNameWithoutExtension(path);
                    
                    // 查找该资源的修改记录
                    var renameRecords = changeRecords.Where(r => r.GUID == guid && r.ChangeType == "Rename").ToList();
                    bool isRenamed = renameRecords.Count > 0;
                    string displayName = originalName;
                    
                    if (isRenamed)
                    {
                        // 获取最新的修改记录
                        var latestRecord = renameRecords.OrderByDescending(r => r.Timestamp).First();
                        displayName = latestRecord.NewValue;
                    }
                    
                    allResources.Add(new ResourceItem
                    {
                        GUID = guid,
                        Path = path,
                        Object = obj,
                        OriginalName = originalName,
                        DisplayName = displayName,
                        FileSize = GetFileSize(path),
                        LastModified = GetLastModified(path),
                        IsRenamed = isRenamed,
                        RelativePath = GetRelativePath(path)
                    });
                }
            }
            
            // 按名称排序
            allResources = allResources.OrderBy(r => r.DisplayName).ToList();
        }
        
        private long GetFileSize(string path)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                return fileInfo.Exists ? fileInfo.Length : 0;
            }
            catch
            {
                return 0;
            }
        }
        
        private DateTime GetLastModified(string path)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                return fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.Now;
            }
            catch
            {
                return DateTime.Now;
            }
        }
        
        private string GetRelativePath(string fullPath)
        {
            if (string.IsNullOrEmpty(targetDirectory) || targetDirectory == "Assets/")
                return fullPath;
            
            string targetDir = targetDirectory.TrimEnd('/');
            if (fullPath.StartsWith(targetDir))
                return fullPath.Substring(targetDir.Length).TrimStart('/');
            
            return fullPath;
        }
        
        private void FilterResources()
        {
            filteredResources = allResources.Where(item =>
            {
                // 名称/路径搜索
                bool nameMatch = string.IsNullOrEmpty(searchFilter) ||
                    item.DisplayName.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.RelativePath.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                
                // 文件大小筛选
                bool sizeMatch = item.FileSize >= minFileSize * 1024 && item.FileSize <= maxFileSize * 1024;
                
                return nameMatch && sizeMatch;
            }).ToList();
            
            // 更新全选状态
            selectAllState = selectedItems.Count == filteredResources.Count && filteredResources.Count > 0;
        }
        
        private void ClearFilters()
        {
            searchFilter = "";
            minFileSize = 0;
            maxFileSize = 10000;
            SaveConfig();
            FilterResources();
        }
        
        private void BatchRenameSelected()
        {
            if (selectedItems.Count == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select resources to rename.", "OK");
                return;
            }
            
            int index = batchStartIndex;
            var sortedItems = selectedItems.OrderBy(item => item.DisplayName).ToList();
            
            foreach (var item in sortedItems)
            {
                string newName = batchRenamePattern
                    .Replace("{name}", item.OriginalName)
                    .Replace("{index}", index.ToString())
                    .Replace("{type}", item.Object.GetType().Name);
                
                ApplyNameChange(item, newName);
                index++;
            }
            
            AssetDatabase.Refresh();
            SaveChangeRecords();
        }
        
        private void BatchDeleteSelected()
        {
            var itemsToDelete = selectedItems.ToList();
            foreach (var item in itemsToDelete)
            {
                DeleteResource(item, false);
            }
            
            selectedItems.Clear();
            AssetDatabase.Refresh();
            SaveChangeRecords();
            RefreshResources();
        }
        
        private void BatchResetNames()
        {
            foreach (var item in selectedItems.Where(i => i.IsRenamed).ToList())
            {
                ResetResourceName(item);
            }
            
            AssetDatabase.Refresh();
            SaveChangeRecords();
        }
        
        // 批量替换相关方法
        private void UpdateReplacePreviews()
        {
            if (batchReplacePreviewMode && !string.IsNullOrEmpty(batchReplaceFind))
            {
                replacementPreviews.Clear();
                foreach (var item in filteredResources)
                {
                    string newName = ApplyReplaceToName(item.DisplayName);
                    if (newName != item.DisplayName)
                    {
                        replacementPreviews[item] = newName;
                    }
                }
            }
        }
        
        private void UpdateReplacePreviewsForSelected()
        {
            if (batchReplacePreviewMode && !string.IsNullOrEmpty(batchReplaceFind))
            {
                foreach (var item in selectedItems)
                {
                    string newName = ApplyReplaceToName(item.DisplayName);
                    if (newName != item.DisplayName)
                    {
                        replacementPreviews[item] = newName;
                    }
                }
            }
        }
        
        private int GetAffectedResourcesCount()
        {
            if (string.IsNullOrEmpty(batchReplaceFind))
                return 0;
                
            int count = 0;
            foreach (var item in filteredResources)
            {
                string newName = ApplyReplaceToName(item.DisplayName);
                if (newName != item.DisplayName)
                {
                    count++;
                }
            }
            return count;
        }
        
        private int GetSelectedAffectedResourcesCount()
        {
            if (string.IsNullOrEmpty(batchReplaceFind))
                return 0;
                
            int count = 0;
            foreach (var item in selectedItems)
            {
                string newName = ApplyReplaceToName(item.DisplayName);
                if (newName != item.DisplayName)
                {
                    count++;
                }
            }
            return count;
        }
        
        private string ApplyReplaceToName(string originalName)
        {
            if (string.IsNullOrEmpty(batchReplaceFind))
                return originalName;
                
            try
            {
                if (batchReplaceUseRegex)
                {
                    // 使用正则表达式替换
                    RegexOptions options = batchReplaceCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    return Regex.Replace(originalName, batchReplaceFind, batchReplaceWith, options);
                }
                else
                {
                    // 使用普通字符串替换
                    StringComparison comparison = batchReplaceCaseSensitive ? 
                        StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    
                    // 替换所有匹配项
                    string result = originalName;
                    int index = result.IndexOf(batchReplaceFind, comparison);
                    
                    while (index >= 0)
                    {
                        result = result.Remove(index, batchReplaceFind.Length)
                                     .Insert(index, batchReplaceWith);
                        index = result.IndexOf(batchReplaceFind, index + batchReplaceWith.Length, comparison);
                    }
                    
                    return result;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to apply replace to '{originalName}': {e.Message}");
                return originalName;
            }
        }
        
        private void BatchReplaceSelected()
        {
            if (selectedItems.Count == 0 || string.IsNullOrEmpty(batchReplaceFind))
                return;
                
            foreach (var item in selectedItems.ToList())
            {
                string newName = ApplyReplaceToName(item.DisplayName);
                if (newName != item.DisplayName)
                {
                    ApplyNameChange(item, newName);
                }
            }
            
            replacementPreviews.Clear();
            AssetDatabase.Refresh();
            SaveChangeRecords();
        }
        
        private void BatchReplaceAllFiltered()
        {
            if (string.IsNullOrEmpty(batchReplaceFind))
                return;
                
            int replacedCount = 0;
            foreach (var item in filteredResources.ToList())
            {
                string newName = ApplyReplaceToName(item.DisplayName);
                if (newName != item.DisplayName)
                {
                    ApplyNameChange(item, newName);
                    replacedCount++;
                }
            }
            
            replacementPreviews.Clear();
            AssetDatabase.Refresh();
            SaveChangeRecords();
            
            EditorUtility.DisplayDialog("Batch Replace Complete", 
                $"Replaced '{batchReplaceFind}' with '{batchReplaceWith}' in {replacedCount} resources.", "OK");
        }
        
        private void ApplyNameChange(ResourceItem item, string newName)
        {
            if (string.IsNullOrEmpty(newName) || newName == item.DisplayName)
                return;
                
            RecordNameChange(item, newName);
            RenameAsset(item.Path, newName);
            item.DisplayName = newName;
            item.IsRenamed = true;
        }
        
        private void DeleteResource(ResourceItem item, bool confirm = true)
        {
            if (confirm)
            {
                if (!EditorUtility.DisplayDialog("Delete Resource",
                    $"Are you sure you want to delete {item.DisplayName}?", "Delete", "Cancel"))
                {
                    return;
                }
            }
            
            RecordDeletion(item);
            AssetDatabase.DeleteAsset(item.Path);
            SaveChangeRecords();
            
            if (confirm)
                RefreshResources();
        }
        
        private void ResetResourceName(ResourceItem item)
        {
            RecordNameChange(item, item.OriginalName);
            RenameAsset(item.Path, item.OriginalName);
            item.DisplayName = item.OriginalName;
            item.IsRenamed = false;
            SaveChangeRecords();
        }
        
        private void ExportModifiedResources()
        {
            var modifiedResources = allResources.Where(r => r.IsRenamed).ToList();
            if (modifiedResources.Count == 0)
            {
                EditorUtility.DisplayDialog("No Modified Resources", "No modified resources to export.", "OK");
                return;
            }
            
            string filePath = Path.Combine(exportPath, $"ModifiedResources_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            
            try
            {
                StringBuilder csv = new StringBuilder();
                csv.AppendLine("GUID,OriginalName,CurrentName,Type,Path,Size,LastModified");
                
                foreach (var item in modifiedResources)
                {
                    csv.AppendLine($"\"{item.GUID}\",\"{item.OriginalName}\",\"{item.DisplayName}\",\"{item.Object.GetType().Name}\",\"{item.Path}\",\"{item.FileSize}\",\"{item.LastModified}\"");
                }
                
                File.WriteAllText(filePath, csv.ToString());
                EditorUtility.DisplayDialog("Export Complete", $"Modified resources exported to:\n{filePath}", "OK");
                EditorUtility.RevealInFinder(filePath);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Export Failed", $"Failed to export: {e.Message}", "OK");
            }
        }
        
        private void ExportChangesToCSV()
        {
            if (changeRecords.Count == 0)
            {
                EditorUtility.DisplayDialog("No Changes", "No changes to export.", "OK");
                return;
            }
            
            string filePath = Path.Combine(exportPath, $"ResourceChanges_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            
            try
            {
                StringBuilder csv = new StringBuilder();
                csv.AppendLine("Timestamp,ChangeType,GUID,Path,OriginalName,NewName,OldValue,NewValue");
                
                foreach (var record in changeRecords.OrderBy(r => r.Timestamp))
                {
                    csv.AppendLine($"\"{record.Timestamp}\",\"{record.ChangeType}\",\"{record.GUID}\",\"{record.Path}\",\"{record.OriginalName}\",\"{record.NewName}\",\"{record.OldValue}\",\"{record.NewValue}\"");
                }
                
                File.WriteAllText(filePath, csv.ToString());
                EditorUtility.DisplayDialog("Export Complete", $"Changes exported to:\n{filePath}", "OK");
                EditorUtility.RevealInFinder(filePath);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Export Failed", $"Failed to export: {e.Message}", "OK");
            }
        }
        
        private void RecordNameChange(ResourceItem item, string newName)
        {
            var record = new ResourceChangeRecord
            {
                GUID = item.GUID,
                Path = item.Path,
                OriginalName = item.OriginalName,
                NewName = newName,
                ChangeType = "Rename",
                OldValue = item.DisplayName,
                NewValue = newName,
                Timestamp = DateTime.Now,
                AssetObject = item.Object
            };
            
            changeRecords.Add(record);
            undoStack.Push(record);
            redoStack.Clear();
        }
        
        private void RecordDeletion(ResourceItem item)
        {
            var record = new ResourceChangeRecord
            {
                GUID = item.GUID,
                Path = item.Path,
                OriginalName = item.OriginalName,
                ChangeType = "Delete",
                OldValue = item.DisplayName,
                NewValue = "DELETED",
                Timestamp = DateTime.Now,
                AssetObject = item.Object
            };
            
            changeRecords.Add(record);
            undoStack.Push(record);
            redoStack.Clear();
        }
        
        private void UndoLastChange()
        {
            if (undoStack.Count > 0)
            {
                var record = undoStack.Pop();
                UndoChange(record);
                redoStack.Push(record);
                SaveChangeRecords();
                RefreshResources();
            }
        }
        
        private void RedoLastChange()
        {
            if (redoStack.Count > 0)
            {
                var record = redoStack.Pop();
                RedoChange(record);
                undoStack.Push(record);
                SaveChangeRecords();
                RefreshResources();
            }
        }
        
        private void UndoChange(ResourceChangeRecord record)
        {
            switch (record.ChangeType)
            {
                case "Rename":
                    RenameAsset(record.Path, record.OldValue);
                    break;
                case "Delete":
                    // 删除操作无法直接撤销
                    AssetDatabase.Refresh();
                    break;
            }
        }
        
        private void RedoChange(ResourceChangeRecord record)
        {
            switch (record.ChangeType)
            {
                case "Rename":
                    RenameAsset(record.Path, record.NewValue);
                    break;
                case "Delete":
                    // 重新执行删除（如果资源仍然存在）
                    if (AssetDatabase.LoadAssetAtPath<Object>(record.Path) != null)
                    {
                        AssetDatabase.DeleteAsset(record.Path);
                    }
                    break;
            }
        }
        
        private void RenameAsset(string path, string newName)
        {
            string error = AssetDatabase.RenameAsset(path, newName);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"Failed to rename {path}: {error}");
            }
        }
        
        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{(bytes / 1024f):0.0} KB";
            return $"{(bytes / (1024f * 1024f)):0.0} MB";
        }
        
        [Serializable]
        private class ResourceItem
        {
            public string GUID;
            public string Path;
            public Object Object;
            public string OriginalName;
            public string DisplayName;
            public long FileSize;
            public DateTime LastModified;
            public bool IsRenamed;
            public string RelativePath;
        }
        
        [Serializable]
        private class ResourceChangeRecord
        {
            public string GUID;
            public string Path;
            public string OriginalName;
            public string NewName;
            public string ChangeType; // Rename, Delete
            public string OldValue;
            public string NewValue;
            public DateTime Timestamp;
            public Object AssetObject;
        }
        
        [Serializable]
        private class ResourceManagerConfig
        {
            public string LastDirectory = "Assets/";
            public bool IncludeSubdirectories = true;
            public string LastType = "UnityEngine.Texture2D, UnityEngine.CoreModule";
            public string LastSearchFilter = "";
            public string BatchRenamePattern = "{name}_{index}";
            public string ExportPath = "Assets/";
            public float MinFileSize = 0;
            public float MaxFileSize = 10000;
            public string BatchReplaceFind = "";
            public string BatchReplaceWith = "";
            public bool BatchReplaceCaseSensitive = false;
            public bool BatchReplaceUseRegex = false;
            public DateTime LastOpenTime = DateTime.Now;
        }
        
        [Serializable]
        private class ChangeRecordsContainer
        {
            public List<ResourceChangeRecord> Records = new List<ResourceChangeRecord>();
        }
    }
}