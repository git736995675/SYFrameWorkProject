using UnityEngine;
namespace ZSYFramework.Core
{
/// <summary>
/// 状态机状态基类。子类实现进入/退出/更新逻辑，由 StateMachine 驱动切换。
/// </summary>
public abstract class BaseState : MonoBehaviour 
{
[SerializeField] private bool isInProgress = false;

   

public virtual void StateStart(string strArg1 = "", bool boolArg1 = false, int intArg1 = 0)
{
    isInProgress = true;
}

public virtual void StateUpdate()
{
}

public virtual void StateExit()
{
    isInProgress = false;
}

}
}
