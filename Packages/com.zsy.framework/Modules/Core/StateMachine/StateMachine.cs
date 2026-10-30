using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZSYFramework.Core.Json;

namespace ZSYFramework.Core
{
/// <summary>
/// 有限状态机。管理状态的注册、切换与当前状态更新。
/// </summary>
public class StateMachine : MonoBehaviour
{
private readonly Dictionary<string, BaseState> _states = new();
private readonly Dictionary<string, BaseState> _currentStates = new();

public void Register<T>() where T : BaseState
{
    var typeName = typeof(T).Name;
    var state = new GameObject(typeName).AddComponent<T>();
    state.transform.SetParent(transform);
    _states.TryAdd(typeName, state);
}

/// <summary>
/// 退出状态机
/// </summary>
/// <param name="stateName"></param>
/// <param name="nextStateName"></param>
public void ExitState(string stateName, string nextStateName, string strArg1 = "", bool boolArg1 = false, int intArg1 = 0)
{
    _currentStates.TryGetValue(stateName, out var state);
    if (state)
    {
        state.StateExit();
        _currentStates.Remove(stateName);
    }

    if (_currentStates.Count == 0 && !string.IsNullOrEmpty(nextStateName))
    {
        ChangeState(nextStateName, strArg1, boolArg1, intArg1);
    }
}

/// <summary>
/// 添加状态机（与原状态并行）
/// </summary>
public void AddState<T>(string strArg1 = "", bool boolArg1 = false, int intArg1 = 0)
{
    var typeName = typeof(T).Name;
    _states.TryGetValue(typeName, out var currentState);
    if (!_currentStates.TryAdd(typeName, currentState)) return;
    currentState?.StateStart(strArg1, boolArg1, intArg1);
}

/// <summary>
/// 切换状态机（与原状态全部退出）
/// </summary>
public void ChangeState<T>(string strArg1 = "", bool boolArg1 = false, int intArg1 = 0) where T : BaseState
{
    ChangeState(typeof(T).Name, strArg1, boolArg1, intArg1);
}

private void ChangeState(string stateName, string strArg1 = "", bool boolArg1 = false, int intArg1 = 0)
{
    if (_currentStates.ContainsKey(stateName)) return;

    foreach (var state in _currentStates)
    {
        state.Value?.StateExit();
    }
    _currentStates.Clear();

    _states.TryGetValue(stateName, out var currentState);
    _currentStates.TryAdd(stateName, currentState);
    currentState?.StateStart(strArg1, boolArg1, intArg1);
}

private void Update()
{
    if (_currentStates.Count == 0) return;

    // 只更新当前激活的状态
    foreach (var state in _currentStates)
    {
        state.Value?.StateUpdate();
    }
}
}
}
