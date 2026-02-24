using System;
using UnityEngine;

namespace SY
{
    public class ViewAnimation : MonoBehaviour
    {
        public virtual void PlayOpen(Transform trs, Action onComplete = null)
        {
        }

        public virtual void PlayHide(Transform trs, Action onComplete = null)
        {
        }
    }
}