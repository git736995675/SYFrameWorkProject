using UnityEngine;
namespace ZSYFramework.Core
{
/// <summary>
/// �������
/// </summary>
public class ProcessManager : MonoSingleton<ProcessManager>
{
private readonly MemoryPool<Process> _pool = new();

/// <summary>
/// ȡ��һ������
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
/// ���ղ���
/// </summary>
/// <param name="process"></param>
public void Recycle(Process process)
{
    process.gameObject.SetActive(false);
    _pool.Free(process);
}
}
}
