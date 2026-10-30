using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace ZSYFramework.Core
{
/// <summary>
/// Slider 组件事件监听器。捕获滑块数值变化与拖拽事件。
/// </summary>
public class SliderEventListener : MonoBehaviour
{
public event Action OnDragStart;
public event Action OnDragEnd;

private Slider _slider;
private bool _isDragging = false;

void Start()
{
    _slider = GetComponent<Slider>();
}

private void OnPointerDown(PointerEventData eventData)
{
    _isDragging = true;
    OnDragStart?.Invoke();
}

private void OnPointerUp(PointerEventData eventData)
{
    if (!_isDragging) return;
    _isDragging = false;
    OnDragEnd?.Invoke();
}
}
}
