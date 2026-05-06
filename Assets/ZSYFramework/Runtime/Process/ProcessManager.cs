using UnityEngine;

namespace SY
{
    /// <summary>
    /// 步骤管理
    /// </summary>
    public class ProcessManager : MonoSingleton<ProcessManager>
    {
        private readonly MemoryPool<Process> _pool = new();

        /// <summary>
        /// 取出一个步骤
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