using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ZSYFramework.AssetBundle;
using ZSYFramework.Core;
using ZSYFramework.Core.Json;

namespace ZSYFramework.Audio;
/// <summary>
/// 音频类型枚举：背景音乐、音效等。
/// </summary>
public enum SoundType
{
BGM, //背景音
SFX, //音效
}

/// <summary>
/// 音频管理器。统一管理 BGM 与音效的播放、音量与生命周期。
/// </summary>
public class AudioManager : MonoSingleton<AudioManager>
{
//已加载音效
private Dictionary<string, AudioClip> _loadedClips;

//现存音效管理
private readonly List<AudioInfo> _soundList = new List<AudioInfo>();

//音效对象池
private readonly Queue<AudioInfo> _audioPool = new Queue<AudioInfo>();

private AudioInfo _bgmAudioSource;

private const int PoolSize = 3;

//音效数据是否需要本地保存
internal bool IsNeedSave = true;

// 当前设置
internal AudioSettings Settings;


protected override void Awake()
{
    LoadSettings();
    // 初始化对象池
    CreateSource(PoolSize);
}

// 创建Source入池
private void CreateSource(int size)
{
    for (var i = 0; i < size; i++)
    {
        var poolGo = new GameObject("AudioSource");
        poolGo.transform.SetParent(transform);
        var source = poolGo.AddComponent<AudioSource>();
        var info = new AudioInfo
        {
            Source = source,
        };
        _audioPool.Enqueue(info);
        poolGo.SetActive(false);
    }
}

// 加载音频资源（同步）
private void LoadClip(string clipName, Action<AudioClip> callback)
{
    _loadedClips ??= new Dictionary<string, AudioClip>();

    if (_loadedClips.TryGetValue(clipName, out var clip))
    {
        callback?.Invoke(clip);
        return;
    }

    ABLoadManager.Instance.LoadAudioClip(clipName, (clip) =>
    {
        if (clip != null)
        {
            _loadedClips[clipName] = clip;
            callback?.Invoke(clip);
        }
        else
            SYLog.Error($"AudioClip {clipName} not found!", LogCategory.Audio, this);
    });
}

internal void PlayBGM(string clipName, bool loop = true, float volume = 1)
{
    if (string.IsNullOrEmpty(clipName))
    {
        SYLog.Error($"Sound {clipName} not in config!", LogCategory.Audio, this);
        return;
    }

    LoadClip(clipName, (clip) =>
    {
        if (clip == null)
        {
            SYLog.Error($"{clipName} load null!", LogCategory.Audio, this);
            return;
        }

        _bgmAudioSource ??= new AudioInfo
        {
            SType = SoundType.BGM,
            Source = new GameObject("AudioSource").AddComponent<AudioSource>()
        };
        _bgmAudioSource.Source.transform.SetParent(transform);
        _bgmAudioSource.Source.gameObject.SetActive(true);
        _bgmAudioSource.Source.name = $"{_bgmAudioSource.Source.name}_{SoundType.BGM}_{Path.GetFileName(clipName)}";
        _bgmAudioSource.Source.volume = Settings.BGMMuteStatus ? 0 : volume * Settings.BGMVolume;
        _bgmAudioSource.Source.clip = clip;
        _bgmAudioSource.Source.loop = loop;
        _bgmAudioSource.Volume = volume;
        _bgmAudioSource.ClipName = clipName;
        _bgmAudioSource.Source.Play();
    });
}

// 播放音效（分类控制）
internal void PlaySfx(SoundType type, string clipName, bool isOneShot = false, bool loop = false, float volume = 1.0f)
{
    if (string.IsNullOrEmpty(clipName))
    {
        SYLog.Error($"Sound {clipName} not in config!", LogCategory.Audio, this);
        return;
    }

    LoadClip(clipName, (clip) =>
    {
        if (clip == null)
        {
            SYLog.Error($"{clipName} load null!", LogCategory.Audio, this);
            return;
        }

        if (_audioPool.Count == 0)
        {
            CreateSource(2);
        }

        var audioInfo = _audioPool.Dequeue();
        audioInfo.Source.gameObject.SetActive(true);
        audioInfo.Source.name = audioInfo.Source.name.Replace("(Clone)", $"_{type}_{Path.GetFileName(clipName)}");
        audioInfo.Source.volume = Settings.SfxMuteStatus ? 0 : volume * Settings.SfxVolume;
        audioInfo.SType = type;
        audioInfo.Volume = volume;
        audioInfo.ClipName = clipName;
        if (!_soundList.Contains(audioInfo))
            _soundList.Add(audioInfo);

        if (isOneShot)
        {
            audioInfo.Source.Stop();
            audioInfo.Source.PlayOneShot(clip);
        }
        else
        {
            audioInfo.Source.clip = clip;
            audioInfo.Source.loop = loop;
            audioInfo.Source.Play();
        }

        if (loop) return;
        var enumerator = ReturnToPoolAfterPlay(audioInfo, clip.length);
        audioInfo.Enumerator = enumerator;
        StartCoroutine(enumerator);
    });
}

// 播放音效（分类控制）
internal void PlayClip(SoundType type, AudioClip clip, bool isOneShot = false, bool loop = false,
    float volume = 1.0f)
{
    if (clip == null)
    {
        SYLog.Error($"clip is null!", LogCategory.Audio, this);
        return;
    }

    if (_audioPool.Count == 0)
    {
        CreateSource(2);
    }

    var audioInfo = _audioPool.Dequeue();
    audioInfo.Source.gameObject.SetActive(true);
    audioInfo.Source.name = clip.name;
    audioInfo.Source.volume = Settings.SfxMuteStatus ? 0 : volume * Settings.SfxVolume;
    audioInfo.SType = type;
    audioInfo.Volume = volume;
    audioInfo.ClipName = clip.name;
    if (!_soundList.Contains(audioInfo))
        _soundList.Add(audioInfo);

    if (isOneShot)
    {
        audioInfo.Source.Stop();
        audioInfo.Source.PlayOneShot(clip);
    }
    else
    {
        audioInfo.Source.clip = clip;
        audioInfo.Source.loop = loop;
        audioInfo.Source.Play();
    }

    if (loop) return;
    var enumerator = ReturnToPoolAfterPlay(audioInfo, clip.length);
    audioInfo.Enumerator = enumerator;
    StartCoroutine(enumerator);
}

private IEnumerator ReturnToPoolAfterPlay(AudioInfo source, float duration)
{
    yield return new WaitForSeconds(duration);
    source.Source.Stop();
    source.Source.gameObject.SetActive(false);
    _audioPool.Enqueue(source);
}

// 停止指定类型音效
internal void Stop(SoundType type)
{
    if (type == SoundType.BGM)
    {
        _bgmAudioSource?.Source.Stop();
        return;
    }

    foreach (var info in _soundList.Where(info => type == info.SType))
    {
        if (!info.Source.gameObject.activeSelf) continue;
        info.Source.Stop();
        info.Source.gameObject.SetActive(false);
        if (info.Enumerator != null)
        {
            StopCoroutine(info.Enumerator);
        }

        _audioPool.Enqueue(info);
    }
}

internal bool IsPlaying(string soundName)
{
    if (_bgmAudioSource != null && _bgmAudioSource.ClipName == soundName)
    {
        return _bgmAudioSource.Source.isPlaying;
    }

    foreach (var info in _soundList.Where(info => soundName == info.ClipName))
    {
        return info.Source.gameObject.activeSelf && info.Source.isPlaying;
    }

    return false;
}

// 停止指定名称音效
internal void Stop(string soundName)
{
    if (_bgmAudioSource != null && _bgmAudioSource.ClipName == soundName)
    {
        _bgmAudioSource?.Source.Stop();
        return;
    }

    foreach (var info in _soundList.Where(info => soundName == info.ClipName))
    {
        if (!info.Source.gameObject.activeSelf) continue;
        info.Source.Stop();
        info.Source.gameObject.SetActive(false);
        if (info.Enumerator != null)
        {
            StopCoroutine(info.Enumerator);
        }

        _audioPool.Enqueue(info);
    }
}

// 暂停背景音
internal void Pause()
{
    _bgmAudioSource?.Source.Pause();
}

//恢复暂停
internal void UnPause()
{
    _bgmAudioSource?.Source.UnPause();
}

// 设置全局音量
internal void SetVolume(SoundType type, float volume)
{
    if (type == SoundType.BGM)
    {
        if (_bgmAudioSource != null)
            _bgmAudioSource.Source.volume = volume * _bgmAudioSource.Volume;
        
        Settings.BGMVolume = volume;
        Settings.BGMMuteStatus = volume == 0;
    }
    else
    {
        foreach (var source in _soundList.Where(info => type == info.SType))
        {
            source.Source.volume = volume * source.Volume;
        }

        Settings.SfxVolume = volume;
        Settings.SfxMuteStatus = volume == 0;
    }

    SaveSettings(Settings);
}

//设置静音
internal void SetBgmMute(bool mute)
{
    Settings.BGMMuteStatus = mute;
    if (_bgmAudioSource != null)
        _bgmAudioSource.Source.volume = mute ? 0 : _bgmAudioSource.Volume * _bgmAudioSource.Volume;
    SaveSettings(Settings);
}

//设置静音
internal void SetSfxMute(bool mute)
{
    Settings.SfxMuteStatus = mute;
    foreach (var source in _soundList)
    {
        source.Source.volume = mute ? 0 : source.Volume * source.Volume;
    }

    SaveSettings(Settings);
}

//设置静音
internal void SetMute(bool mute)
{
    Settings.GlobalMuteStatus = mute;
    AudioListener.pause = mute;
    SaveSettings(Settings);
}


//音效数据存本地
private void SaveSettings(AudioSettings settings)
{
    if (!IsNeedSave) return;
    var json = JsonUtility.ToJson(settings);
    PlayerPrefs.SetString("SoundSettings", json);
}

//加载音效数据
private void LoadSettings()
{
    if (!PlayerPrefs.HasKey("SoundSettings"))
    {
        Settings = new AudioSettings();
        SaveSettings(Settings);
    }
    else
    {
        var json = PlayerPrefs.GetString("SoundSettings");
        Settings = JsonUtility.FromJson<AudioSettings>(json);
    }

    Settings ??= new AudioSettings();
    AudioListener.pause = Settings.GlobalMuteStatus;
}

public void FadeBGM(AudioSource source, float targetVolume, float duration)
{
    StartCoroutine(FadeCoroutine(source, targetVolume, duration));
}

private IEnumerator FadeCoroutine(AudioSource source, float targetVolume, float duration)
{
    var startVolume = source.volume;
    float timer = 0;

    while (timer < duration)
    {
        source.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
        timer += Time.deltaTime;
        yield return null;
    }

    source.volume = targetVolume;
}
}

/// <summary>
/// 音频配置资产。存储音量等可持久化设置。
/// </summary>
internal class AudioSettings
{
public float BGMVolume = 1.0f;
public float SfxVolume = 1.0f;

public bool BGMMuteStatus = false;
public bool SfxMuteStatus = false;

public bool GlobalMuteStatus = false;
}

/// <summary>
/// 音频资源描述信息。
/// </summary>
internal class AudioInfo
{
public IEnumerator Enumerator;
public string ClipName;
public float Volume;
public SoundType SType;
public AudioSource Source;
}
