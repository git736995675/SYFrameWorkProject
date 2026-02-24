namespace SY
{
    /// <summary>
    /// 功能描述：事件参数基类
    /// </summary>
    public class EventArgs<T>
    {
        public static string EventName => typeof(T).GetHashCode().ToString();
    }
}