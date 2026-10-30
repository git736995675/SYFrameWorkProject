using System;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using UnityEngine;
namespace ZSYFramework.Core
{
/// <summary>
/// 对象池管理器。统一管理各类对象池的创建、取用与回收。
/// </summary>
public class PoolManager : Singleton<PoolManager>
{
/// <summary>
/// Gameobject池子
/// </summary>
private readonly Dictionary<string, MemoryPool<GameObject>> _objPool = new();

/// <summary>
/// 入池后放入这个下面
/// </summary>
private Transform _objParent;

public GameObject GetObj(string objName)
{
    if (!_objPool.TryGetValue(objName, out var pool)) return null;
    var go = pool.Alloc();
    if (!go) return go;
    go.gameObject.SetActive(true);
    go.name = objName;
    return go;
}

public GameObject GetObj(string objName, Transform template, Transform parent)
{
    var go = GetObj(objName);
    Transform ts = null;
    if (!go)
    {
        ts = Object.Instantiate(template, parent);
    }
    else
    {
        ts = go.transform;
        ts.SetParent(parent);
    }

    ts.gameObject.name = objName;
    return ts.gameObject;
}

public GameObject GetObj(string objName, Transform template, Transform parent, Vector3 pos, int scale)
{
    var go = GetObj(objName);
    Transform ts = null;
    if (!go)
    {
        ts = Object.Instantiate(template, parent);
        ts.gameObject.name = template.name;
    }
    else
    {
        ts = go.transform;
        ts.SetParent(parent);
    }

    ts.transform.position = pos;
    ts.LocalScale(scale);
    return ts.gameObject;
}

public void FreeObj(GameObject obj)
{
    if (obj)
        FreeObj(obj.name, obj);
}

public void FreeObj(string objName, GameObject obj)
{
    if (!obj) return;
    if (!_objParent)
    {
        var go = new GameObject("ObjPool");
        _objParent = go.transform;
    }

    obj.gameObject.SetActive(false);
    obj.transform.SetParent(_objParent);
    var dis = obj.GetComponent<IDisposable>();
    dis?.Dispose();
    if (!_objPool.TryGetValue(objName, out var pool))
    {
        pool = new MemoryPool<GameObject>(100);
        _objPool.Add(objName, pool);
    }

    pool.Free(obj);
}

public void FreeObj(Transform target)
{
    if (target != null)
    {
        FreeObj(target.name, target.gameObject);
    }
}
}
}
