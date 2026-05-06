using System;
using UnityEngine;

namespace ZSYFramework.Runtime.ComponentExt
{
    public class PlayerPrefsExt
    {
        /// <summary>
        /// 判断键值对是否存在
        /// </summary>
        /// <returns><c>true</c>, if key was hased, <c>false</c> otherwise.</returns>
        /// <param name="key">Key.</param>
        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        /// <summary>
        /// 删除指定键值对
        /// </summary>
        /// <param name="key">Key.</param>
        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        /// <summary>
        /// 删除全部键值对
        /// </summary>
        public static void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }

        public static void SaveInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public static int GetInt(string key, int defValue)
        {
            return PlayerPrefs.GetInt(key, defValue);
        }

        public static void SaveFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        public static float GetFloat(string key, float defValue)
        {
            return PlayerPrefs.GetFloat(key, defValue);
        }

        public static void SaveString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public static string GetString(string key, string defValue = null)
        {
            return PlayerPrefs.GetString(key, defValue);
        }

        public static void SaveBoolean(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }

        public static bool GetBoolean(string key, bool defValue)
        {
            var temp = PlayerPrefs.GetInt(key, defValue ? 1 : 0) == 1;
            return temp;
        }

        public static void SaveLong(string key, long value)
        {
            PlayerPrefs.SetString(key, Convert.ToString(value));
        }

        public static long GetLong(string key, long defValue)
        {
            return Convert.ToInt64(PlayerPrefs.GetString(key, Convert.ToString(defValue)));
        }
    }
}