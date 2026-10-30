namespace ZSYFramework.Core
{
// 普通单例基类 (非MonoBehaviour)
/// <summary>
/// 普通 C# 类泛型单例基类，非 MonoBehaviour，首次访问 Instance 时自动构造。
/// </summary>
public class Singleton<T> where T : class, new()
{
private static T _instance;
private static readonly object _lock = new object();

public static T Instance
{
    get
    {
        lock (_lock)
        {
            return _instance ??= new T();
        }
    }
}

// 销毁单例（可选）
public static void DestroyInstance()
{
    _instance = null;
}
}
}
