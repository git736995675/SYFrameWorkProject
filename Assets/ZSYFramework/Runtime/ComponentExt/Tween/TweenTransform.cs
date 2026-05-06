using System;
using DG.Tweening;
using SY;
using UnityEngine;

namespace ZSYFramework.Runtime.ComponentExt.Tween
{
    /// <summary>
    /// 动画之变化类
    /// </summary>
    public class TweenTransform : UiTween
    {
        private int finishedFlag = 0;

        /// <summary>
        /// 起始变化
        /// </summary>
        public Transform from;

        /// <summary>
        /// 结束变化
        /// </summary>
        public Transform to;
        private Transform cacheTransform;

        /// <summary>
        /// 缓存transform
        /// </summary>
        private Transform CacheTransform
        {
            get
            {
                if( cacheTransform == null )
                {
                    cacheTransform = transform;
                }

                return cacheTransform;
            }
        }

        private GameObject cacheGameObject;

        /// <summary>
        /// 缓存gameObject
        /// </summary>
        private GameObject CacheGameObject
        {
            get
            {
                if( cacheGameObject == null )
                {
                    cacheGameObject = gameObject;
                }

                return cacheGameObject;
            }
        }

        /// <summary>
        /// 本物体的位移坐标
        /// </summary>
        private Vector3 Position
        {
            get
            {
                return CacheTransform.localPosition;
            }
        }

        /// <summary>
        /// 本物体的旋转坐标
        /// </summary>
        private Vector3 Rotation
        {
            get
            {
                return CacheTransform.localEulerAngles;
            }
        }

        /// <summary>
        /// 本物体的缩放坐标
        /// </summary>
        private Vector3 Scale
        {
            get
            {
                return CacheTransform.localScale;
            }
        }

        /// <summary>
        /// 顺序播放动画
        /// </summary>
        public override void PlayForward()
        {
            if( ErrorTip() )
            {
                base.PlayForward();
            }
        }

        /// <summary>
        /// 顺序播放动画 延迟
        /// </summary>
        public override void PlayForwardDelay()
        {
            CacheGameObject.SetActive(true);
            Play(from, to);
        }

        /// <summary>
        /// 倒序播放动画
        /// </summary>
        public override void PlayReverse()
        {
            if( ErrorTip() )
            {
                base.PlayReverse();
            }
        }

        /// <summary>
        /// 倒序播放动画 延迟
        /// </summary>
        public override void PlayReverseDelay()
        {
            CacheGameObject.SetActive(true);
            Play(to, from);
        }

        /// <summary>
        /// 错误提示
        /// </summary>
        private bool ErrorTip()
        {
            if( !from || !to )
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 播放动画
        /// </summary>
        private void Play(Transform from, Transform to)
        {
            switch( style )
            {
                case TweenStyle.Once:
                    Once(from, to);
                    break;
                case TweenStyle.Loop:
                    Loop(from, to);
                    break;
                case TweenStyle.Repeatedly:
                    Repeatedly(from, to);
                    break;
                case TweenStyle.PingPong:
                    PingPong(from, to);
                    break;
            }
        }

        /// <summary>
        /// 一次
        /// </summary>
        private void Once(Transform from, Transform to)
        {
            CacheTransform.localPosition = from.localPosition;
            CacheTransform.localEulerAngles = from.localEulerAngles;
            CacheTransform.localScale = from.localScale;
            CacheTransform.DOLocalMove(to.localPosition, duration).SetEase(ease).OnComplete(() => OnFinished(() => onFinished()));
            CacheTransform.DOLocalRotate(to.localEulerAngles, duration, RotateMode.FastBeyond360).SetEase(ease).OnComplete(() => OnFinished(() => onFinished()));
            CacheTransform.DOScale(to.localScale, duration).SetEase(ease).OnComplete(() => OnFinished(() => onFinished()));
        }

        /// <summary>
        /// 循环
        /// </summary>
        private void Loop(Transform from, Transform to)
        {
            CacheTransform.localPosition = from.localPosition;
            CacheTransform.localEulerAngles = from.localEulerAngles;
            CacheTransform.localScale = from.localScale;
            CacheTransform.DOLocalMove(to.localPosition, duration).SetEase(ease).OnComplete(() => OnFinished(() => Loop(from, to)));
            CacheTransform.DOLocalRotate(to.localEulerAngles, duration, RotateMode.FastBeyond360).SetEase(ease).OnComplete(() => OnFinished(() => Loop(from, to)));
            CacheTransform.DOScale(to.localScale, duration).SetEase(ease).OnComplete(() => OnFinished(() => Loop(from, to)));
        }

        /// <summary>
        /// 一次来回
        /// </summary>
        private void Repeatedly(Transform from, Transform to)
        {
            CacheTransform.localPosition = from.localPosition;
            CacheTransform.localEulerAngles = from.localEulerAngles;
            CacheTransform.localScale = from.localScale;
            CacheTransform.DOLocalMove(to.localPosition, duration).SetEase(ease).OnComplete(() => CacheTransform.DOLocalMove(from.localPosition, duration));
            CacheTransform.DOLocalRotate(to.localEulerAngles, duration, RotateMode.FastBeyond360).SetEase(ease).OnComplete(() => CacheTransform.DOLocalRotate(from.localEulerAngles, duration).SetEase(ease));
            CacheTransform.DOScale(to.localScale, duration).SetEase(ease).OnComplete(() => CacheTransform.DOScale(from.localScale, duration));
        }

        /// <summary>
        /// 循环来回
        /// </summary>
        private void PingPong(Transform from, Transform to)
        {
            CacheTransform.DOLocalMove(to.localPosition, duration).SetEase(ease).OnComplete(() => OnFinished(() => PingPong(to, from)));
            CacheTransform.DOLocalRotate(to.localEulerAngles, duration, RotateMode.FastBeyond360).SetEase(ease).OnComplete(() => OnFinished(() => PingPong(to, from)));
            CacheTransform.DOScale(to.localScale, duration).SetEase(ease).OnComplete(() => OnFinished(() => PingPong(to, from)));
        }

        private void OnFinished(Action action)
        {
            finishedFlag++;
            if( finishedFlag == 3 )
            {
                action();
                finishedFlag = 0;
            }
        }

        /// <summary>
        /// 起始值
        /// </summary>
        protected override void StartValue()
        {
            from.LocalPosition(Position).LocalEulerAngles(Rotation).LocalScale(Scale);
        }

        /// <summary>
        /// 结束值
        /// </summary>
        protected override void EndValue()
        {
            to.LocalPosition(Position).LocalEulerAngles(Rotation).LocalScale(Scale);
        }
    }
}