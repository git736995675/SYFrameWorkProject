using System;
using DG.Tweening;
using UnityEngine;

namespace SY
{
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
