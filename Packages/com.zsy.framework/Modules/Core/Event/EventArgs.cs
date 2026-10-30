namespace ZSYFramework.Core
{
/// <summary>
/// 带泛型参数的事件标识类
/// </summary>
public class EventArgs<T>
{
public static string EventName => typeof(T).GetHashCode().ToString();
}
}
