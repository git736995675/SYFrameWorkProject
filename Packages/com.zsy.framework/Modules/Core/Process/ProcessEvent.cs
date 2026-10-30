using UnityEngine.Events;

namespace ZSYFramework.Core;
// 事件类
[System.Serializable]
/// <summary>
/// 流程事件定义，用于流程节点间的通知与数据传递。
/// </summary>
public class ProcessEvent : UnityEvent<string, float>
{
}
