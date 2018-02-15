using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        public System.Action OnAlerted;
        public System.Action OnReachedEndOfLevel;
        public Texture GuardAlertedTexture;
        public Texture GuardAlertedTextureRed;
        //bool disabled;

        private Camera _camera;
        private GameObject[] guards;
        public Vector2 size = new Vector2(50, 50);

		public float angle = 45;

        void Start()
        {
            //Guard.OnGuardApprehendingPlayer += Disable;
            if (GuardAlertedTexture == null) {
                GuardAlertedTexture = new Texture();
            }

            guards = GameObject.FindGameObjectsWithTag("Guard");
            _camera = FindObjectOfType<Camera>();

        }

        private void Update()
        {
            
        }

        private void OnGUI()
        {

            foreach (var guard in guards)
            {
                var state = guard.GetComponent<Guard>().state;
                    //Vector3 direction = (transform.position - guard.transform.position).normalized;

                Vector3 guardScreenPoint = _camera.WorldToViewportPoint(guard.transform.position).normalized;
                //guardScreenPoint.y = 0;


                Vector2 centre = new Vector3(Screen.width / 2, Screen.height / 2);
                //print("Centre: " + centre);

                bool isForwards = false;
                bool isBackwards = false;
                bool isLeft = false;
                bool isRight = false;

                float forwardAngle = Vector3.Angle(_camera.transform.forward, (guard.transform.position - _camera.transform.position));
                float rightAngle = Vector3.Angle(_camera.transform.right, (guard.transform.position - _camera.transform.position));

                isForwards |= forwardAngle < 90;
                isBackwards |= forwardAngle > 90;
                isRight |= rightAngle < 90;
                isLeft |= rightAngle > 90;

                Vector2 guardScreenPos;
                if (isForwards && isLeft)
                {
                    // Top Left
                    guardScreenPos = new Vector2(guardScreenPoint.x, -guardScreenPoint.z).normalized;
                }
                else if (isForwards && isRight)
                {
                    // Top Right
                    guardScreenPos = new Vector2(guardScreenPoint.x, -guardScreenPoint.z).normalized;
                }
                else if (isBackwards && isLeft)
                {
                    // Bottom Left
                    guardScreenPos = new Vector2(-guardScreenPoint.x, -guardScreenPoint.z).normalized;
                }
                else if (isBackwards && isRight)
                {
                    // Bottom Right
                    guardScreenPos = new Vector2(-guardScreenPoint.x, -guardScreenPoint.z).normalized;
                }
                else
                {
                    guardScreenPos = new Vector2(guardScreenPoint.x, -guardScreenPoint.z).normalized;
                }

                Vector2 pos = centre + guardScreenPos * 250;

                Vector2 pivot = new Vector2(pos.x + (size.x / 2), pos.y + (size.y / 2));

                if (isForwards && isLeft)
                {
                    GUIUtility.RotateAroundPivot((forwardAngle + -rightAngle) / 2, pivot);
                }
                else if (isForwards && isRight)
                {
                    GUIUtility.RotateAroundPivot((forwardAngle + rightAngle) / 2, pivot);
                }
                else if (isBackwards && isLeft)
                {
                    GUIUtility.RotateAroundPivot((-forwardAngle + -rightAngle) / 2, pivot);
                }
                else if (isBackwards && isRight)
                {
                    GUIUtility.RotateAroundPivot((forwardAngle + rightAngle) / 2, pivot);
                }

                if(state == Guard.State.Patrol && guard.GetComponent<Guard>().CanSeePlayer()) {
					GUI.DrawTexture(new Rect(pos, size), GuardAlertedTexture);
                } else if (state == Guard.State.Alert || state == Guard.State.Investigate || state == Guard.State.Chase)
                {
                    //Change to Red
                    GUI.DrawTexture(new Rect(pos, size), GuardAlertedTextureRed);
                }

            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (collider.tag == "Finish") {
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
