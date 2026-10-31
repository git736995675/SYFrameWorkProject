using System.IO;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;


namespace ZSYFramework.AssetBundle.Editor
{
/// <summary>
/// 打包自动化流程。命令行打包入口与构建后处理。
/// </summary>
public static class BuildAutomation
{
// 默认打包输出路径
private static string DefaultBuildPath => Path.Combine(Application.dataPath, "../Builds");

public static void PerformBuild()
{
    Debug.Log("=== 构建开始 ===");

    // 从命令行获取参数
    var buildPath = GetArgument("customBuildPath");
    var buildTargetStr = GetArgument("buildTarget");
    Debug.Log($"接收参数 - buildPath: {buildPath}");
    Debug.Log($"接收参数 - buildTarget: {buildTargetStr}");

    if (string.IsNullOrEmpty(buildPath))
    {
        buildPath = Path.Combine(Path.Combine(Application.dataPath, "../Builds"), buildTargetStr);
        Debug.Log($"使用默认路径: {buildPath}");
    }

    // 收集场景
    var scenes = EditorBuildSettings.scenes
        .Where(s => s.enabled)
        .Select(s => s.path)
        .ToArray();

    if (scenes.Length == 0)
    {
        Debug.LogError("没有可用的场景！请检查Build Settings");
        EditorApplication.Exit(1);
        return;
    }

    Debug.Log($"将打包以下场景:\n{string.Join("\n", scenes)}");

    // 解析构建目标
    BuildTarget buildTarget;
    try
    {
        buildTarget = (BuildTarget)System.Enum.Parse(typeof(BuildTarget), buildTargetStr);
    }
    catch
    {
        Debug.LogError($"无效的构建目标: {buildTargetStr}");
        EditorApplication.Exit(1);
        return;
    }

    // 执行构建
    try
    {
        WebGlSetting(buildTarget);
        //AndroidSetting(buildTarget);

        // 构建文件名
        var productName = PlayerSettings.productName;
        var version = PlayerSettings.bundleVersion;
        var platform = EditorUserBuildSettings.activeBuildTarget.ToString();
        var dateTime = DateTime.Now.ToString("yyyyMMdd_HHmm");
        var fileName = $"{productName}_{platform}_v{version}_{dateTime}{PlatformSuffix(buildTarget)}";
        if (!buildPath.Contains(platform))
            buildPath = Path.Combine(buildPath, platform);
        if (buildTarget is BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64)
        {
            buildPath = Path.Combine(buildPath, $"{productName}_{platform}_v{version}_{dateTime}");
            fileName = $"{productName}{PlatformSuffix(buildTarget)}";
        }

        // 验证路径
        try
        {
            Directory.CreateDirectory(buildPath);
            Debug.Log($"成功创建输出目录: {buildPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"目录准备失败: {e}");
            EditorApplication.Exit(1);
            return;
        }

        var fullPath = Path.Combine(buildPath, fileName);
        BuildBundlesForPlayer(buildTarget);
        BuildPipeline.BuildPlayer(scenes, fullPath, buildTarget, BuildOptions.None);
        Debug.Log("=== 构建成功 ===");

        // 打印生成的文件
        Debug.Log("生成的文件:");
        foreach (var file in Directory.GetFiles(buildPath))
        {
            Debug.Log(file);
        }

        EditorUtility.RevealInFinder(buildPath);
    }
    catch (System.Exception e)
    {
        Debug.LogError($"构建失败: {e}");
        EditorApplication.Exit(1);
    }
}

private static string GetArgument(string name)
{
    var args = System.Environment.GetCommandLineArgs();
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "-" + name && i + 1 < args.Length)
        {
            return args[i + 1];
        }
    }

    return null;
}

/// <summary>
/// Webgl独立设置
/// </summary>
private static void WebGlSetting(BuildTarget buildTarget)
{
    if (buildTarget != BuildTarget.WebGL) return;

    PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
    PlayerSettings.WebGL.decompressionFallback = true;
    PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
}

/// <summary>
/// 配置安卓签名
/// </summary>
private static void AndroidSetting(BuildTarget buildTarget)
{
    if (buildTarget != BuildTarget.Android) return;
    PlayerSettings.Android.useCustomKeystore = true;
    const string keyPass = "test123";
    PlayerSettings.Android.keystoreName = $"KeyStore/{keyPass}.keystore";
    PlayerSettings.Android.keystorePass = keyPass;
    PlayerSettings.Android.keyaliasName = keyPass;
    PlayerSettings.Android.keyaliasPass = keyPass;
}

// 当前平台打包后缀
private static string PlatformSuffix(BuildTarget buildTarget)
{
    return buildTarget switch
    {
        BuildTarget.Android => ".apk",
        BuildTarget.iOS => "",
        BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => ".exe",
        BuildTarget.StandaloneOSX => ".app",
        _ => ""
    };
}

/// <summary>
/// 出包前构建 AssetBundles（走 BundleSystem 显式构建管线）。
/// 版本号取当前时间戳，与 BundleSystemWindow 手动构建保持同一数据源。
/// </summary>
private static void BuildBundlesForPlayer(BuildTarget target)
{
    var rules = AssetDatabase.FindAssets("t:BundleRuleAsset")
        .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
        .Select(path => AssetDatabase.LoadAssetAtPath<BundleRuleAsset>(path))
        .Where(r => r != null && r.TargetAssetPath != null)
        .ToList();
    if (rules.Count == 0)
    {
        Debug.LogWarning("[BuildTool] 工程内没有打包规则（BundleRuleAsset），跳过 AB 构建");
        return;
    }

    var version = DateTime.Now.ToString("yyyyMMdd_HHmm");
    if (!BundleBuilder.Build(rules, target, version))
    {
        throw new InvalidOperationException("AssetBundle 构建失败，中止出包");
    }
}
}
}
