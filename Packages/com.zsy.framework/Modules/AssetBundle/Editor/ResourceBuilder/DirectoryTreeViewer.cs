using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using ZSYFramework.Core.Editor.Utils;

namespace ZSYFramework.AssetBundle.Editor
{
/// <summary>
/// 资源分包，名称设置方式
/// </summary>
public enum BundleNameConfiguration
{
Root, //使用根目录命名
Parent, //使用父目录命名
Alone, //每个文件单独命名
Customization, //自定义
}

/// <summary>
/// 目录或文件的Model类，存储层级信息
/// </summary>
public class FileSystemModel
{
public readonly string Guid;
public readonly string Name;
public readonly string FullPath;
public readonly bool IsDirectory; //是否为文件夹
public bool IsExpanded; //是否展开
public List<FileSystemModel> Children;
public FileEditInfo  SaveInfo;

public FileSystemModel(string path)
{
    Guid = AssetDatabase.AssetPathToGUID(path);
    FullPath = path;
    Name = Path.GetFileName(path);
    IsDirectory = Directory.Exists(path);
    IsExpanded = false;
    Children = new List<FileSystemModel>();
}
}

/// <summary>
/// 目录或文件的Model类，存储层级信息
/// </summary>
public class FileSaveModel
{
   public List<string> RootPaths = new();
   
   public Dictionary<string, FileEditInfo> FolderInfos=  new();
   
   public Dictionary<string, FileEditInfo> FileInfos= new();
}

/// <summary>
/// 文件编辑信息记录。
/// </summary>
public class FileEditInfo
{
public string Guid;
public string RootGuid;

public BundleNameConfiguration NameConfiguration = BundleNameConfiguration.Root;
public string AbName;

public FileEditInfo(string path,string rootGuid)
{
    RootGuid = rootGuid;
    Guid = AssetDatabase.AssetPathToGUID(path);
}
}

/// <summary>
/// 编辑器工具：通过选定目录，树状展示所有文件和目录
/// </summary>
public class DirectoryTreeViewer : EditorWindow
{
private static string BundleFilesPath => Path.Combine(BundleConfigLocator.ConfigDirectory, "BundleResPath.txt");

private Vector2 _scrollPosition;

// 样式
private GUIStyle _treeItemStyle;
private GUIStyle _folderStyle;
private GUIStyle _fileStyle;

private FileSaveModel _fileEditModel = new();

private readonly List<FileSystemModel> _rootModels  = new(); 

[MenuItem("SY-Tool/AssetBundle/旧版资源编辑器(待移除)", false, 98)]
public static void ShowWindow()
{
    GetWindow<DirectoryTreeViewer>("AssetBundle资源编辑器");
}

private void OnEnable()
{
    InitStyles();

    LoadInfo();
}

private void OnGUI()
{
    // 顶部工具栏
    DrawToolbar();

    //绘制已选目录
    DrawPaths();

    //绘制所有节点
    DrawTreeNodes();
}


private void InitStyles()
{
    if (_treeItemStyle != null) return;

    _treeItemStyle = new GUIStyle(EditorStyles.label)
    {
        padding = new RectOffset(20, 5, 2, 2),
        margin = new RectOffset(0, 0, 0, 0),
        richText = true
    };

    _folderStyle = new GUIStyle(_treeItemStyle)
    {
        fontStyle = FontStyle.Normal
    };

    _fileStyle = new GUIStyle(_treeItemStyle)
    {
        fontStyle = FontStyle.Normal
    };
}

private void LoadInfo()
{
    if (!File.Exists(BundleFilesPath))
    {
        return;
    }

    var json = File.ReadAllText(BundleFilesPath);

    if (string.IsNullOrEmpty(json))
    {
        return;
    }

    _fileEditModel = JsonConvert.DeserializeObject<FileSaveModel>(json);

    if (_fileEditModel == null || _fileEditModel.RootPaths == null || _fileEditModel.RootPaths.Count == 0) return;

    foreach (var path in _fileEditModel.RootPaths)
    {
        LoadDirectory(path);
    }
}

private void DrawToolbar()
{
    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
    if (GUILayout.Button("添加目录", EditorStyles.toolbarButton, GUILayout.Width(80)))
    {
        var selectedPath = EditorUtility.OpenFolderPanel("选择目录", "Assets", "");
        if (!string.IsNullOrEmpty(selectedPath))
        {
            var relativePath = GetRelativePath(selectedPath);
            LoadDirectory(relativePath);
        }
    }

    if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(80)))
    {
        //LoadRootDirectory(_rootPath);
    }

