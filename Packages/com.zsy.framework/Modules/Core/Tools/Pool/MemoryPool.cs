using ZSYFramework.Core.Json;
using System.Collections.Generic;

namespace ZSYFramework.Core
{
/// <summary>
/// 对象池
/// </summary>
public class MemoryPool<T> where T : class
{
/**
* @description 切勿单独操作此集合，如需增删请引用封装的函数
* @description 此数组在此用作模拟队列来处理数据，遵循先进先出，所以请勿单独操作此数组
*/
public int Count
{
    get { return mobjs.Count; }
}

private Queue<T> mobjs = new Queue<T>();
private int mMaxSize = 10;

/// <summary>
/// 构造
/// </summary>
/// <param name="maxSize">最大数量</param>
public MemoryPool(int maxSize = 10)
{
    mMaxSize = maxSize;
}

/// <summary>
/// 出列
/// </summary>
public T Alloc()
{
    T t = null;
    if (mobjs.Count > 0)
    {
        t = mobjs.Dequeue();
    }

    return t;
}

/// <summary>
/// 入队
/// </summary>
public void Free(T t)
{
    if (!mobjs.Contains(t))
    {
        if (mobjs.Count < mMaxSize)
        {
            mobjs.Enqueue(t);
        }
    }
}

/// <summary>
/// 部署
/// </summary>
public void Dispose()
{
    mobjs.Clear();
}
}
}
