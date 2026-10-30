using System.Collections.Generic;
using UnityEngine;
using ZSYFramework.Core;

namespace ZSYFramework.HotUpdate
{
    /// <summary>
    /// 热更配置。通过 Assets → Create → ZSYFramework → HotUpdate Settings 创建，
    /// 放入任意 Resources 目录（保持默认文件名 HotUpdateSettings）即可被 <see cref="Load"/> 找到。
    /// </summary>
    [CreateAssetMenu(fileName = "HotUpdateSettings", menuName = "ZSYFramework/HotUpdate Settings", order = 0)]
    public class HotUpdateSettings : ScriptableObject
    {
        [Header("CDN 根地址（以 / 结尾，版本清单与 AB 文件的下载基准）")]
        public string cdnRootUrl = "";

        [Header("版本清单文件名（位于 CDN 根目录与 HotRes 目录）")]
        public string versionFileName = "version.json";

        [Header("批量下载并发数")]
        public int maxConcurrency = 4;

        [Header("热更程序集名（不含 .dll 后缀，与 HybridCLR 热更程序集列表一致）")]
        public List<string> hotUpdateAssemblies = new();

        [Header("需补充元数据的 AOT 程序集名（不含 .dll 后缀）")]
        public List<string> aotMetaAssemblies = new();

        private static HotUpdateSettings _instance;

        /// <summary>从 Resources 目录加载全局配置（资源名 HotUpdateSettings），找不到时返回空配置。</summary>
        public static HotUpdateSettings Load()
        {
            if (_instance != null) return _instance;
            _instance = Resources.Load<HotUpdateSettings>("HotUpdateSettings");
            if (_instance == null)
            {
                SYLog.Warning("[HotUpdate] 未在 Resources 中找到 HotUpdateSettings，使用空配置", LogCategory.HotUpdate);
                _instance = CreateInstance<HotUpdateSettings>();
            }

            return _instance;
        }
    }
}
