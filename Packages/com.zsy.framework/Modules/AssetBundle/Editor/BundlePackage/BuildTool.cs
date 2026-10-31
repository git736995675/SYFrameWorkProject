using System.IO;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;


namespace ZSYFramework.AssetBundle.Editor
{
/// <summary>
/// 打包工具核心逻辑。
/// </summary>
public static class BuildTool
{
// 默认打包输出路径
private static string DefaultBuildPath => Path.Combine(Application.dataPath, "../Builds");

// 当前平台安装包后缀
private static string PlatformSuffix
{
    get
    {
        switch (EditorUserBuildSettings.activeBuildTarget)
        {
            case BuildTarget.Android:
                return ".apk";
            case BuildTarget.iOS:
                return "";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return ".exe";
            case BuildTarget.StandaloneOSX:
                return ".app";
            default:
                return "";
        }
    }
}

/// <summary>
/// 设置应用基础信息
/// </summary>
public static void SetAppInfo(string productName, string bundleIdentifier, string version, int buildNumber = 1)
{
    PlayerSettings.productName = productName;
    PlayerSettings.applicationIdentifier = bundleIdentifier;
    PlayerSettings.bundleVersion = version;

    // 平台特定的版本字段
    switch (EditorUserBuildSettings.activeBuildTarget)
    {
        case BuildTarget.Android:
            PlayerSettings.Android.bundleVersionCode = buildNumber;
            break;
        case BuildTarget.iOS:
            PlayerSettings.iOS.buildNumber = buildNumber.ToString();
            break;
        case BuildTarget.StandaloneWindows:
        case BuildTarget.StandaloneWindows64:
        case BuildTarget.StandaloneOSX:
            // PC/Mac 平台无版本号特定字段
            break;
    }
}

/// <summary>
/// 获取当前应用信息
/// </summary>
public static void GetAppInfo(out string productName, out string bundleIdentifier, out string version,
    out int buildNumber)
{
    productName = PlayerSettings.productName;
    bundleIdentifier = PlayerSettings.applicationIdentifier;
    version = PlayerSettings.bundleVersion;
    buildNumber = 1;
    switch (EditorUserBuildSettings.activeBuildTarget)
    {
        case BuildTarget.Android:
            buildNumber = PlayerSettings.Android.bundleVersionCode;
            break;
        case BuildTarget.iOS:
            int.TryParse(PlayerSettings.iOS.buildNumber, out buildNumber);
            break;
    }
}

/// <summary>
/// WebGL 平台设置
/// </summary>
private static void WebGlSetting()
{
    if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL) return;

    PlayerSettings.WebGL.decompressionFallback = true;
    PlayerSettings.WebGL.dataCaching = true;
}

/// <summary>
/// 配置安卓签名
/// </summary>
private static void AndroidSetting()
{
    if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) return;
    PlayerSettings.Android.useCustomKeystore = true;
    var mKeyPass = "test123";
    PlayerSettings.Android.keystoreName = $"KeyStore/{mKeyPass}.keystore";
    PlayerSettings.Android.keystorePass = mKeyPass;
    PlayerSettings.Android.keyaliasName = mKeyPass;
    PlayerSettings.Android.keyaliasPass = mKeyPass;
}

/// <summary>
/// 执行打包
/// </summary>
public static void Build(string buildPath = null, bool developmentBuild = false, bool autoRun = true,
    bool abBuild = true)
{
    try
    {
        if (string.IsNullOrEmpty(buildPath))
        {
            buildPath = DefaultBuildPath;
        }

        WebGlSetting();
        // AndroidSetting();

        // 拼接文件名
        var productName = PlayerSettings.productName;
        var version = PlayerSettings.bundleVersion;
        var platform = EditorUserBuildSettings.activeBuildTarget.ToString();
        var dateTime = DateTime.Now.ToString("yyyyMMdd_HHmm");
        var fileName = $"{productName}_{platform}_v{version}_{dateTime}{PlatformSuffix}";
        buildPath = Path.Combine(buildPath, platform);
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows ||
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
        {
            buildPath = Path.Combine(buildPath, $"{productName}_{platform}_v{version}_{dateTime}");
            fileName = $"{productName}{PlatformSuffix}";
        }

        // 确保输出目录存在
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }

        // 构建选项
        var options = autoRun ? BuildOptions.AutoRunPlayer : BuildOptions.None;
        if (developmentBuild)
        {
            options |= BuildOptions.Development;
            options |= BuildOptions.AllowDebugging;
        }

        var fullPath = Path.Combine(buildPath, fileName);

        if (abBuild)
        {
            BuildBundlesForPlayer(EditorUserBuildSettings.activeBuildTarget);
        }

        // 开始打包
        BuildPipeline.BuildPlayer(GetEnabledScenes(), fullPath, EditorUserBuildSettings.activeBuildTarget,
            options);

        // 打包完成后打开文件夹
        EditorUtility.RevealInFinder(buildPath);
        EditorUtility.DisplayDialog("Build Complete", $"Build successful!\nPath: {fullPath}", "OK");
    }
    catch (Exception e)
    {
        EditorUtility.DisplayDialog("Build Failed", $"Build failed with error: {e.Message}", "OK");
    }
}

/// <summary>
/// 切换平台并打包
/// </summary>
private static void SwitchPlatformAndBuild(BuildTarget targetPlatform, string buildPath = null,
    bool developmentBuild = false, bool autoRun = true, bool abBuild = true)
{
    if (EditorUserBuildSettings.activeBuildTarget == targetPlatform)
    {
        Build(buildPath, developmentBuild, autoRun, abBuild);
        return;
    }

    EditorUtility.DisplayProgressBar("Building", $"Switching platform to {targetPlatform}...", 0.1f);
    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(targetPlatform),
        targetPlatform);

    // 平台切换是异步的，需要等待完成
    EditorApplication.delayCall += () =>
    {
        EditorApplication.delayCall = null;
        EditorUtility.ClearProgressBar();
        Build(buildPath, developmentBuild, autoRun, abBuild);
    };
}

public static void BuildAndroid(string buildPath = null, bool developmentBuild = false, bool autoRun = true,
    bool abBuild = true)
{
    SwitchPlatformAndBuild(BuildTarget.Android, buildPath, developmentBuild, autoRun, abBuild);
}

public static void BuildIos(string buildPath = null, bool developmentBuild = false, bool autoRun = true,
    bool abBuild = true)
{
    SwitchPlatformAndBuild(BuildTarget.iOS, buildPath, developmentBuild, autoRun, abBuild);
}

public static void BuildWindows(string buildPath = null, bool developmentBuild = false, bool autoRun = true,
    bool abBuild = true)
{
    SwitchPlatformAndBuild(BuildTarget.StandaloneWindows64, buildPath, developmentBuild, autoRun,
        abBuild);
}

public static void BuildMac(string buildPath = null, bool developmentBuild = false, bool autoRun = true,
    bool abBuild = true)
{
    SwitchPlatformAndBuild(BuildTarget.StandaloneOSX, buildPath, developmentBuild, autoRun, abBuild);
}

public static void BuildWebGL(string buildPath = null, bool developmentBuild = false, bool autoRun = true,
    bool abBuild = true)
{
    SwitchPlatformAndBuild(BuildTarget.WebGL, buildPath, developmentBuild, autoRun, abBuild);
}

// 获取所有启用的场景
private static string[] GetEnabledScenes()
{
    return (from scene in EditorBuildSettings.scenes where scene.enabled select scene.path).ToArray();
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
