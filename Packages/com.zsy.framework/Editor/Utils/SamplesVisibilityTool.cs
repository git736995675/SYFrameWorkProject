#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZSYFramework.Editor.Utils
{
    /// <summary>
    /// Samples~ 可见性切换工具。
    /// 把 Packages/com.zsy.framework/Samples~ 下的内容临时改名为 Samples（去掉 ~），
    /// 让 Unity 立即导入，方便在管理工程里直接调试 Demo / 插件；
    /// 再切回 Samples~（隐藏），恢复"面板 Import"形态。
    /// 提交推送前务必切回隐藏态。
    /// </summary>
    public static class SamplesVisibilityTool
    {
        private const string PackageRoot = "Packages/com.zsy.framework";
        private const string HiddenName = "Samples~";
        private const string VisibleName = "Samples";

        /// <summary>当前是否处于"显示"状态（目录名无 ~）</summary>
        public static bool IsVisible => Directory.Exists(VisiblePath) && !Directory.Exists(HiddenPath);

        /// <summary>切换显示/隐藏。返回切换后的状态（true=显示中）</summary>
        public static bool Toggle()
        {
            if (IsVisible)
            {
                Hide();
                return false;
            }

            Show();
            return true;
        }

        public static void Show()
        {
            if (IsVisible)
            {
                Debug.Log("[SamplesTool] 已经是显示状态");
                return;
            }

            if (!Directory.Exists(HiddenPath))
            {
                Debug.LogError($"[SamplesTool] 未找到 {HiddenPath}");
                return;
            }

            AssetDatabase.Refresh();
            Directory.Move(HiddenPath, VisiblePath);
            AssetDatabase.Refresh();
            Debug.Log($"[SamplesTool] <color=#4EC9B0>Samples 已显示</color>（{VisiblePath}）。提交推送前请先切回隐藏态！");
        }

        public static void Hide()
        {
            if (!IsVisible)
            {
                Debug.Log("[SamplesTool] 已经是隐藏状态");
                return;
            }

            AssetDatabase.Refresh();
            Directory.Move(VisiblePath, HiddenPath);
            AssetDatabase.Refresh();
            Debug.Log($"[SamplesTool] <color=#4EC9B0>Samples 已隐藏</color>（{HiddenPath}），可以安全提交推送。");
        }

        private static string HiddenPath => Path.Combine(PackageRoot, HiddenName);
        private static string VisiblePath => Path.Combine(PackageRoot, VisibleName);
    }
}
#endif
