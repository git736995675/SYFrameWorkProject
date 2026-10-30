using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZSYFramework.Core.Json;

namespace ZSYFramework.Core
{
/// <summary>
/// 动画管理器。集中管理动画播放与事件回调注册。
/// </summary>
public class AnimationManager : MonoSingleton<AnimationManager>
{
private readonly Dictionary<Animation, Action> _animations = new();

private readonly List<Animation> _animationsToRemove = new();

public void AddAnimation(Animation animation, Action callback)
{
    _animations.TryAdd(animation, callback);
}

// Update is called once per frame
private void Update()
{
    if (_animations.Count <= 0) return;
    foreach (var pair in _animations)
    {
        if (pair.Key.isPlaying) continue;
        pair.Value?.Invoke();
        _animationsToRemove.Add(pair.Key);
    }

    if (_animationsToRemove.Count <= 0) return;
    foreach (var pair in _animationsToRemove)
    {
        _animations.Remove(pair);
    }
    _animationsToRemove.Clear();
}
}
}
