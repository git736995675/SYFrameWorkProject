using System;
using UnityEngine;

namespace SY
{
    public abstract class ScriptableSingleton<T> : ScriptableObject where T : ScriptableSingleton<T>
    {
        public static T Instance { get; private set; }

        public static void Init(Action callback)
        {
            if (Instance)
            {
                callback();
                return;
            }

            ABLoadManager.Instance.Load<T>(typeof(T).ToString(), (setting) =>
            {
                Instance = setting;
                callback();
            });
        }
    }
}