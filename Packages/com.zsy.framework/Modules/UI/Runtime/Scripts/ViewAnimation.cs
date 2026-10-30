using System;
using UnityEngine;

namespace ZSYFramework.UI
{
/// <summary>
/// 视图动画抽象基类。定义视图打开/关闭动画接口。
/// </summary>
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
