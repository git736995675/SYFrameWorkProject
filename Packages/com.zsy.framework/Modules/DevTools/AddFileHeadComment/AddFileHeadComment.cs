#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using ZSYFramework.Core.Json;


namespace ZSYFramework.DevTools;
/// <summary>
/// 监听脚本创建事件，自动添加作者和创建时间注释
/// </summary>
public class ScriptAuthorCommentTool : AssetModificationProcessor
{
private static void OnWillCreateAsset(string assetPath)
{
    // 只处理 .cs 脚本文件，忽略 .meta 文件
    if (!assetPath.EndsWith(".cs") || assetPath.EndsWith(".cs.meta"))
        return;

    // 延迟一帧执行，确保 Unity 已完成模板内容填充
    EditorApplication.delayCall += () => AddCommentToCreatedScript(assetPath);
}

private static void AddCommentToCreatedScript(string assetPath)
{
    // 获取文件的完整系统路径
    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
    if (!File.Exists(fullPath))
        return;

    // 读取现有内容
    var content = File.ReadAllText(fullPath);

    // 防止重复添加（检测已存在的作者注释标记）
    if (content.Contains("// 作者:"))
        return;

    // 从 EditorPrefs 读取作者名（若无则使用系统用户名）
    var author = EditorPrefs.GetString("ScriptAuthorName", System.Environment.UserName);
    var creationTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    // 使用当前平台的换行符（自动适配 Windows/macOS/Linux）
    var newLine = System.Environment.NewLine;
    var comment = $"//================================={newLine}" +
                  $"// 作者: {author}{newLine}" +
                  $"// 创建时间: {creationTime}{newLine}" +
                  $"// 描述: {newLine}" +
                  $"//================================={newLine}";

    // 将注释写入文件顶部
    var newContent = comment + content;
    File.WriteAllText(fullPath, newContent, System.Text.Encoding.UTF8);

    // 刷新 AssetDatabase 使修改立即生效
    AssetDatabase.Refresh();
}
}

/// <summary>
/// 提供 Project Settings 界面，让用户配置作者名
/// </summary>
public class ScriptAuthorSettingsProvider : SettingsProvider
{
private ScriptAuthorSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope)
{
}

[SettingsProvider]
public static SettingsProvider CreateMySettingsProvider()
{
    var provider = new ScriptAuthorSettingsProvider("Project/Script Author Comment", SettingsScope.Project)
    {
        label = "Script Author Comment",
        guiHandler = (searchContext) =>
        {
            EditorGUILayout.LabelField("新建脚本自动添加注释的设置", EditorStyles.boldLabel);

            // 获取当前的作者名
            var currentAuthor = EditorPrefs.GetString("ScriptAuthorName", System.Environment.UserName);
            EditorGUI.BeginChangeCheck();
            var newAuthor = EditorGUILayout.TextField("作者名称", currentAuthor);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString("ScriptAuthorName", newAuthor);
            }

            EditorGUILayout.HelpBox("每当你创建一个新的 C# 脚本，顶部会自动添加作者和创建日期的注释。\n作者名称如上设置（默认使用系统用户名）。",
                MessageType.Info);
        },
        keywords = new[] { "script", "author", "comment", "注释", "作者" }
    };
    return provider;
}
}
#endif
