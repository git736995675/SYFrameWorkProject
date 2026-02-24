
namespace SY
{
    // 普通单例基类 (非MonoBehaviour)
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
