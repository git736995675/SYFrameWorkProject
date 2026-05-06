#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZSYFramework.Editor.DirectoryOpener
{
    /// <summary>
    /// 目录快速打开工具窗口
    /// </summary>
    public class DirectoryOpener : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<CustomDirectory> _customDirectories = new();
        private string _newDirectoryName = "";
        private string _newDirectoryPath = "";
        private bool _showCustomDirectories = true;

        // 保存自定义目录的路径
        private const string CustomDirectoriesKey = "DirectoryQuickOpen_CustomDirectories";

        [MenuItem("SY-Tool/工具/目录快速打开工具")]
        public static void ShowWindow()
        {
            var window = GetWindow<DirectoryOpener>();
            window.titleContent = new GUIContent("目录快速打开", "快速打开各种Unity目录");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            LoadCustomDirectories();
        }

        private void OnGUI()
        {
            DrawHeader();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawUnityDirectories();
            DrawSpecialDirectories();
            DrawCustomDirectories();

            EditorGUILayout.EndScrollView();

            DrawFooter();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);

            // 平台提示
            var platform = Application.platform == RuntimePlatform.WindowsEditor ? "Windows" : "Mac";
            var infoStyle = new GUIStyle(EditorStyles.helpBox);
            infoStyle.normal.textColor = Color.gray;
            EditorGUILayout.LabelField($"当前平台: {platform}", infoStyle);

            EditorGUILayout.Space(10);
        }

        private void DrawUnityDirectories()
        {
            EditorGUILayout.LabelField("📁 Unity 项目目录", EditorStyles.boldLabel);

            // 项目根目录
            DrawDirectoryButton(
                "📂 项目根目录",
                Application.dataPath.Substring(0, Application.dataPath.Length - 6),
                "打开Unity项目根目录"
            );

            // Assets目录
            DrawDirectoryButton(
                "📂 Assets目录",
                Application.dataPath,
                "打开Assets目录"
            );

            // streamingAssets目录
            DrawDirectoryButton(
                "📂 StreamingAssets目录",
                Application.streamingAssetsPath,
                "打开StreamingAssets目录"
            );

            // persistentDataPath
            DrawDirectoryButton(
                "📂 持久化数据存储目录",
                Application.persistentDataPath,
                "打开持久化数据存储目录"
            );

            EditorGUILayout.Space(10);
        }

        private static void DrawSpecialDirectories()
        {
            EditorGUILayout.LabelField("🔧 Unity 特殊目录", EditorStyles.boldLabel);

            // 安装目录
            DrawDirectoryButton(
                "💾 Unity安装目录",
                Path.Combine(GetCurrentUnityInstallPath()),
                "打开Unity安装目录"
            );

            // 编辑器日志目录
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                DrawDirectoryButton(
                    "📋 编辑器日志目录",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity",
                        "Editor"),
                    "打开Unity编辑器日志目录"
                );
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                DrawDirectoryButton(
                    "📋 编辑器日志目录",
                    Path.Combine(Environment.GetEnvironmentVariable("HOME"), "Library", "Logs", "Unity"),
                    "打开Unity编辑器日志目录"
                );
            }

            // 临时目录
            DrawDirectoryButton(
                "📂 临时缓存目录",
                Application.temporaryCachePath,
                "打开临时缓存目录"
            );

            // 工程设置目录
            DrawDirectoryButton(
                "⚙️ ProjectSettings目录",
                Path.Combine(Application.dataPath, "..", "ProjectSettings"),
                "打开ProjectSettings目录"
            );

            // Packages目录
            DrawDirectoryButton(
                "📦 Packages目录",
                Path.Combine(Application.dataPath, "..", "Packages"),
                "打开Packages目录"
            );

            // Library
            DrawDirectoryButton(
                "💾 Library目录",
                Path.Combine(Application.dataPath, "..", "Library"),
                "打开Library缓存目录"
            );

            EditorGUILayout.Space(10);
        }

        private void DrawCustomDirectories()
        {
            // 自定义目录区域
            _showCustomDirectories = EditorGUILayout.Foldout(_showCustomDirectories, "📌 自定义目录", true);

            if (_showCustomDirectories)
            {
                EditorGUI.indentLevel++;

                for (var i = 0; i < _customDirectories.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    // 目录按钮
                    if (GUILayout.Button(_customDirectories[i].name, GUILayout.Height(25)))
                    {
                        OpenFolder(_customDirectories[i].path);
                    }

                    // 删除按钮
                    if (GUILayout.Button("✖", GUILayout.Width(25), GUILayout.Height(25)))
                    {
                        _customDirectories.RemoveAt(i);
                        SaveCustomDirectories();
                        GUIUtility.ExitGUI();
                    }

                    EditorGUILayout.EndHorizontal();
                }

                // 添加新自定义目录
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("添加新目录:", EditorStyles.miniLabel);

                EditorGUILayout.BeginHorizontal();
                _newDirectoryName = EditorGUILayout.TextField("名称", _newDirectoryName);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _newDirectoryPath = EditorGUILayout.TextField("路径", _newDirectoryPath);

                if (GUILayout.Button("浏览", GUILayout.Width(50)))
                {
                    var path = EditorUtility.OpenFolderPanel("选择目录", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        _newDirectoryPath = path;
                    }
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("添加", GUILayout.Width(80)))
                {
                    AddCustomDirectory();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("刷新", GUILayout.Height(30)))
            {
                LoadCustomDirectories();
            }

            var helpStyle = new GUIStyle(EditorStyles.miniLabel);
            helpStyle.alignment = TextAnchor.MiddleRight;
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawDirectoryButton(string label, string path, string tooltip)
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent(label, tooltip), GUILayout.Height(30)))
            {
                OpenFolder(path);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void AddCustomDirectory()
        {
            if (string.IsNullOrEmpty(_newDirectoryName) || string.IsNullOrEmpty(_newDirectoryPath))
            {
                EditorUtility.DisplayDialog("提示", "请输入目录名称和路径", "确定");
                return;
            }

            if (!Directory.Exists(_newDirectoryPath))
            {
                EditorUtility.DisplayDialog("错误", "目录不存在", "确定");
                return;
            }

            _customDirectories.Add(new CustomDirectory
            {
                name = _newDirectoryName,
                path = _newDirectoryPath
            });

            SaveCustomDirectories();

            // 清空输入
            _newDirectoryName = "";
            _newDirectoryPath = "";

            GUIUtility.ExitGUI();
        }

        private void SaveCustomDirectories()
        {
            var json = JsonUtility.ToJson(new CustomDirectoryList { directories = _customDirectories });
            EditorPrefs.SetString(CustomDirectoriesKey, json);
        }

        private void LoadCustomDirectories()
        {
            if (EditorPrefs.HasKey(CustomDirectoriesKey))
            {
                var json = EditorPrefs.GetString(CustomDirectoriesKey);
                var list = JsonUtility.FromJson<CustomDirectoryList>(json);
                if (list != null)
                {
                    _customDirectories = list.directories ?? new List<CustomDirectory>();
                }
            }

            _customDirectories ??= new List<CustomDirectory>();
        }

        /// <summary>
        /// 打开指定路径的文件夹
        /// </summary>
        private static void OpenFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                UnityEngine.Debug.LogError($"目录不存在: {path}");
                EditorUtility.DisplayDialog("错误", $"目录不存在:\n{path}", "确定");
                return;
            }

            try
            {
                // 统一处理路径分隔符
                path = path.Replace("/", "\\");

                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                        Process.Start("explorer.exe", $"\"{path}\"");
                        break;
                    case RuntimePlatform.OSXEditor:
                    {
                        path = ConvertToFileUrl(path);
                        Process.Start("open", $"\"{path}\"");
                        break;
                    }
                    default:
                        UnityEngine.Debug.LogError("不支持的平台");
                        break;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"打开目录失败: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"打开目录失败:\n{e.Message}", "确定");
            }
        }

        /// <summary>
        /// 将普通路径转换为file://格式
        /// </summary>
        private static string ConvertToFileUrl(string path)
        {
            // 确保路径使用正斜杠
            path = path.Replace("\\", "/");

            // 添加file://前缀
            // 注意：标准的file://协议是三个斜杠 file:///路径
            if (path.StartsWith("file://")) return path;
            // 确保路径以/开头
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            return "file://" + path;
        }

        private static string GetCurrentUnityInstallPath()
        {
            // EditorApplication.applicationPath 返回的是 Unity.exe 的完整路径
            // Windows: C:\Program Files\Unity\Editor\Unity.exe
            // Mac: /Applications/Unity/Unity.app/Contents/MacOS/Unity
            var executablePath = EditorApplication.applicationPath;

            // 获取所在目录
            var installPath = Path.GetDirectoryName(executablePath);

            // Mac上需要返回.app包路径
            if (Application.platform != RuntimePlatform.OSXEditor) return installPath;
            // 从 .../Unity.app/Contents/MacOS/Unity 返回到 .../Unity.app
            if (installPath != null && installPath.Contains("Unity.app/Contents/MacOS"))
            {
                installPath = installPath.Replace("/Contents/MacOS", "");
            }

            return installPath;
        }


        [System.Serializable]
        private class CustomDirectory
        {
            public string name;
            public string path;
        }

        [System.Serializable]
        private class CustomDirectoryList
        {
            public List<CustomDirectory> directories;
        }
    }
}
#endif