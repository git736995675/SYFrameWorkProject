using ZSYFramework.Core;

namespace ZSYFramework.Audio
{
    /// <summary>
    /// 音频反馈服务注入器：把 Audio 门面适配为 Core 的 <see cref="IAudioFeedback"/> 契约，
    /// 注册进 <see cref="ServiceHub"/>。Play 模式启动时自动安装；
    /// 未安装时 UI 调用 <see cref="ServiceHub.AudioFeedback"/> 得到 null，音效静默跳过。
    /// </summary>
    public static class AudioFeedbackInstaller
    {
        private static bool _installed;

        // 进 Play 模式前自动注册，消费方（UI 等）无需关心安装时机
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void AutoInstall()
        {
            Install();
        }

        /// <summary>把音频门面注册为全局 UI 反馈服务。重复调用幂等。</summary>
        public static void Install()
        {
            if (_installed) return;
            _installed = true;
            ServiceHub.Register(new AudioFeedback());
        }

        private sealed class AudioFeedback : IAudioFeedback
        {
            public void PlaySfx(string soundName)
            {
                SYAudio.PlaySfx(soundName);
            }
        }
    }
}
