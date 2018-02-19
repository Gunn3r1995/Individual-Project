using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;

namespace Assets.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        #region Variables
        public System.Action OnReachedEndOfLevel;

        public GameObject alert;
        public float RotationSpeed;

        private List<GuardAlert> guardsAlerts;

        private Rigidbody _rigidbody;

        private bool _isDisabled = false;
        #endregion

        void Start()
        {
            // Add Disable method call to OnGuardCaughtPlayer action
            Guard.OnGuardCaughtPlayer += Disable;

			// Get all guard game objects and other assets
			guardsAlerts = new List<GuardAlert>();
            foreach (GameObject guard in GameObject.FindGameObjectsWithTag("Guard"))
            {
                guardsAlerts.Add(
                    new GuardAlert(guard, 
                                   Instantiate(alert, transform.position + -(Vector3.forward)/2, transform.rotation, transform.parent), 
                                   alert.GetComponent<Renderer>().sharedMaterial.color));
            }
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            // If disabled stop the player moving
            _rigidbody.isKinematic |= _isDisabled;

            DrawAlertArrows();
        }

        private void OnTriggerEnter(Collider collider)
        {
            // If collider tag is finish then disable player and level Win UI
            if (collider.tag == "Finish")
            {
                Disable();
                if (OnReachedEndOfLevel != null)
                {
                    OnReachedEndOfLevel();
                }
            }
        }

		void Disable()
		{
			// Disables player when the OnGuardCaughtPlayer action is called from guard script
			_isDisabled = true;
		}

        private void OnDestroy()
        {
            // Remove disabled 
            Guard.OnGuardCaughtPlayer -= Disable;
        }

		private void DrawAlertArrows()
		{
			foreach (GuardAlert guardAlert in guardsAlerts)
			{
				var guard = guardAlert.guard.GetComponent<Guard>();

				if (guard.state == Guard.State.Patrol && guard.CanSeePlayer() || guard.state == Guard.State.Investigate)
				{
					// Calculate Rotation and Direction
					CalculateArrowDirection(guardAlert.guard, guardAlert.alert);

					// Colour
					guardAlert.alert.GetComponent<Renderer>().material.color = Color.black;
				}
				else if (guard.state == Guard.State.Alert || guard.state == Guard.State.Chase)
				{
					// Calculate Rotation and Direction
					CalculateArrowDirection(guardAlert.guard, guardAlert.alert);

					// Colour
					guardAlert.alert.GetComponent<Renderer>().material.color = Color.red;
				}
				else
				{
					guardAlert.alert.SetActive(false);
				}
			}
		}

		private void CalculateArrowDirection(GameObject guard, GameObject alertObj)
		{
			alertObj.SetActive(true);

			var direction = (guard.transform.position - transform.position).normalized;
			var targetRotation = Quaternion.LookRotation(guard.transform.position - transform.position);

            // Rotate and position alert to the guard
			alertObj.transform.rotation = Quaternion.Lerp(alertObj.transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
			alertObj.transform.position = transform.position + direction;
		}
    }
}

public struct GuardAlert
{
	public GameObject guard;
	public GameObject alert;
    public Color originalColor;

    public GuardAlert(GameObject guard, GameObject alert, Color originalColor)
	{
		this.guard = guard;
		this.alert = alert;
        this.originalColor = originalColor;
	}
}