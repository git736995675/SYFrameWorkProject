using UnityEngine.Events;

namespace SY
{
    // 事件定义
    [System.Serializable]
    public class ProcessEvent : UnityEvent<string, float>
    {
    }
}