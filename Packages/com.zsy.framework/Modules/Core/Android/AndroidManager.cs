using UnityEngine;
using ZSYFramework.Core.Json;

namespace ZSYFramework.Core;
/// <summary>
/// Android 平台桥接管理器。封装 Java 层调用与平台判断。
/// </summary>
public class AndroidManager
{
private static AndroidJavaObject mjavaObjectShowToast;

private static AndroidJavaObject JavaObjectShowToast =>
    mjavaObjectShowToast ??= new AndroidJavaObject("com.zsy.callandroid.Unity2Android");

private static AndroidJavaClass _unityClass = new("com.unity3d.player.UnityPlayer");

private static AndroidJavaObject _androidJavaObject;

private static AndroidJavaObject AndroidJavaObject
{
    get
    {
        if (_androidJavaObject == null)
        {
#if UNITY_EDITOR
            _androidJavaObject = new AndroidJavaObject("");
#elif UNITY_ANDROID
            AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            _androidJavaObject = jc.GetStatic<AndroidJavaObject>("currentActivity");
#endif
        }

        return _androidJavaObject;
    }
}


/// <summary>
/// 获取手机系统语言
/// </summary>
/// <returns></returns>
public static string GetLocalLanguage()
{
    var locale = AndroidJavaObject.Call<AndroidJavaObject>("getResources")
        .Call<AndroidJavaObject>("getConfiguration").Get<AndroidJavaObject>("locale");
    return locale.Call<string>("getLanguage");
}

/// <summary>
/// 显示Toast
/// </summary>
public static void ShowToast(string str)
{
    try
    {
        JavaObjectShowToast.Call<bool>("showToast", str);
    }
    catch (System.Exception e)
    {
        Debug.LogError("ShowToast error:" + e.ToString());
    }
}

/// <summary>
/// 获取安卓系统版本
/// </summary>
/// <returns></returns>
public static int GetSdkInt()
{
#if UNITY_ANDROID && !UNITY_EDITOR
    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
    {
        return version.GetStatic<int>("SDK_INT");
    }
#endif
    return -1;
}


public static void InstallApk()
{
}
}
