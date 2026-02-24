using UnityEngine;

namespace SY
{
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