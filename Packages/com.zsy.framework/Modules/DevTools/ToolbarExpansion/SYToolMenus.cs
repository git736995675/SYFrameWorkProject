using UnityEditor;
using UnityEngine;

namespace ZSYFramework.DevTools
{
    /// <summary>
    /// SY-Tool 统一菜单常量。所有模块的编辑器工具菜单统一挂到 SY-Tool 根目录下，
    /// 路径用 <see cref="Root"/> + 模块分组名拼接；新工具请在对应分组下追加，
    /// 不要再自建 SY_Tools / SY-Tool / Tools/GCZ 等散落前缀。
    ///
    /// 分组规划（每组 priority 间隔 100，组内从 1 递增，为后续插入留空间）：
    /// - SY-Tool/AssetBundle/   资源打包与加载相关
    /// - SY-Tool/表格/          配置表相关
    /// - SY-Tool/UI/            UI 生成与调试
    /// - SY-Tool/热更/          版本更新与热更调试（预留）
    /// - SY-Tool/网络/          网络调试（预留）
    /// - SY-Tool/开发工具/      通用开发辅助
    /// - SY-Tool/工具/          历史遗留的通用小工具
    /// - SY-Tool/文件编辑/      文件批处理工具
    /// </summary>
    public static class SYToolMenus
    {
        /// <summary>统一菜单根路径。</summary>
        public const string Root = "SY-Tool";

        /// <summary>资源打包分组。</summary>
        public const string AssetBundle = Root + "/AssetBundle";

        /// <summary>配置表分组。</summary>
        public const string Table = Root + "/表格";

        /// <summary>UI 分组。</summary>
        public const string UI = Root + "/UI";

        /// <summary>热更分组（预留：版本更新、热更 dll 查看等）。</summary>
        public const string HotUpdate = Root + "/热更";

        /// <summary>网络分组（预留：HTTP 请求调试等）。</summary>
        public const string Network = Root + "/网络";

        /// <summary>通用开发辅助分组。</summary>
        public const string DevTools = Root + "/开发工具";

        /// <summary>通用小工具分组（历史遗留）。</summary>
        public const string Utils = Root + "/工具";

        /// <summary>文件批处理分组（历史遗留）。</summary>
        public const string FileEditing = Root + "/文件编辑";
    }

    /// <summary>
    /// SY-Tool 工具总览窗口：集中列出全部框架编辑器工具入口。
    /// 后续新增工具时在 <see cref="BuildEntries"/> 中追加一行即可；
    /// 点击条目直接执行对应菜单命令，与从菜单栏进入等效。
    /// </summary>
    public sealed class SYToolLauncherWindow : EditorWindow
    {
        // (分组, 名称, 菜单完整路径) —— 与各模块 MenuItem 保持一致
        private static readonly (string group, string name, string menu)[] Entries =
        {
            ("AssetBundle", "AssetBundles管理", SYToolMenus.AssetBundle + "/AssetBundles管理"),
            ("AssetBundle", "打包构建", SYToolMenus.AssetBundle + "/打包构建"),
            ("表格", "表格管理", SYToolMenus.Table + "/表格管理"),
            ("UI", "UI生成器", SYToolMenus.UI + "/UI生成器"),
            ("开发工具", "脚本变量分配", SYToolMenus.DevTools + "/脚本变量分配"),
            ("开发工具", "打开日志文件", SYToolMenus.DevTools + "/打开日志文件"),
            ("开发工具", "文件拷贝工具", SYToolMenus.DevTools + "/文件拷贝工具"),
            ("开发工具", "文件清理", SYToolMenus.DevTools + "/文件清理"),
            ("工具", "代码行数统计", SYToolMenus.Utils + "/代码行数统计"),
            ("工具", "目录快速打开工具", SYToolMenus.Utils + "/目录快速打开工具"),
            ("工具", "编辑器Icon列表", SYToolMenus.Utils + "/编辑器Icon列表"),
            ("文件编辑", "Bytes-Video互转", SYToolMenus.FileEditing + "/Bytes-Video互转"),
            ("文件编辑", "文件重命名工具", SYToolMenus.FileEditing + "/文件重命名工具"),
            ("文件编辑", "文件加解密工具", SYToolMenus.FileEditing + "/文件加解密工具"),
            ("文件编辑", "文件信息导出工具", SYToolMenus.FileEditing + "/文件信息导出工具"),
        };

        private Vector2 _scroll;

        [MenuItem(SYToolMenus.Root + "/工具总览", false, -100)]
        private static void Open()
        {
            var window = GetWindow<SYToolLauncherWindow>("SY-Tool 总览");
            window.minSize = new Vector2(320, 380);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("ZSY Framework 编辑器工具总览。点击条目打开对应工具。", MessageType.Info);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            var currentGroup = "";
            foreach (var (group, name, menu) in Entries)
            {
                if (group != currentGroup)
                {
                    if (currentGroup != "") EditorGUILayout.Space(6);
                    EditorGUILayout.LabelField(group, EditorStyles.boldLabel);
                    currentGroup = group;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(name, EditorStyles.miniButton, GUILayout.Height(22)))
                    {
                        EditorApplication.ExecuteMenuItem(menu);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
