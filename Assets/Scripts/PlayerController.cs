﻿using System;
﻿using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        #region Variables
        public event Action OnReachedEndOfLevel;
        public event Action<Collider> OnPlayerEnterGuardTrigger;

        public GameObject Alert;
        public float RotationSpeed;

        private List<GuardAlert> _guardsAlerts;
        private Rigidbody _rigidbody;

        private bool _isDisabled;
        #endregion

        [UsedImplicitly]
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        [UsedImplicitly]
        private void Start()
        {
            // Add Disable method call to OnGuardCaughtPlayer action
            GuardUtil.OnGuardCaughtPlayer += Disable;

			// Get all guard game objects and other assets
			_guardsAlerts = new List<GuardAlert>();
            foreach (var guard in GameObject.FindGameObjectsWithTag("Guard"))
            {
                _guardsAlerts.Add(
                    new GuardAlert(guard, 
                                   Instantiate(Alert, transform.position + -(Vector3.forward)/2, transform.rotation, transform.parent), 
                                   Alert.GetComponent<Renderer>().sharedMaterial.color));
            }
        }

        [UsedImplicitly]
        private void Update()
        {
            // If disabled stop the player moving
            _rigidbody.isKinematic |= _isDisabled;

            DrawAlertArrows();
        }

        [UsedImplicitly]
        private void OnTriggerEnter(Collider col)
        {
            HandleFinishCollider(col);
            HandleGuardTriggerColliders(col);
        }

        private void HandleFinishCollider(Collider col)
        {
            // If collider tag is finish then disable player and level Win UI
            if (col.tag != "Finish") return;

            Disable();
            if (OnReachedEndOfLevel != null)
            {
                OnReachedEndOfLevel();
            }
        }

        private void HandleGuardTriggerColliders(Collider col)
        {
            if (col.tag != "GuardTrigger") return;
            if (OnPlayerEnterGuardTrigger == null) return;

            OnPlayerEnterGuardTrigger(col);
        }

        private void Disable()
		{
			// Disables player when the OnGuardCaughtPlayer action is called from guard script
			_isDisabled = true;
		}

		private void DrawAlertArrows()
		{
			foreach (var guardAlert in _guardsAlerts)
			{
                // Get GuardUtil from Guard and get state from that
                var state = guardAlert.Guard.GetComponent<GuardUtil>().state;
                var guardCanSeePlayer = guardAlert.Guard.GetComponent<Sight>().VisibleTargets.Count > 0;

                if (state == GuardUtil.State.Patrol && guardCanSeePlayer || state == GuardUtil.State.Investigate)
				{
					// Calculate Rotation and Direction
					CalculateArrowDirection(guardAlert.Guard, guardAlert.Alert);

					// Colour
					guardAlert.Alert.GetComponent<Renderer>().material.color = Color.black;
				} else if (state == GuardUtil.State.Alert || state == GuardUtil.State.Chase)
				{
					// Calculate Rotation and Direction
					CalculateArrowDirection(guardAlert.Guard, guardAlert.Alert);

					// Colour
					guardAlert.Alert.GetComponent<Renderer>().material.color = Color.red;
				} else
				{
					guardAlert.Alert.SetActive(false);
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
	public GameObject Guard;
	public GameObject Alert;
    public Color OriginalColor;

    public GuardAlert(GameObject guard, GameObject alert, Color originalColor)
	{
		Guard = guard;
		Alert = alert;
        OriginalColor = originalColor;
	}
}