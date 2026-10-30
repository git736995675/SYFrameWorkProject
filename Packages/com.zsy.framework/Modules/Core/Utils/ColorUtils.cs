using UnityEngine;

namespace ZSYFramework.Core
{
/// <summary>
/// 颜色工具类。提供十六进制转 Color、颜色插值等常用方法。
/// </summary>
public static class ColorUtils
{
/// <summary>
/// hax转颜色
/// </summary>
/// <param name="hax"> 不加#号时 可直接输入red, cyan, blue, darkblue, lightblue, purple, yellow, lime, fuchsia, white, silver, grey, black, orange, brown, maroon, green, olive, navy, teal, aqua, magenta 加#号随意 </param>
/// <param name="hax"> 不加#号时 可直接输入 红色，青色，蓝色，深蓝色，浅蓝色，紫色，黄色，石灰，紫红色，白色，银色，灰色，黑色，橙色，棕色，栗色，绿色，橄榄色，海军，蓝绿色，浅绿色，洋红色。 加#号随意 </param>
/// <returns>成功时转换颜色，失败时默认白色</returns>
public static Color GetColorByHax(this string hax)
{
    hax = string.IsNullOrEmpty(hax) ? "#FFFFFF" : hax;
    var isSuccess = ColorUtility.TryParseHtmlString(hax, out var nowColor);
    return isSuccess ? nowColor : Color.white;
}

/// <summary>
/// 颜色转rgb
/// </summary>
/// <param name="color"></param>
/// <returns></returns>
public static string GetRGBByColor(this Color color)
{
    return ColorUtility.ToHtmlStringRGB(color);
}

/// <summary>
/// 颜色转rgba
/// </summary>
/// <param name="color"></param>
/// <returns></returns>
public static string GetRGBAByColor(this Color color)
{
    return ColorUtility.ToHtmlStringRGBA(color);
}
}
}
