using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using ZSYFramework.Core;

namespace ZSYFramework.Download
{
    /// <summary>
    /// 通用文件下载器。基于 UnityWebRequest 的纯异步实现（不依赖 MonoBehaviour），
    /// 支持断点续传（Range + 临时文件追加）、失败重试、MD5/大小校验与并发批量下载。
    /// </summary>
    public static class Downloader
    {
        /// <summary>下载中的临时文件后缀，完成后重命名为目标文件。</summary>
        private const string TempSuffix = ".downloading";

        /// <summary>
        /// 下载单个文件。本地已有同名目标文件时直接视为完成（由调用方用 MD5 决定是否覆盖）。
        /// 失败按 <see cref="DownloadOptions.MaxRetries"/> 自动重试。
        /// </summary>
        /// <param name="url">文件地址。</param>
        /// <param name="savePath">保存到的本地完整路径。</param>
        /// <param name="options">下载配置，传 null 用默认值。</param>
        /// <param name="progress">单文件进度（0~1）。</param>
        /// <param name="ct">取消令牌。</param>
        public static async Task DownloadFileAsync(string url, string savePath,
            DownloadOptions options = null, IProgress<float> progress = null, CancellationToken ct = default)
        {
            options ??= new DownloadOptions();

            var dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var tempPath = savePath + TempSuffix;
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    await DownloadOnce(url, savePath, tempPath, options, progress, ct);
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception) when (attempt < options.MaxRetries)
                {
                    SYLog.Warning($"[Download] 第 {attempt + 1} 次下载失败，准备重试: {url}", LogCategory.Network);
                    await Task.Delay(TimeSpan.FromSeconds(options.RetryDelaySeconds * (attempt + 1)), ct);
                }
            }
        }

        /// <summary>
        /// 并发批量下载。任一文件最终失败（重试耗尽）则整体抛出异常，已成功的文件保留在磁盘上。
        /// </summary>
        /// <param name="tasks">文件任务列表。</param>
        /// <param name="maxConcurrency">最大并发数。</param>
        /// <param name="progress">整体进度（0~1，按完成文件数计）。</param>
        /// <param name="ct">取消令牌。</param>
        public static async Task DownloadFilesAsync(IReadOnlyList<DownloadTask> tasks, int maxConcurrency,
            IProgress<float> progress = null, CancellationToken ct = default)
        {
            if (tasks == null || tasks.Count == 0) return;
            maxConcurrency = Mathf.Max(1, maxConcurrency);

            var completed = 0;
            var report = new SynchronizationContextProgress(progress);
            using var semaphore = new SemaphoreSlim(maxConcurrency);
            var exceptions = new List<Exception>();

            await Task.WhenAll(tasks.Select(task => RunOneAsync(task, semaphore, report, ct, () =>
            {
                Interlocked.Increment(ref completed);
                report.Report((float)completed / tasks.Count);
            }, exceptions)));

            if (exceptions.Count > 0)
            {
                throw new AggregateException($"[Download] 批量下载有 {exceptions.Count} 个文件失败", exceptions);
            }
        }

        /// <summary>拉取远端文本（版本清单等小文件）。失败时抛出异常。</summary>
        /// <param name="url">文本地址。</param>
        /// <param name="timeoutSeconds">超时秒数。</param>
        /// <param name="ct">取消令牌。</param>
        public static async Task<string> GetTextAsync(string url, int timeoutSeconds = 30,
            CancellationToken ct = default)
        {
            using var request = UnityWebRequest.Get(url);
            request.timeout = timeoutSeconds;
            var op = request.SendWebRequest();
            while (!op.isDone)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException($"[Download] 文本请求失败: {url} ({request.error})");
            }

            // 去除可能的 BOM
            var data = request.downloadHandler.data;
            if (data != null && data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                return System.Text.Encoding.UTF8.GetString(data, 3, data.Length - 3);
            }

            return request.downloadHandler.text;
        }

        private static async Task RunOneAsync(DownloadTask task, SemaphoreSlim semaphore,
            IProgress<float> overallProgress, CancellationToken ct, Action onCompleted,
            ICollection<Exception> exceptions)
        {
            try
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    var options = new DownloadOptions { ExpectedMd5 = task.Md5, ExpectedSize = task.Size };
                    await DownloadFileAsync(task.Url, task.SavePath, options, null, ct);
                    onCompleted();
                }
                finally
                {
                    semaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                lock (exceptions)
                {
                    exceptions.Add(e);
                }
                SYLog.Error($"[Download] 文件下载失败: {task.Url} ({e.Message})", LogCategory.Network);
            }
        }

        private static async Task DownloadOnce(string url, string savePath, string tempPath,
            DownloadOptions options, IProgress<float> progress, CancellationToken ct)
        {
            var resumeFrom = File.Exists(tempPath) ? new FileInfo(tempPath).Length : 0;

            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            request.timeout = options.TimeoutSeconds;

            var append = resumeFrom > 0;
            if (append) request.SetRequestHeader("Range", $"bytes={resumeFrom}-");

            var handler = new DownloadHandlerFile(tempPath, append) { removeFileOnAbort = true };
            request.downloadHandler = handler;

            var op = request.SendWebRequest();
            while (!op.isDone)
            {
                progress?.Report(op.progress);
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            progress?.Report(1f);

            // 服务器不支持 Range 时会返回 200（全量）或 416，此时丢弃临时文件重新下载
            if (append && request.responseCode != 206)
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
                throw new IOException($"[Download] 服务器不支持断点续传(响应码 {request.responseCode})，已重置下载: {url}");
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new IOException($"[Download] 下载失败: {url} ({request.error}, 响应码 {request.responseCode})");
            }

            VerifyAndPublish(tempPath, savePath, options, url);
        }

        private static void VerifyAndPublish(string tempPath, string savePath, DownloadOptions options, string url)
        {
            if (options.ExpectedSize > 0)
            {
                var size = new FileInfo(tempPath).Length;
                if (size != options.ExpectedSize)
                {
                    throw new IOException($"[Download] 文件大小校验失败: {url} (期望 {options.ExpectedSize}, 实际 {size})");
                }
            }

            if (!string.IsNullOrEmpty(options.ExpectedMd5))
            {
                var md5 = ComputeMd5(tempPath);
                if (!string.Equals(md5, options.ExpectedMd5, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException($"[Download] MD5 校验失败: {url} (期望 {options.ExpectedMd5}, 实际 {md5})");
                }
            }

            if (File.Exists(savePath)) File.Delete(savePath);
            File.Move(tempPath, savePath);
        }

        private static string ComputeMd5(string path)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(path);
            var hash = md5.ComputeHash(stream);
            var sb = new System.Text.StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        /// <summary>线程安全的进度转发器：把工作线程上的进度回调封送回 Unity 主线程。</summary>
        private sealed class SynchronizationContextProgress : IProgress<float>
        {
            private readonly IProgress<float> _inner;
            private readonly SynchronizationContext _context;

            public SynchronizationContextProgress(IProgress<float> inner)
            {
                _inner = inner;
                _context = SynchronizationContext.Current;
            }

            public void Report(float value)
            {
                if (_inner == null) return;
                if (_context != null) _context.Post(_ => _inner.Report(value), null);
                else _inner.Report(value);
            }
        }
    }
}
