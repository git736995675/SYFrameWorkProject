using System;
using DG.Tweening;
using UnityEngine;
using ZSYFramework.Core.Json;

namespace ZSYFramework.UI
{
/// <summary>
/// 缩放视图动画实现。打开时放大、关闭时缩小的默认动画。
/// </summary>
public class ScaleViewAnimation : ViewAnimation
{
private float duration = 0.3f;

public override void PlayOpen(Transform trs, Action onComplete = null)
{
    trs.localScale = Vector3.zero;
    trs.transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack).OnComplete(() => 
    { 
        onComplete?.Invoke();
    });
}

public override void PlayHide(Transform trs, Action onComplete = null)
{
    trs.localScale = Vector3.one;
    trs.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack).onComplete = () =>
    {
        trs.gameObject.SetActive(false);
        onComplete?.Invoke();
    };
}

}
}
