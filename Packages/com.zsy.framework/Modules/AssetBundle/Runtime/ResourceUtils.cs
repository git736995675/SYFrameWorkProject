using System.IO;
using UnityEngine;

namespace ZSYFramework.AssetBundle
{
    /// <summary>
    /// 资源路径工具集。仅保留跨模块共用的路径常量；
    /// 资源定位已由 <see cref="BundleManifestManager"/>（manifest.json 驱动）接管。
    /// </summary>
    public static class ResourceUtils
    {
        /// <summary>热更资源下载根目录。版本更新器把新版本 AB 下载到这里，加载时优先于包内置资源。</summary>
        public static string HotResDir => Path.Combine(Application.persistentDataPath, "HotRes");
    }
}
