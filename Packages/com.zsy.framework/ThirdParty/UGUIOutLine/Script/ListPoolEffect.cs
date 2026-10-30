/*
build by AppsTools.net
*/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AppsTools
{
    public class ObjectPool<T> where T : new()
    {
        private readonly Stack<T> sk = new Stack<T>();
        private readonly UnityAction<T> getAction;
        private readonly UnityAction<T> rAction;

        public int countAll { get; private set; }
        public int countActive { get { return countAll - countInactive; } }
        public int countInactive { get { return sk.Count; } }

        public ObjectPool(UnityAction<T> _getAction, UnityAction<T> _rAction)
        {
            getAction = _getAction;
            rAction = _rAction;
        }

        public T Get()
        {
            T e;
            if (sk.Count == 0)
            {
                e = new T();
                countAll++;
            }
            else
            {
                e = sk.Pop();
            }
            if (getAction != null)
                getAction(e);
            return e;
        }

        public void Release(T element)
        {
            if (sk.Count > 0 && ReferenceEquals(sk.Peek(), element))
            {
                return;
            }
            if (rAction != null)
                rAction(element);
            sk.Push(element);
        }
    }

    public static class ListPoolEffect<T>
    {
        private static readonly ObjectPool<List<T>> pl = new ObjectPool<List<T>>(null, l => l.Clear());

        public static List<T> Get()
        {
            return pl.Get();
        }

        public static void Release(List<T> r)
        {
            pl.Release(r);
        }
    }
}