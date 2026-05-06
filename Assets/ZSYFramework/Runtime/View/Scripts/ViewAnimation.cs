using System;
using UnityEngine;

namespace ZSYFramework.Runtime.View.Scripts
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