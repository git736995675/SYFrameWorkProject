using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class DirectoryScannerWithFilter : EditorWindow
{
    // --- UI 变量 ---
    private string targetDirectory = "Assets/"; // 目标目录
    private string outputFilePath = "Assets/ScannedFiles.txt"; // 输出路径
    private SearchOption searchOption = SearchOption.TopDirectoryOnly; // 搜索范围
    private string searchPattern = "*.*"; // 搜索通配符
    private string filterKeyword = ""; // 名称包含关键字

    private Vector2 scrollPosition;
    private string[] currentFileList = new string[0]; // 缓存当前列表用于显示

    [MenuItem("Tools/GCZ/输出文件名称列表")]
    public static void ShowWindow()
    {
        GetWindow<DirectoryScannerWithFilter>("Directory Scanner");
    }

    private void OnGUI()
    {
        GUILayout.Label("扫描设置", EditorStyles.boldLabel);

        // 1. 目录选择
        EditorGUILayout.BeginHorizontal();
        targetDirectory = EditorGUILayout.TextField("目标目录:", targetDirectory);
        if (GUILayout.Button("选择", GUILayout.Width(60)))
        {
             targetDirectory = EditorUtility.OpenFolderPanel("选择扫描目录", "Assets", "");
        }
        EditorGUILayout.EndHorizontal();

        // 2. 筛选设置
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("搜索模式:", GUILayout.Width(80));
        searchOption = (SearchOption)EditorGUILayout.EnumPopup(searchOption);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        searchPattern = EditorGUILayout.TextField("文件类型:", searchPattern);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        filterKeyword = EditorGUILayout.TextField("名称包含:", filterKeyword);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 3. 扫描按钮
        if (GUILayout.Button("🔍 扫描并列出"))
        {
            ScanDirectory();
        }

        // 4. 保存按钮
        if (GUILayout.Button("💾 保存到文件"))
        {
            SaveToFile();
        }

        // 5. 结果预览 (滚动视图)
        GUILayout.Label("扫描结果预览:", EditorStyles.boldLabel);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        
        if (currentFileList.Length == 0)
        {
            GUILayout.Label("暂无数据，请点击上方按钮扫描。");
        }
        else
        {
            foreach (string file in currentFileList)
            {
                GUILayout.Label(file);
            }
            GUILayout.Label($"--- 共 {currentFileList.Length} 个文件 ---", EditorStyles.boldLabel);
        }
        
        GUILayout.EndScrollView();
    }

    private void ScanDirectory()
    {
        if (!Directory.Exists(targetDirectory))
        {
            Debug.LogError($"目录不存在: {targetDirectory}");
            currentFileList = new string[0];
            return;
        }

        try
        {
            // 执行搜索
            string[] files = Directory.GetFiles(targetDirectory, searchPattern, searchOption);

            // 应用名称过滤 (如果输入了关键字)
            if (!string.IsNullOrEmpty(filterKeyword))
            {
                System.Collections.Generic.List<string> filteredList = new System.Collections.Generic.List<string>();
                foreach (string file in files)
                {
                    if (Path.GetFileName(file).Contains(filterKeyword))
                    {
                        filteredList.Add(file);
                    }
                }
                files = filteredList.ToArray();
            }

            // 转换为相对路径或仅文件名用于显示 (这里显示相对路径)
            currentFileList = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                currentFileList[i] = "Assets" + files[i].Substring(Application.dataPath.Length);
            }

            Debug.Log($"找到 {currentFileList.Length} 个文件。");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"扫描出错: {e.Message}");
            currentFileList = new string[0];
        }
    }

    private void SaveToFile()
    {
        if (currentFileList.Length == 0)
        {
            Debug.LogWarning("没有数据可保存，请先扫描。");
            return;
        }

        try
        {
            StringBuilder sb = new StringBuilder();
            foreach (string file in currentFileList)
            {
                sb.AppendLine(file);
            }

            // 确保输出目录存在
            string outputDir = Path.GetDirectoryName(outputFilePath);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            File.WriteAllText(outputFilePath, sb.ToString(), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"文件列表已保存至: {outputFilePath}");
            EditorUtility.RevealInFinder(outputFilePath); // 自动定位到文件 (可选)
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存失败: {e.Message}");
        }
    }
}