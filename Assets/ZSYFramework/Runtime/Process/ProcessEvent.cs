using UnityEngine.Events;

namespace SY
{
    // 事件类
    [System.Serializable]
    public class ProcessEvent : UnityEvent<string, float>
    {
    }
}