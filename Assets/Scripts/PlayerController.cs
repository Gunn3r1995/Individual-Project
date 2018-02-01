using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        public System.Action OnReachedEndOfLevel;
        //bool disabled;

        void Start()
        {
            //Guard.OnGuardApprehendingPlayer += Disable;
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (collider.tag == "Finish")
            {
                //Disable();
                if (OnReachedEndOfLevel != null)
                {
                    OnReachedEndOfLevel();
                }
            }
        }

        private void OnDestroy()
        {
            //Guard.OnGuardApprehendingPlayer -= Disable;
        }

        /*
        void Disable()
        {
            disabled = true;
        }
        */
    }
}
