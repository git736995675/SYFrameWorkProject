using System.Collections;

namespace ZSYFramework.Core
{
/// <summary>
/// 协程扩展管理器。提供不依赖场景对象的全局协程调度能力。
/// </summary>
public class CoroutineExtensions : MonoSingleton<CoroutineExtensions>
{
public void StartC(IEnumerator ie)
{
    StartCoroutine(ie);
}
}
}
