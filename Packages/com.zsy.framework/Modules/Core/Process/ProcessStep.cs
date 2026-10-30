using System;
using ZSYFramework.Core.Json;

namespace ZSYFramework.Core
{
// 流程步骤类
/// <summary>
/// 流程步骤描述，定义流程中的单个执行阶段。
/// </summary>
public class ProcessStep
{
public int Weight { get; private set; }
public Action<Action> ProcessFunc { get; private set; }
public string Description { get; private set; }

public ProcessStep(int weight, Action<Action> processFunc, string description)
{
    Weight = weight;
    ProcessFunc = processFunc;
    Description = description;
}
}
}
