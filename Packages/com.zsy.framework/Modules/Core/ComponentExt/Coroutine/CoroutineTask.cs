using System.Collections;
using ZSYFramework.Core.Json;

namespace ZSYFramework.Core;
/// <summary>
/// 协程任务封装。将协程抽象为可跟踪状态的任务对象。
/// </summary>
public class CoroutineTask
{
public bool IsRunning { get; private set; }

private readonly IEnumerator _routine;

public CoroutineTask(IEnumerator routine)
{
    _routine = routine;
}

public IEnumerator Start()
{
    IsRunning = true;
    yield return _routine;
    IsRunning = false;
}
}
