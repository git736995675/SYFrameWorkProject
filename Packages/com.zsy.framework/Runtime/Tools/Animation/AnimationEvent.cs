using System;
using UnityEngine;

namespace SY
{
    public static class AnimationExt 
    {
        public static void PlayAnimation(this Animation animation,string clipName,Action callback)
        {
            animation.Play(clipName);
            AnimationManager.Instance.AddAnimation(animation, callback);
        }
    }
}