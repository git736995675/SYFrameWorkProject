using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using ZSYFramework.Core;
using ZSYFramework.Core.Json;

namespace ZSYFramework.Video
{
[RequireComponent(typeof(VideoPlayer))]
/// <summary>
/// 视频管理器。封装 VideoPlayer 的播放控制与事件回调。
/// </summary>
public class VideoManager : MonoSingleton<VideoManager>
{
[Header("Settings")] 
public bool autoPlayOnStart = true; // 启动自动播放
public bool autoAdjustAspect = false; // 自动调整比例

private VideoPlayer _videoPlayer;
private RawImage _videoDisplay; // 视频显示区域
private RenderTexture _renderTexture;

public Action PreparedCallback;//准备完成回调
public Action FinishedCallback;//播放完成回调(包括手动关闭)

public bool IsPlaying
{
    get
    {
        if (_videoDisplay == null || !_videoPlayer.isPrepared) return false;
        return _videoPlayer.isPlaying;
    }
} 

public bool IsPrepared => _videoPlayer != null && _videoPlayer.isPrepared;

public float Progress
{
    get
    {
        if (_videoPlayer == null || !_videoPlayer.isPrepared) return 0;
        return  (float)_videoPlayer.time / (float)_videoPlayer.length;
    }
}

public float GetVolume {
    get
    {
        if (_videoPlayer == null || !_videoPlayer.isPrepared) return 0;
        return _videoPlayer.GetDirectAudioVolume(0);
    }
}

// 准备并播放视频（支持本地/远程路径）
public void PrepareVideo(string videoPath, RawImage rawImage)
{
    if (_videoPlayer == null)
    {
        _videoPlayer = GetComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = false;
        _videoPlayer.loopPointReached += EndWithVideoPlay;
        _videoPlayer.prepareCompleted += OnVideoPrepared;
        
        // 创建RenderTexture
        _renderTexture = new RenderTexture(1920, 1080, 24);
        _videoPlayer.targetTexture = _renderTexture;
    }
    
    _videoDisplay = rawImage;
    _videoDisplay.texture = _renderTexture;
    
    if (!string.IsNullOrEmpty(videoPath))
    {
        _videoPlayer.url = videoPath.StartsWith("http") ? videoPath : GetPlatformPath(videoPath);
    }
    
    _videoPlayer.Prepare();
}

// 视频准备完成回调
private void OnVideoPrepared(VideoPlayer source)
{
    if (autoAdjustAspect) AdjustAspectRatio();
    if (autoPlayOnStart) _videoPlayer.Play();
    PreparedCallback?.Invoke();
}

private void EndWithVideoPlay(VideoPlayer source)
{
    FinishedCallback?.Invoke();
}

public void StopVideo()
{
    if(_renderTexture != null)
        _renderTexture.Release();
    if (_videoDisplay == null || !_videoPlayer.isPrepared) return;
    _videoPlayer.Stop();
    FinishedCallback?.Invoke();
}

public void TogglePlayPause()
{
    if (_videoDisplay == null || !_videoPlayer.isPrepared) return;

    if (_videoPlayer.isPlaying)
    {
        _videoPlayer.Pause();
    }
    else
    {
        _videoPlayer.Play();
    }
}

public void SetVolume(float volume)
{
    if (_videoDisplay == null || !_videoPlayer.isPrepared) return;
    _videoPlayer.SetDirectAudioVolume(0, volume);
}

public void OnProgressChanged(float value)
{
    if (_videoDisplay == null || !_videoPlayer.isPrepared) return;
    _videoPlayer.time = value * _videoPlayer.length;
}

// 调整视频显示比例
private void AdjustAspectRatio()
{
    if (_videoDisplay == null || !_videoPlayer.isPrepared) return;

    var videoRatio = (float)_videoPlayer.width / _videoPlayer.height;
    var screenRatio = (float)Screen.width / Screen.height;

    var rt = _videoDisplay.GetComponent<RectTransform>();
    rt.sizeDelta = videoRatio > screenRatio ? new Vector2(Screen.width, Screen.width / videoRatio) : new Vector2(Screen.height * videoRatio, Screen.height);
}

// 获取跨平台路径
private static string GetPlatformPath(string relativePath)
{
#if UNITY_ANDROID && !UNITY_EDITOR
return "file://" + Application.streamingAssetsPath + "/" + relativePath;
#else
    return Application.streamingAssetsPath + "/" + relativePath;
#endif
}
}
}
