using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZSYFramework.Core.Json;

namespace ZSYFramework.Core
{
/// <summary>
/// 流程节点基类。表示一个可执行的业务流程单元，由 ProcessManager 驱动。
/// </summary>
public class Process : MonoBehaviour
{
// �����¼�
public ProcessEvent onProcessStart = new();
public ProcessEvent onProcessUpdate = new();
public ProcessEvent onProcessComplete = new();

private readonly List<ProcessStep> _processSteps = new();
private int _totalSteps = 0;
private int _completedSteps = 0;
private bool _isRunning = false;

// ע��һ�����̲���
public void RegisterProcess(int weight, Action<Action> processFunc, string description = "")
{
    if (_isRunning)
    {
        Debug.LogWarning("Cannot register new processes while the manager is running");
        return;
    }

    _processSteps.Add(new ProcessStep(weight, processFunc, description));
}

// ��ʼִ����������
public void ExecuteProcesses()
{
    if (_isRunning || _processSteps.Count == 0) return;

    _isRunning = true;
    _completedSteps = 0;
    _totalSteps = _processSteps.Count;

    // ��Ȩ������
    _processSteps.Sort((a, b) => a.Weight.CompareTo(b.Weight));

    StartCoroutine(RunProcesses());
}

private IEnumerator RunProcesses()
{
    var currentWeight = _processSteps[0].Weight;
    var currentBatch = new List<ProcessStep>();

    // ����ִ����ͬȨ�صĲ���
    foreach (var t in _processSteps)
    {
        if (t.Weight != currentWeight)
        {
            // ִ�е�ǰ����
            yield return ExecuteBatch(currentBatch);

            // ׼����һ����
            currentWeight = t.Weight;
            currentBatch.Clear();
        }

        currentBatch.Add(t);
    }

    // ִ�����һ��
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

    // ��ʼ����������Э��
    foreach (var step in batch)
    {
        step.ProcessFunc(StepCompleteCallback);
        onProcessStart?.Invoke(step.Description, 0f);
        continue;

        // Ϊÿ�����贴����ɻص�
        void StepCompleteCallback()
        {
            batchCompleted++;
            _completedSteps++;
            var progress = (float)_completedSteps / _totalSteps;
            onProcessUpdate?.Invoke(step.Description, progress);
        }
    }

    // �ȴ����������в������
    while (batchCompleted < batchSize)
    {
        yield return null;
    }
}

// �������̹�����
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
