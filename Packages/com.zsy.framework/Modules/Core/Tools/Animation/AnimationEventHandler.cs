using System;
using UnityEngine;

namespace ZSYFramework.Core;
/// <summary>
/// 动画事件辅助工具。基于动画时长计算回调时机，用于动画播完通知。
/// </summary>
public static class AnimationEventHandler 
{
public static void PlayAnimation(this Animation animation,string clipName,Action callback)
{
    animation.Play(clipName);
    AnimationManager.Instance.AddAnimation(animation, callback);
}
}
