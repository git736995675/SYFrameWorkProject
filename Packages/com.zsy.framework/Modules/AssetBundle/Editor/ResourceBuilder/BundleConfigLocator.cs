using System.IO;
using UnityEngine;
namespace ZSYFramework.AssetBundle.Editor
{
/// <summary>
/// AB 打包配置文件(BundleResPath.txt)的定位器。
/// 配置文件跟随使用工程：优先 Assets/Config/，不存在则自动创建。
/// (框架包以 Samples 形式分发 Demo 时，路径随导入位置变化，故不能写死)
/// </summary>
public static class BundleConfigLocator
{
private const string ConfigDirectoryRelative = "Assets/Config";

public static string ConfigDirectory
{
    get
    {
        var full = Path.Combine(Application.dataPath, "Config");
        if (!Directory.Exists(full))
        {
            Directory.CreateDirectory(full);
        }
        return full;
    }
}
}
}
