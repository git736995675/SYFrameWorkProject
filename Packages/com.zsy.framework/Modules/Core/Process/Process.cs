using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ZSYFramework.Core
{
/// <summary>
/// 流程节点基类。表示一个可执行的业务流程单元，由 ProcessManager 驱动。
/// </summary>
public class Process : MonoBehaviour
{
// 流程事件
public ProcessEvent onProcessStart = new();
public ProcessEvent onProcessUpdate = new();
public ProcessEvent onProcessComplete = new();

private readonly List<ProcessStep> _processSteps = new();
private int _totalSteps = 0;
private int _completedSteps = 0;
private bool _isRunning = false;

// 注册一个流程步骤
public void RegisterProcess(int weight, Action<Action> processFunc, string description = "")
{
    if (_isRunning)
    {
        Debug.LogWarning("Cannot register new processes while the manager is running");
        return;
    }

    _processSteps.Add(new ProcessStep(weight, processFunc, description));
}

// 开始执行所有步骤
public void ExecuteProcesses()
{
    if (_isRunning || _processSteps.Count == 0) return;

    _isRunning = true;
    _completedSteps = 0;
    _totalSteps = _processSteps.Count;

    // 按权重排序
    _processSteps.Sort((a, b) => a.Weight.CompareTo(b.Weight));

    StartCoroutine(RunProcesses());
}

private IEnumerator RunProcesses()
{
    var currentWeight = _processSteps[0].Weight;
    var currentBatch = new List<ProcessStep>();

    // 顺序执行不同权重的步骤
    foreach (var t in _processSteps)
    {
        if (t.Weight != currentWeight)
        {
            // 执行当前批
            yield return ExecuteBatch(currentBatch);

            // 准备下一批
            currentWeight = t.Weight;
            currentBatch.Clear();
        }

        currentBatch.Add(t);
    }

    // 执行最后一批
    if (currentBatch.Count > 0)
    {
        yield return ExecuteBatch(currentBatch);
    }

    onProcessComplete?.Invoke("All processes completed", 1f);
    _isRunning = false;
    Recycle();
}

private IEnumerator ExecuteBatch(List<ProcessStep> batch)
{
    var batchCompleted = 0;
    var batchSize = batch.Count;

    // 初始化执行本批协程
    foreach (var step in batch)
    {
        step.ProcessFunc(StepCompleteCallback);
        onProcessStart?.Invoke(step.Description, 0f);
        continue;

        // 为每个步骤创建完成回调
        void StepCompleteCallback()
        {
            batchCompleted++;
            _completedSteps++;
            var progress = (float)_completedSteps / _totalSteps;
            onProcessUpdate?.Invoke(step.Description, progress);
        }
    }

    // 等待本批所有步骤完成
    while (batchCompleted < batchSize)
    {
        yield return null;
    }
}

// 回收流程管理器
private void Recycle()
{
    if (_isRunning)
    {
        StopAllCoroutines();
        _isRunning = false;
    }

    _processSteps.Clear();
    onProcessComplete?.RemoveAllListeners();
    onProcessStart?.RemoveAllListeners();
    onProcessStart?.RemoveAllListeners();
    ProcessManager.Instance.Recycle(this);
}
}
}
