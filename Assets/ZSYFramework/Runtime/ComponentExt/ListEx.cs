using System;
using System.Collections.Generic;
using System.Linq;

namespace ZSYFramework.Runtime.ComponentExt
{
    public static class ListEx
    {
        public static bool CustomContains<T>(this IList<T> list, T t) where T : class
        {
            //节省gc的开销
            var count = list.Count;
            for (var i = 0; i < count; i++)
            {
                if (list[i] == t)
                {
                    return true;
                }
            }
            return false;
        }

        public static List<T> CloneList<T>(this List<T> list) where T : new()
        {
            var items = new List<T>();
            for (var i = 0; i < list.Count; i++)
            {
                var model = new T();
                var ps = model.GetType().GetProperties();
                var properities = list[i].GetType().GetProperties();
                foreach (var t in properities)
                {
                    for (var k = 0; k < ps.Length; k++)
                    {
                        if (t.Name == ps[k].Name)
                        {
                            ps[k].SetValue(model, t.GetValue(list[i]));
                        }
                    }
                }

                items.Add(model);
            }

            return items;
        }

        /// <summary>
        /// 获取随机集合对象
        /// </summary>
        /// <param name="list"></param>
        /// <param name="Count"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> Random<T>(this IEnumerable<T> list, int Count = -1)
        {
            return list.OrderBy(x => Guid.NewGuid()).Take(Count == -1 ? list.Count() : Count);
        }
    }
}