#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZSYFramework.Editor.Utils
{
    /// <summary>
    /// Samples~ 可见性切换工具。
    /// 显示: 把 Samples~ 内容复制到 Samples（Unity 立即导入，可直接调试）；
    /// 隐藏: 把 Samples 里的改动同步回 Samples~ 后删除可见副本。
    /// Samples~ 始终是权威源，Samples 只是调试用的临时副本。
    /// </summary>
    public static class SamplesVisibilityTool
    {
        private const string PackageRoot = "Packages/com.zsy.framework";
        private const string HiddenName = "Samples~";
        private const string VisibleName = "Samples";

        /// <summary>当前是否处于"显示"状态（存在无 ~ 的 Samples 目录）</summary>
        public static bool IsVisible => Directory.Exists(VisiblePath);

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
            CopyDirectory(HiddenPath, VisiblePath);
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

            // 把可见副本的改动(新文件/新meta)同步回权威源, 再删除副本
            SyncBack(VisiblePath, HiddenPath);
            DeleteDirectory(VisiblePath);
            if (File.Exists(VisiblePath + ".meta"))
            {
                File.Delete(VisiblePath + ".meta");
            }

            AssetDatabase.Refresh();
            Debug.Log($"[SamplesTool] <color=#4EC9B0>Samples 已隐藏</color>，改动已同步回 {HiddenPath}，可以安全提交推送。");
        }

        /// <summary>把 src 里有而 dst 没有的文件复制过去（按相对路径对齐，dst 侧同名文件以 src 为准不覆盖）</summary>
        private static void SyncBack(string src, string dst)
        {
            foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(src, file);
                var target = Path.Combine(dst, rel);
                var dir = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (!File.Exists(target) || File.GetLastWriteTimeUtc(file) > File.GetLastWriteTimeUtc(target))
                {
                    File.Copy(file, target, true);
                }
            }
        }

        private static void CopyDirectory(string src, string dst)
        {
            CopyDirectory(new DirectoryInfo(src), dst);
        }

        private static void CopyDirectory(DirectoryInfo src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var file in src.GetFiles())
            {
                file.CopyTo(Path.Combine(dst, file.Name), true);
            }

            foreach (var dir in src.GetDirectories())
            {
                CopyDirectory(dir, Path.Combine(dst, dir.Name));
            }
        }

        private static void DeleteDirectory(string path)
        {
            // Unity 资产系统可能持有文件句柄, 重试几次
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    Directory.Delete(path, true);
                    return;
                }
                catch (IOException)
                {
                    if (attempt == 2) throw;
                    System.Threading.Thread.Sleep(100);
                }
            }
        }

        private static string HiddenPath => Path.Combine(PackageRoot, HiddenName);
        private static string VisiblePath => Path.Combine(PackageRoot, VisibleName);
    }
}
#endif
