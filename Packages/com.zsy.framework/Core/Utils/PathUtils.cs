using System.IO;
using UnityEngine;

namespace SY
{
    public class PathUtils
    {
        /// <summary>
        /// 绝对路径转Asset路径
        /// </summary>
        public static string AbsoluteToAssetPath(string absolutePath)
        {
            try
            {
                // 规范化路径
                var fullPath = Path.GetFullPath(absolutePath).Replace("\\", "/");
                var assetsFullPath = Path.GetFullPath(Application.dataPath).Replace("\\", "/");

                // 检查是否在Assets目录下
                if (fullPath.StartsWith(assetsFullPath))
                    return "Assets" + fullPath[assetsFullPath.Length..];

                Debug.LogError("Path is not under Assets folder: " + absolutePath);
                return null;

                // 计算相对路径
            }
            catch (System.Exception e)
            {
                Debug.LogError("Path conversion failed: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Asset路径转为绝对路径
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static string AssetToAbsolutePath(string assetPath)
        {
            if (!assetPath.StartsWith("Assets"))
            {
                Debug.LogError("Not a valid asset path: " + assetPath);
                return null;
            }

            var assetsPath = Application.dataPath;
            return Path.Combine(assetsPath[..^"Assets".Length], assetPath).Replace("\\", "/");
        }

        /// <summary>
        /// 获取Assets相对路径的上一级目录名
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static string GetParentDirectoryName(string path)
        {
            // 首先获取完整目录路径
            var directoryPath = Path.GetDirectoryName(path);

            // 然后获取该目录的父目录名
            var parentDirectoryName = Path.GetFileName(directoryPath);

            return parentDirectoryName;
        }
        
    }
}