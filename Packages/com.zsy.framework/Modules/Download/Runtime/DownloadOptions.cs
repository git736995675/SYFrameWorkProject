namespace ZSYFramework.Download
{
    /// <summary>
    /// 单次下载的配置项。所有字段均有默认值，按需覆盖即可。
    /// </summary>
    public sealed class DownloadOptions
    {
        /// <summary>单次请求的超时秒数。</summary>
        public int TimeoutSeconds = 30;

        /// <summary>失败后的最大重试次数（不含首次）。</summary>
        public int MaxRetries = 3;

        /// <summary>重试基础等待秒数，实际按尝试次数线性递增。</summary>
        public float RetryDelaySeconds = 1f;

        /// <summary>期望的文件 MD5（32 位小写十六进制），非空时下载完成后校验。</summary>
        public string ExpectedMd5;

        /// <summary>期望的文件字节数，大于 0 时下载完成后校验。</summary>
        public long ExpectedSize;
    }

    /// <summary>
    /// 批量下载中的一个文件任务。
    /// </summary>
    public sealed class DownloadTask
    {
        /// <summary>文件下载地址。</summary>
        public string Url;

        /// <summary>保存到的本地完整路径。</summary>
        public string SavePath;

        /// <summary>期望 MD5，可空。</summary>
        public string Md5;

        /// <summary>期望字节数，0 表示不校验。</summary>
        public long Size;

        /// <summary>创建一个下载任务。</summary>
        public DownloadTask(string url, string savePath, string md5 = null, long size = 0)
        {
            Url = url;
            SavePath = savePath;
            Md5 = md5;
            Size = size;
        }
    }
}
