using System.Collections.Generic;

namespace ZSYFramework.HotUpdate
{
    /// <summary>
    /// 版本清单（version.json）。由打包侧生成并上传 CDN，运行时与本地清单比对差异后增量下载。
    /// </summary>
    public class VersionManifest
    {
        /// <summary>版本号（建议与 AB 打包时的资源版本一致，如日期或自增数）。</summary>
        public string version;

        /// <summary>文件清单。</summary>
        public List<VersionFileInfo> files = new();

        /// <summary>按文件名建立索引，便于比对差异。</summary>
        public Dictionary<string, VersionFileInfo> ToDictionary()
        {
            var dict = new Dictionary<string, VersionFileInfo>();
            if (files == null) return dict;
            foreach (var file in files)
            {
                if (!string.IsNullOrEmpty(file.name)) dict[file.name] = file;
            }

            return dict;
        }
    }

    /// <summary>版本清单中的单个文件。</summary>
    public class VersionFileInfo
    {
        /// <summary>相对路径（含 bundle 文件名，使用正斜杠）。</summary>
        public string name;

        /// <summary>文件 MD5（32 位十六进制）。</summary>
        public string md5;

        /// <summary>文件字节数。</summary>
        public long size;
    }
}
