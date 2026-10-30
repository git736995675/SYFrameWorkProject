using UnityEngine;

namespace ZSYFramework.UI
{
/// <summary>
/// 视图配置资产。全局 UI 框架参数。
/// </summary>
public class ViewSetting
{
// 是否是vr模式
public static bool IsVr { get; set; } = false;

// vr模式摄像机
public static Camera Camera { get; set; }

// 摄像机到ui的距离
public static float CameraZoom { get; set; } = 80;

// ui是否跟随摄像机
public static bool IsFollow { get; set; } = false;
}
}
