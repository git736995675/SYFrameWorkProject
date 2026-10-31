using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZSYFramework.AssetBundle.Editor
{
    /// <summary>Bundle 命名模式。注意：枚举底层整数值已随历史资产固化，调整仅改声明顺序（影响 Inspector 下拉显示），勿改动 = 后面的数值。</summary>
    public enum BundleNamingMode
    {
        /// <summary>子目录名：每个一级子目录一个 bundle（经典模式，兼容旧习惯）。默认首选模式。</summary>
        SubDir = 2,

        /// <summary>根目录名：整个打包目录内的所有资源打进同一个 bundle，名字自动取当前文件夹名。</summary>
        RootDir = 1,

        /// <summary>手动：整个目录打包成一个 bundle，包名由用户在文件夹 Inspector 的“手动包名”输入框填写（可自定义）。</summary>
        Manual = 0,

        /// <summary>资源自身名：每个资源文件独立成包（适合大文件如场景、视频）。</summary>
        AssetName = 3,
    }

    /// <summary>远程（CDN 热更）策略。</summary>
    public enum BundleRemoteRule
    {
        /// <summary>全部内置：打进包体 StreamingAssets，不走热更。</summary>
        AllLocal = 0,

        /// <summary>全部远程：整个目录的 bundle 都上 CDN，首包不携带。</summary>
        AllRemote = 1,

        /// <summary>按例外条目：仅 overrides 中标记 remote 的 bundle 走 CDN，其余内置。</summary>
        PerBundle = 2,
    }

    /// <summary>
    /// 单个 bundle 的例外配置：手动命名、单独远程标记。
    /// </summary>
    [Serializable]
    public class BundleOverride
    {
        /// <summary>目标：相对 targetDirectory 的路径（目录或单个资源），使用正斜杠。</summary>
        public string relativePath;

        /// <summary>手动指定的 bundle 名（仅 Manual 命名模式下必需；其他模式下留空即继承自动名）。</summary>
        public string bundleName;

        /// <summary>是否为远程资源（仅 PerBundle 远程策略下生效）。</summary>
        public bool isRemote;
    }

    /// <summary>
    /// AB 打包规则资产。每个参与打包的根目录对应一个规则资产（推荐放在该目录下），
    /// 描述该目录的命名模式、排除规则与远程策略。由 BundleRuleApplier 批量应用到
    /// AssetImporter，由 BundleBuilder 构建产出。
    /// 创建：在 Project 窗口右键 Create → ZSYFramework → Bundle Rule Asset。
    /// </summary>
    [CreateAssetMenu(fileName = "BundleRule", menuName = "ZSYFramework/Bundle Rule Asset", order = 0)]
    public class BundleRuleAsset : ScriptableObject
    {
        [Header("打包目标目录（资源引用型，目录移动时 GUID 不失效）")]
        public DefaultAsset targetDirectory;

        [Header("Bundle 命名模式")]
        public BundleNamingMode namingMode = BundleNamingMode.SubDir;

        /// <summary>Manual 命名模式下整目录使用的自定义包名（在文件夹 Inspector 的“手动包名”输入框填写）。其他模式忽略。</summary>
        [Tooltip("Manual 模式下整个目录打成一个包的名字；其他模式下忽略")]
        public string bundleName = "";

        public BundleRemoteRule remoteRule = BundleRemoteRule.AllLocal;

        [Tooltip("命中任意模式的文件不参与打包，如 *.cs、Test/*、*~")]
        public List<string> excludePatterns = new();

        /// <summary>预留：逐资源/子目录的命名与远程例外。当前命名模式已简化为整目录统一命名，此字段暂不在 Inspector 暴露，保留供高级扩展。</summary>
        [HideInInspector]
        public List<BundleOverride> overrides = new();

        /// <summary>解析目标目录的工程内路径（Assets/...），无效时返回 null。</summary>
        public string TargetAssetPath
        {
            get
            {
                if (targetDirectory == null) return null;
                var path = AssetDatabase.GetAssetPath(targetDirectory);
                return AssetDatabase.IsValidFolder(path) ? path : null;
            }
        }
    }
}
