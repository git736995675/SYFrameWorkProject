using DG.Tweening;
using UnityEngine;
namespace ZSYFramework.Core
{
/// <summary>
///     动画之旋转类
/// </summary>
public class TweenRotation : UiTween
{
public bool isClockwise;

/// <summary>
///     起始坐标
/// </summary>
public Vector3 from;

/// <summary>
///     结束坐标
/// </summary>
public Vector3 to;

private GameObject cacheGameObject;
private Transform cacheTransform;

/// <summary>
///     缓存transform
/// </summary>
private Transform CacheTransform
{
    get
    {
        if(cacheTransform == null)
        {
            cacheTransform = transform;
        }

        return cacheTransform;
    }
}

/// <summary>
///     缓存gameObject
/// </summary>
private GameObject CacheGameObject
{
    get
    {
        if(cacheGameObject == null)
        {
            cacheGameObject = gameObject;
        }

        return cacheGameObject;
    }
}

/// <summary>
///     本物体的旋转坐标
/// </summary>
private Vector3 Rotation => CacheTransform.localEulerAngles;

/// <summary>
///     顺序播放动画
/// </summary>
public override void PlayForward()
{
    base.PlayForward();
}

/// <summary>
///     顺序播放动画 延迟
/// </summary>
public override void PlayForwardDelay()
{
    CacheGameObject.SetActive(true);
    Play(from, to);
}

/// <summary>
///     倒序播放动画
/// </summary>
public override void PlayReverse()
{
    base.PlayReverse();
}

/// <summary>
///     倒序播放动画 延迟
/// </summary>
public override void PlayReverseDelay()
{
    CacheGameObject.SetActive(true);
    Play(to, from);
}

/// <summary>
///     播放动画
/// </summary>
private void Play(Vector3 from, Vector3 to)
{
    var tempfrom = !isClockwise ? from : to;
    var tempto = !isClockwise ? to : from;
    switch (style)
    {
        case TweenStyle.Once:
            Once(tempfrom, tempto);
            break;
        case TweenStyle.Loop:
            Loop(tempfrom, tempto);
            break;
        case TweenStyle.Repeatedly:
            Repeatedly(tempfrom, tempto);
            break;
        case TweenStyle.PingPong:
            PingPong(tempfrom, tempto);
            break;
    }
}

/// <summary>
///     一次
/// </summary>
private void Once(Vector3 from, Vector3 to)
{
    CacheTransform.localEulerAngles = from;
    CacheTransform.DORotate(to, duration, RotateMode.FastBeyond360).SetEase(ease).OnComplete(() => onFinished());
}

/// <summary>
///     循环
/// </summary>
private void Loop(Vector3 from, Vector3 to)
{
    CacheTransform.localEulerAngles = from;
    CacheTransform.DORotate(to, duration, RotateMode.FastBeyond360).SetEase(ease).OnComplete(() => Loop(from, to));
}

/// <summary>
///     一次来回
/// </summary>
private void Repeatedly(Vector3 from, Vector3 to)
{
    CacheTransform.localEulerAngles = from;
    CacheTransform.DORotate(to, duration, RotateMode.FastBeyond360).SetEase(ease).OnComplete(() => CacheTransform.DOLocalRotate(from, duration).SetEase(ease));
}

/// <summary>
///     循环来回
/// </summary>
private void PingPong(Vector3 from, Vector3 to)
{
    CacheTransform.DORotate(to, duration, RotateMode.FastBeyond360).SetEase(ease).OnComplete(() => PingPong(to, from));
}

/// <summary>
///     起始值
/// </summary>
protected override void StartValue()
{
    from = Rotation;
}

/// <summary>
///     结束值
/// </summary>
protected override void EndValue()
{
    to = Rotation;
}
}
}
