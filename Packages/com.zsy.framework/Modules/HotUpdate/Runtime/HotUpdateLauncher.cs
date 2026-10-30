using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using ZSYFramework.Core;

namespace ZSYFramework.HotUpdate
{
    /// <summary>
    /// HybridCLR 启动器：负责补充 AOT 元数据与加载热更程序集。
    /// 安装 HybridCLR 包（com.code-philosophy.hybridclr）后本模块自动启用
    /// （asmdef versionDefines 激活 HYBRIDCLR 宏）；未安装时全部方法为安全空操作。
    /// </summary>
    public static class HotUpdateLauncher
    {
        // 已加载的热更程序集（按配置顺序）
        private static readonly List<Assembly> _loaded = new();

        /// <summary>已加载的热更程序集列表。</summary>
        public static IReadOnlyList<Assembly> LoadedAssemblies => _loaded;

        /// <summary>
        /// 热更启动全流程：资源版本更新 → AOT 元数据补充 → 热更程序集加载。
        /// 版本更新网络失败不会中断流程（回落到包内置资源），可直接在启动场景最先调用。
        /// </summary>
        /// <param name="settings">热更配置，传 null 从 Resources 加载。</param>
        /// <param name="progress">版本下载整体进度（0~1）。</param>
        /// <param name="ct">取消令牌。</param>
        public static async Task<VersionUpdateResult> RunStartupFlowAsync(HotUpdateSettings settings = null,
            IProgress<float> progress = null, CancellationToken ct = default)
        {
            var updater = new VersionUpdater(settings);
            var result = await updater.UpdateAsync(progress, ct);
            if (!result.Success)
            {
                SYLog.Warning($"[HotUpdate] 版本更新未成功({result.Error})，使用包内置资源继续启动", LogCategory.HotUpdate);
                return result;
            }

            await LoadMetadataForAOTAssemblyAsync(settings);
            await LoadHotUpdateAssembliesAsync(settings);
            return result;
        }

        /// <summary>
        /// 补充 AOT 元数据。热更代码中实例化 AOT 泛型（如以 AOT 类型为参数的 List/Dictionary）前必须执行。
        /// </summary>
        public static async Task LoadMetadataForAOTAssemblyAsync(HotUpdateSettings settings)
        {
            settings ??= HotUpdateSettings.Load();
#if HYBRIDCLR
            foreach (var name in settings.aotMetaAssemblies)
            {
                var bytes = await ReadDllBytesAsync("AOTMeta", name);
                var code = global::HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(
                    bytes, global::HybridCLR.HomologousImageMode.SuperSet);
                SYLog.Info($"[HotUpdate] AOT 元数据补充完成: {name} (code={code})", LogCategory.HotUpdate);
            }
#else
            await Task.CompletedTask;
            if (settings.aotMetaAssemblies.Count > 0)
            {
                SYLog.Warning("[HotUpdate] 未安装 HybridCLR 包，跳过 AOT 元数据补充", LogCategory.HotUpdate);
            }
#endif
        }

        /// <summary>按配置顺序加载热更程序集并返回已加载列表。</summary>
        public static async Task<IReadOnlyList<Assembly>> LoadHotUpdateAssembliesAsync(HotUpdateSettings settings)
        {
            settings ??= HotUpdateSettings.Load();
            _loaded.Clear();
#if HYBRIDCLR
            foreach (var name in settings.hotUpdateAssemblies)
            {
                var bytes = await ReadDllBytesAsync("HotUpdateDlls", name);
                var assembly = Assembly.Load(bytes);
                _loaded.Add(assembly);
                SYLog.Info($"[HotUpdate] 热更程序集已加载: {name}", LogCategory.HotUpdate);
            }
#else
            await Task.CompletedTask;
            if (settings.hotUpdateAssemblies.Count > 0)
            {
                SYLog.Warning("[HotUpdate] 未安装 HybridCLR 包，跳过热更程序集加载", LogCategory.HotUpdate);
            }
#endif
            return _loaded;
        }

        // HybridCLR 打包后会把热更 dll 与 AOT 补充元数据放在 StreamingAssets/HybridCLRData 下
        private static async Task<byte[]> ReadDllBytesAsync(string subDir, string assemblyName)
        {
            var uri = $"{Application.streamingAssetsPath}/HybridCLRData/{subDir}/{assemblyName}.dll";
            if (!uri.Contains("://")) uri = "file://" + uri;

            using var request = UnityWebRequest.Get(uri);
            request.timeout = 30;
            var op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new FileNotFoundException($"[HotUpdate] 读取程序集失败: {assemblyName}.dll ({request.error})");
            }

            return request.downloadHandler.data;
        }
    }
}
