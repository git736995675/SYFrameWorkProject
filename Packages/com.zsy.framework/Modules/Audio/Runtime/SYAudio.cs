using UnityEngine;

namespace ZSYFramework.Audio;
/// <summary>
/// 音频静态门面。提供全局播放音效/背景音乐的快捷方法。
/// </summary>
public class SYAudio : MonoBehaviour
{
   /// <summary>
/// 设置背景音量  
/// </summary>
public static float BgmVolume
{
    get => AudioManager.Instance.Settings.BGMVolume;
    set => AudioManager.Instance.SetVolume(SoundType.BGM, value);
}

/// <summary>
/// 设置特效音量
/// </summary>
public static float SfxVolume
{
    get => AudioManager.Instance.Settings.SfxVolume;
    set => AudioManager.Instance.SetVolume(SoundType.SFX, value);
}

/// <summary>
/// 静音状态获取
/// </summary>
public static bool MuteState
{
    get => AudioManager.Instance.Settings.GlobalMuteStatus;
    set => AudioManager.Instance.SetMute(value);
}

/// <summary>
/// 背景静音状态获取
/// </summary>
public static bool BgmMuteState
{
    get => AudioManager.Instance.Settings.BGMMuteStatus;
    set => AudioManager.Instance.SetBgmMute(value);
}

/// <summary>
/// 音效静音状态获取
/// </summary>
public static bool SfxMuteState
{
    get => AudioManager.Instance.Settings.SfxMuteStatus;
    set => AudioManager.Instance.SetSfxMute(value);
}

/// <summary>
/// 是否需要存储
/// </summary>
public static bool IsNeedSave
{
    get => AudioManager.Instance.IsNeedSave;
    set => AudioManager.Instance.IsNeedSave = value;
}

/// <summary>
/// 播放背景乐
/// </summary>
public static void PlayBGM(string soundName, bool loop = true, float volume = 1)
{
    AudioManager.Instance.PlayBGM( soundName, loop, volume);
}

/// <summary>
/// 播放特效音
/// </summary>
public static void PlaySfx(string soundName, bool loop = false, float volume = 1)
{
    AudioManager.Instance.PlaySfx(SoundType.SFX, soundName, false, loop, volume);
}

/// <summary>
/// 播放Clip
/// </summary>
public static void PlayClip(AudioClip clip, bool loop = false, float volume = 1)
{
    AudioManager.Instance.PlayClip(SoundType.SFX, clip, false, loop, volume);
}

/// <summary>
/// 播放短音效
/// </summary>
public static void PlayOneShot(string soundName, float volume = 1)
{
    AudioManager.Instance.PlaySfx(SoundType.SFX, soundName, true, false, volume);
}

/// <summary>
/// 停止所有背景音
/// </summary>
public static void StopBGM()
{
    AudioManager.Instance.Stop(SoundType.BGM);
}

/// <summary>
/// 暂停背景音
/// </summary>
public static void PauseBGM()
{
    AudioManager.Instance.Pause();
}

/// <summary>
/// 解除暂停
/// </summary>
public static void UnPauseBGM()
{
    AudioManager.Instance.UnPause();
}

/// <summary>
/// 停止所有特效音
/// </summary>
public static void StopSfx()
{
    AudioManager.Instance.Stop(SoundType.SFX);
}

/// <summary>
/// 根据音效名称，停止指定音效
/// </summary>
public static void StopSourceByName(string soundName)
{
    AudioManager.Instance.Stop(soundName);
}

/// <summary>
/// 音效是否播放中
/// </summary>
public static bool IsPlaying(string soundName)
{
    return AudioManager.Instance.IsPlaying(soundName);
}
}
