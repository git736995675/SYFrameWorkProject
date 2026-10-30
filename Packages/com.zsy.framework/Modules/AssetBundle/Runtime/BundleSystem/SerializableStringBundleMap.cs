using System;
using System.Collections.Generic;

namespace ZSYFramework.AssetBundle
{
    /// <summary>
    /// string→string 字典的 Unity 可序列化包装。JsonUtility 不能直接序列化 Dictionary，
    /// 清单里的 resources 映射以平行数组形式存储。
    /// </summary>
    [Serializable]
    public class SerializableStringBundleMap
    {
        [Serializable]
        public class Entry
        {
            public string key;
            public string value;
        }

        /// <summary>键值平行数组（JsonUtility 序列化载体）。</summary>
        public List<Entry> entries = new();

        private Dictionary<string, string> _cache;

        private Dictionary<string, string> Map
        {
            get
            {
                if (_cache != null) return _cache;
                _cache = new Dictionary<string, string>(entries.Count, StringComparer.Ordinal);
                foreach (var e in entries)
                {
                    if (!string.IsNullOrEmpty(e?.key)) _cache[e.key] = e.value;
                }

                return _cache;
            }
        }

        /// <summary>取值。</summary>
        public bool TryGetValue(string key, out string value) => Map.TryGetValue(key, out value);

        /// <summary>写入（构建期由反射外的编辑器代码调用，写后缓存失效）。</summary>
        public void Set(string key, string value)
        {
            Map[key] = value;
            RebuildEntries();
        }

        private void RebuildEntries()
        {
            entries.Clear();
            foreach (var kv in Map)
            {
                entries.Add(new Entry { key = kv.Key, value = kv.Value });
            }
        }
    }
}