    if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(80)))
    {
        SaveData();
    }

    GUILayout.FlexibleSpace();
    EditorGUILayout.EndHorizontal();
}

private void DrawPaths()
{
    if (_fileEditModel == null || _fileEditModel.RootPaths == null) return;

    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
    EditorGUILayout.LabelField($"目录列表:", GUILayout.Width(60));

    for (var i = 0; i < _fileEditModel.RootPaths.Count; i++)
    {
        EditorGUILayout.BeginHorizontal();
        
        var path = _fileEditModel.RootPaths[i];
        EditorGUILayout.LabelField($"目录{i+1}:", GUILayout.Width(60));
        EditorGUILayout.LabelField(path, GUILayout.MinWidth(300));
        if (GUILayout.Button("移除", GUILayout.Width(80)))
        {
            RemovePath(path);
        }
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }
  
    EditorGUILayout.EndVertical();
}

private void RemovePath(string path)
{
    foreach (var node in _rootModels)
    {
        if (node.FullPath != path) continue;
        _rootModels.Remove(node);
        break;
    }

    if (_fileEditModel != null)
    {
        if (_fileEditModel.RootPaths.Contains(path))
        {
            _fileEditModel.RootPaths.Remove(path);
        }

        var rootGuid = AssetDatabase.AssetPathToGUID(path);
        var removeKeys=new List<string>();
        foreach (var variable in _fileEditModel.FileInfos)
        {
            if(variable.Value.RootGuid == rootGuid)
            {
                removeKeys.Add(variable.Key);
            }
        }

        foreach (var key in removeKeys)
        {
            _fileEditModel.FileInfos.Remove(key);
        }

        removeKeys.Clear();
        foreach (var variable in _fileEditModel.FolderInfos)
        {
            if(variable.Value.RootGuid == rootGuid)
            {
                removeKeys.Add(variable.Key);
            }
        }
        
        foreach (var key in removeKeys)
        {
            _fileEditModel.FolderInfos.Remove(key);
        }
    }
    
    SaveData();
}

private void DrawTreeNodes()
{
    if (_rootModels is not { Count: > 0 }) return;

    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, EditorStyles.helpBox);
    foreach (var node in _rootModels)
    {
        DrawTreeNode(node);
    }
    EditorGUILayout.EndScrollView();
}

/// <summary>
/// 绘制树节点
/// </summary>
private void DrawTreeNode(FileSystemModel node,int Depth = 1)
{
    if (node == null) return;

    EditorGUILayout.BeginHorizontal();
    // 缩进
    GUILayout.Space(Depth * 18);

    // 绘制图标
    var iconContent = GetIconForNode(node);
    GUILayout.Label(iconContent, GUILayout.Width(20), GUILayout.Height(16));

    var style = node.IsDirectory ? _folderStyle : _fileStyle;
    style.normal.textColor = node.IsDirectory ? Color.white : new Color(0.8f, 0.8f, 0.8f);

    if (GUILayout.Button(node.Name, style, GUILayout.Width(300),GUILayout.Height(18)))
    {
        if (node.IsDirectory)
        {
            node.IsExpanded = !node.IsExpanded;
        }
        else
        {
            // 点击文件，在Project窗口选中
            PingObject(node.FullPath);
        }
    }

    if (node.SaveInfo != null)
    {
        GUILayout.Space(-Depth * 18);
        var path = AssetDatabase.GUIDToAssetPath(node.SaveInfo.Guid);
        GUILayout.Label(path, GUILayout.Width(300), GUILayout.Height(16));
    }

    EditorGUILayout.EndHorizontal();

    // 绘制子节点
    if (!node.IsDirectory || !node.IsExpanded) return;
    foreach (var child in node.Children)
    {
        DrawTreeNode(child, Depth + 1);
    }
}

