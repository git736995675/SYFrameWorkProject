using UnityEngine;
namespace ZSYFramework.Core
{
/// <summary>
/// 流程管理器
/// </summary>
public class ProcessManager : MonoSingleton<ProcessManager>
{
private readonly MemoryPool<Process> _pool = new();

/// <summary>
/// 获取一个流程
/// </summary>
/// <returns></returns>
public Process GetProcess()
{
    var process = _pool.Alloc();
    if (!process)
    {
        process = new GameObject("Process").AddComponent<Process>();
    }

    process.SetParent(transform);
    process.gameObject.SetActive(true);
    return process;
}

/// <summary>
/// 回收步骤
/// </summary>
/// <param name="process"></param>
public void Recycle(Process process)
{
    process.gameObject.SetActive(false);
    _pool.Free(process);
}
}
}
