using System.Collections;
using SY;

namespace ZSYFramework.Runtime.ComponentExt.Coroutine
{
    public class CoroutineExt : MonoSingleton<CoroutineExt>
    {
        public void StartC(IEnumerator ie)
        {
            StartCoroutine(ie);
        }
    }
}