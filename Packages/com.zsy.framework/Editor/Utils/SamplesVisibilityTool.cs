#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZSYFramework.Editor.Utils
{
    /// <summary>
    /// Samples~ 可见性切换工具。
    /// 把 Packages/com.zsy.framework/Samples~ 下的内容临时改名为 Samples（去掉 ~），
    /// 让 Unity 立即导入，方便在管理工程里直接调试 Demo / 插件；
    /// 再点一次改回 Samples~（隐藏），恢复"面板 Import"形态。
    /// 提交推送前务必切回隐藏态（工具菜单会标红提醒）。
    /// </summary>
    public static class SamplesVisibilityTool
    {
        private const string PackageRoot = "Packages/com.zsy.framework";
        private const string HiddenName = "Samples~";
        private const string VisibleName = "Samples";
        private const string MenuRoot = "SY_Tools/Samples 显示与隐藏/";

        [MenuItem(MenuRoot + "显示（临时去掉 ~，可直接调试）", priority = 100)]
        public static void Show()
        {
            if (IsVisible())
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
            Debug.Log($"[SamplesTool] <color=#4EC9B0>Samples 已显示</color>（{VisiblePath}）。调试完成后请通过菜单切回隐藏态再提交推送！");
        }

        [MenuItem(MenuRoot + "隐藏（恢复 ~，回到面板 Import 形态）", priority = 101)]
        public static void Hide()
        {
            if (!IsVisible())
            {
                Debug.Log("[SamplesTool] 已经是隐藏状态");
                return;
            }

            AssetDatabase.Refresh();
            Directory.Move(VisiblePath, HiddenPath);
            AssetDatabase.Refresh();
            Debug.Log($"[SamplesTool] <color=#4EC9B0>Samples 已隐藏</color>（{HiddenPath}），可以安全提交推送。");
        }

        [MenuItem(MenuRoot + "当前状态", priority = 102)]
        public static void Report()
        {
            if (IsVisible())
            {
                Debug.Log($"[SamplesTool] 当前: <color=#E2B93B>显示中</color>（{VisiblePath}）—— 提交前请先隐藏");
            }
            else if (Directory.Exists(HiddenPath))
            {
                Debug.Log($"[SamplesTool] 当前: 隐藏中（{HiddenPath}）");
            }
            else
            {
                Debug.LogError($"[SamplesTool] 两个目录都不存在，请检查 {PackageRoot}");
            }
        }

        // 显示态菜单项标红提示（checkbox 不可用时最直观的方式）
        [MenuItem(MenuRoot + "显示（临时去掉 ~，可直接调试）", true)]
        public static bool ShowValidate() => !IsVisible();

        [MenuItem(MenuRoot + "隐藏（恢复 ~，回到面板 Import 形态）", true)]
        public static bool HideValidate() => IsVisible();

        private static bool IsVisible() => Directory.Exists(VisiblePath) && !Directory.Exists(HiddenPath);

        private static string HiddenPath => Path.Combine(PackageRoot, HiddenName);
        private static string VisiblePath => Path.Combine(PackageRoot, VisibleName);
    }
}
#endif
