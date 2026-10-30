using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LitJson;
using ZSYFramework.AssetBundle;
using ZSYFramework.Core;
using ZSYFramework.Download;

namespace ZSYFramework.HotUpdate
{
    /// <summary>版本更新结果。</summary>
    public sealed class VersionUpdateResult
    {
        /// <summary>更新是否成功。</summary>
        public bool Success;

        /// <summary>远端版本号；拉取远端清单失败时为 null。</summary>
        public string Version;

        /// <summary>实际下载的文件数（0 表示本地已是最新）。</summary>
        public int DownloadedCount;

        /// <summary>失败原因，成功时为 null。</summary>
        public string Error;
    }

    /// <summary>
    /// 资源版本更新流程：拉取远端版本清单 → 与本地清单比对差异 → 并发下载变更文件到
    /// <see cref="ResourceUtils.HotResDir"/> → 写回本地清单。
    /// 更新完成后 ABLoadManager / ResourceProvider 会自动优先加载 HotRes 目录中的新版本。
    /// </summary>
    public sealed class VersionUpdater
    {
        private readonly HotUpdateSettings _settings;

        /// <summary>用指定配置创建更新器，传 null 时从 Resources 加载全局配置。</summary>
        public VersionUpdater(HotUpdateSettings settings = null)
        {
            _settings = settings ?? HotUpdateSettings.Load();
        }

        /// <summary>本地版本清单路径。</summary>
        public string LocalManifestPath => Path.Combine(ResourceUtils.HotResDir, _settings.versionFileName);

        /// <summary>
        /// 执行一次检查并更新。差异为空时不产生下载流量；网络失败抛出的信息记录在结果
        /// <see cref="VersionUpdateResult.Error"/> 中，不中断调用方（包内置资源仍可用）。
        /// </summary>
        /// <param name="progress">下载整体进度（0~1，按完成文件数计）。</param>
        /// <param name="ct">取消令牌。</param>
        public async Task<VersionUpdateResult> UpdateAsync(IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            var result = new VersionUpdateResult();
            try
            {
                ct.ThrowIfCancellationRequested();

                // 1. 远端清单（时间戳参数防 CDN 缓存）
                var versionUrl = $"{_settings.cdnRootUrl}{_settings.versionFileName}?t={DateTime.UtcNow.Ticks}";
                var remoteJson = await Downloader.GetTextAsync(versionUrl, 30, ct);
                var remote = JsonMapper.ToObject<VersionManifest>(remoteJson);
                result.Version = remote.version;

                // 2. 与本地清单比对差异
                var local = LoadLocalManifest();
                var localIndex = local?.ToDictionary() ?? new Dictionary<string, VersionFileInfo>();
                var changed = new List<DownloadTask>();
                if (remote.files != null)
                {
                    foreach (var file in remote.files)
                    {
                        if (localIndex.TryGetValue(file.name, out var info) &&
                            string.Equals(info.md5, file.md5, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var savePath = Path.Combine(ResourceUtils.HotResDir, file.name);
                        changed.Add(new DownloadTask($"{_settings.cdnRootUrl}{file.name}", savePath, file.md5, file.size));
                    }
                }

                // 3. 并发下载变更文件
                if (changed.Count > 0)
                {
                    await Downloader.DownloadFilesAsync(changed, _settings.maxConcurrency, progress, ct);
                }

                // 4. 写回本地清单
                Directory.CreateDirectory(ResourceUtils.HotResDir);
                File.WriteAllText(LocalManifestPath, JsonMapper.ToJson(remote), Encoding.UTF8);

                result.Success = true;
                result.DownloadedCount = changed.Count;
                SYLog.Info($"[HotUpdate] 版本更新完成: {remote.version}，下载 {changed.Count} 个文件",
                    LogCategory.HotUpdate);
            }
            catch (OperationCanceledException)
            {
                result.Error = "已取消";
                throw;
            }
            catch (Exception e)
            {
                result.Error = e.Message;
                SYLog.Error($"[HotUpdate] 版本更新失败: {e.Message}", LogCategory.HotUpdate);
            }

            return result;
        }

        private VersionManifest LoadLocalManifest()
        {
            if (!File.Exists(LocalManifestPath)) return null;
            try
            {
                return JsonMapper.ToObject<VersionManifest>(File.ReadAllText(LocalManifestPath, Encoding.UTF8));
            }
            catch (Exception e)
            {
                SYLog.Warning($"[HotUpdate] 本地清单损坏，将全量重新下载: {e.Message}", LogCategory.HotUpdate);
                return null;
            }
        }
    }
}
