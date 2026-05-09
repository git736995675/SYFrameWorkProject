using System.Collections;

namespace SY
{
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
}