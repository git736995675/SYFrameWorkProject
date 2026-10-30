using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZSYFramework.AssetBundle
{
    /// <summary>
    /// Bundle 清单（manifest.json）。构建期由 BundleManifestGenerator 生成，
    /// 运行时由 BundleManifestManager 加载；加载/热更都以它为唯一事实来源。
    /// </summary>
    [Serializable]
    public class BundleManifest
    {
        /// <summary>资源版本号。</summary>
        public string version = "1";

        /// <summary>目标平台。</summary>
        public string platform = "";

        /// <summary>bundle 列表。</summary>
        public List<BundleFileInfo> bundles = new();

        /// <summary>资源名 → bundle 名映射（运行时按资源名定位所在包）。</summary>
        public SerializableStringBundleMap resources = new();

        /// <summary>按名查找 bundle 信息。</summary>
        public BundleFileInfo GetBundle(string name)
        {
            return bundles.Find(b => b.name == name);
        }

        /// <summary>查询资源所在的 bundle 名，找不到返回 null。</summary>
        public string GetBundleOf(string resourceName)
        {
            return resources.TryGetValue(resourceName, out var bundle) ? bundle : null;
        }
    }

    /// <summary>清单中的单个 bundle 条目。</summary>
    [Serializable]
    public class BundleFileInfo
    {
        /// <summary>bundle 文件名（即包名）。</summary>
        public string name;

        /// <summary>文件 MD5。</summary>
        public string md5;

        /// <summary>文件字节数。</summary>
        public long size;

        /// <summary>是否远程（首包不内置，需从 CDN 下载）。</summary>
        public bool isRemote;

        /// <summary>依赖的 bundle 名列表。</summary>
        public List<string> dependencies = new();
    }
}
