using System;

namespace SY
{
    // 流程步骤类
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