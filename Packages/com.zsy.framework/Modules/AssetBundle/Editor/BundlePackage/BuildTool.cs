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
// Ĭ�ϴ�����·��
private static string DefaultBuildPath => Path.Combine(Application.dataPath, "../Builds");

// ��ǰƽ̨�����׺
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
/// ����Ӧ�û�����Ϣ
/// </summary>
public static void SetAppInfo(string productName, string bundleIdentifier, string version, int buildNumber = 1)
{
    PlayerSettings.productName = productName;
    PlayerSettings.applicationIdentifier = bundleIdentifier;
    PlayerSettings.bundleVersion = version;

    // ƽ̨�ض��İ汾����
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
            // PC/Macƽ̨��������ض�����
            break;
    }
}

/// <summary>
/// ��ȡ��ǰӦ����Ϣ
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
/// Webgl��������
/// </summary>
private static void WebGlSetting()
{
    if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL) return;

    PlayerSettings.WebGL.decompressionFallback = true;
    PlayerSettings.WebGL.dataCaching = true;
}

/// <summary>
/// ���ð�׿ǩ��
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
/// ������Ŀ
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

        // �����ļ���
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

        // ȷ�����Ŀ¼����
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }

        // ����ѡ��
        var options = autoRun ? BuildOptions.AutoRunPlayer : BuildOptions.None;
        if (developmentBuild)
        {
            options |= BuildOptions.Development;
            options |= BuildOptions.AllowDebugging;
        }

        var fullPath = Path.Combine(buildPath, fileName);

        if (abBuild)
        {
            ResourceBuilderWindow.ReadPath();
            ResourceBuilderWindow.BuildBundles(EditorUserBuildSettings.activeBuildTarget);
        }

        // ��ʼ����
        BuildPipeline.BuildPlayer(GetEnabledScenes(), fullPath, EditorUserBuildSettings.activeBuildTarget,
            options);

        // ������ɺ���ļ���
        EditorUtility.RevealInFinder(buildPath);
        EditorUtility.DisplayDialog("Build Complete", $"Build successful!\nPath: {fullPath}", "OK");
    }
    catch (Exception e)
    {
        EditorUtility.DisplayDialog("Build Failed", $"Build failed with error: {e.Message}", "OK");
    }
}

/// <summary>
/// �л�ƽ̨������
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

    // ƽ̨�л����첽�ģ���Ҫ�ȴ����
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

// ��ȡ�������õĳ���
private static string[] GetEnabledScenes()
{
    return (from scene in EditorBuildSettings.scenes where scene.enabled select scene.path).ToArray();
}
}
}