/// <summary>
/// 加载根目录
/// </summary>
private void LoadDirectory(string path)
{
    if (_fileEditModel.FileInfos.ContainsKey(path))
    {
        Debug.LogError($"该路径已经存在： {path}");
        return;
    }

    if (!Directory.Exists(path))
    {
        Debug.LogError($"该目录不存在： {path}");
        return;
    }

    var rootGuid = AssetDatabase.AssetPathToGUID(path);
    var rootModel = new FileSystemModel(path);
    _rootModels.Add(rootModel);

    if (!_fileEditModel.RootPaths.Contains(path))
    {
        _fileEditModel.RootPaths.Add(path);
    }
    
    if (!_fileEditModel.FolderInfos.TryGetValue(rootModel.Guid,out var info))
    {
        info = new FileEditInfo(path,rootGuid);
        _fileEditModel.FolderInfos.Add(rootModel.Guid, info);
    }
    rootModel.SaveInfo = info;

    LoadChildren(rootModel,rootGuid);
    
    SaveData();
}

/// <summary>
/// 加载子节点
/// </summary>
private void LoadChildren(FileSystemModel parentNode,string rootGuid)
{
    if (!parentNode.IsDirectory) return;

    parentNode.Children.Clear();

    try
    {
        // 加载目录
        var directories = Directory.GetDirectories(parentNode.FullPath);
        foreach (var dirPath in directories)
        {
            var dirName = Path.GetFileName(dirPath);
            if (dirName.StartsWith(".") || dirName.StartsWith("~")) continue;

            var dirNode = new FileSystemModel(dirPath);
            parentNode.Children.Add(dirNode);

            if (!_fileEditModel.FolderInfos.TryGetValue(dirNode.Guid,out var info))
            {
                info = new FileEditInfo(dirPath,rootGuid);
                _fileEditModel.FolderInfos.Add(dirNode.Guid, info);
            }

            dirNode.SaveInfo = info;
            LoadChildren(dirNode,rootGuid);
        }

        // 加载文件
        var files = Directory.GetFiles(parentNode.FullPath);
        foreach (var filePath in files)
        {
            var fileName = Path.GetFileName(filePath);
            if (fileName.EndsWith(".meta") || fileName.StartsWith(".")) continue;

            var fileNode = new FileSystemModel(filePath);
            parentNode.Children.Add(fileNode);
            
            if (!_fileEditModel.FileInfos.TryGetValue(fileNode.Guid,out var info))
            {
                info = new FileEditInfo(filePath,rootGuid);
                _fileEditModel.FileInfos.Add(fileNode.Guid, info);
            }

            fileNode.SaveInfo = info;
        }

        // 排序
        parentNode.Children = parentNode.Children
            .OrderByDescending(x => x.IsDirectory)
            .ThenBy(x => x.Name, System.StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    catch (System.Exception e)
    {
        Debug.LogError($"加载失败: {parentNode.FullPath}\n{e.Message}");
    }
}


private void SaveData()
{
    if (_fileEditModel == null)
    {
        return;
    }

    var json = JsonConvert.SerializeObject(_fileEditModel, Formatting.Indented);
    Directory.CreateDirectory(Path.GetDirectoryName(BundleFilesPath) ?? string.Empty);
    File.WriteAllText(BundleFilesPath, json, Encoding.UTF8);
    AssetDatabase.Refresh();
}

/// <summary>
/// 获取节点对应的图标
/// </summary>
private GUIContent GetIconForNode(FileSystemModel node)
{
    string extension;
    if (node.IsDirectory)
    {
        extension = node.Children.Count == 0 ? "FolderEmpty" : node.IsExpanded ? "FolderOpened" : "Folder";
    }
    else
    {
        extension = Path.GetExtension(node.Name).ToLower();
    }

    return EditIconTool.GetIcon(extension);
}


/// <summary>
/// 在Project窗口高亮显示资源
/// </summary>
private static void PingObject(string path)
{
    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
    if (obj == null) return;
    EditorGUIUtility.PingObject(obj);
    Selection.activeObject = obj;
}

/// <summary>
/// 获取相对路径
/// </summary>
private string GetRelativePath(string absolutePath)
{
    var dataPath = Application.dataPath;
    var projectPath = dataPath.Substring(0, dataPath.Length - 6);

    if (absolutePath.StartsWith(projectPath))
    {
        return absolutePath.Substring(projectPath.Length);
    }

    return absolutePath;
}
}
}
