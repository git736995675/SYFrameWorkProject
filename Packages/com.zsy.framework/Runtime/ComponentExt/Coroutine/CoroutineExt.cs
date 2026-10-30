using System.Collections;

namespace SY
{
    public class CoroutineExt : MonoSingleton<CoroutineExt>
    {
        public void StartC(IEnumerator ie)
        {
            StartCoroutine(ie);
        }
    }
}