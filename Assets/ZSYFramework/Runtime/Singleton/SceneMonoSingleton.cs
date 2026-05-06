using UnityEngine;

namespace SY
{
    // 场景单例基类 (随场景销毁)
    public abstract class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = FindObjectOfType<T>(true);

                if (_instance == null)
                {
                    CreateInstance();
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"Multiple {typeof(T).Name} detected. Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        private static void CreateInstance()
        {
            var go = new GameObject($"[Scene] {typeof(T).Name}");
            _instance = go.AddComponent<T>();
            _instance.OnInstanceCreated();
        }

        protected virtual void OnInstanceCreated()
        {
            // 动态创建时的初始化
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public static void Release()
        {
            if (_instance == null) return;
            var go = _instance.gameObject;
            _instance = null;
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }
    }
}