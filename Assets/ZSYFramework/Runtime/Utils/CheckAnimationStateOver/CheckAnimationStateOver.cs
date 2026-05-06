using System;
using UniRx;
using UnityEngine;

namespace SY
{
    public class CheckAnimationStateOver
    {
        public static void Action(Animator ani, Action ac)
        {
            var infos = ani.GetCurrentAnimatorClipInfo(0);
            var info = infos[0];
            var stateInfo = ani.GetCurrentAnimatorStateInfo(0);
            var time = info.clip.length / stateInfo.speed;
            Observable.Timer(TimeSpan.FromSeconds(time)).Subscribe(_ => { ac(); });
        }
    }
}