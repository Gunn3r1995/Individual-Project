using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Assets.Scripts
{
    public class Sight : MonoBehaviour
    {

        public float ViewRadius;
        [Range(0, 360)]
        public float ViewAngle;

        public LayerMask PlayerMask;
        public LayerMask GuardMask;
        public LayerMask ObstacleMask;
        public float LookDelay = 0.2f;

        [HideInInspector]
        public List<GameObject> VisibleTargets = new List<GameObject>();
        [HideInInspector]
        public List<GameObject> VisibleGuards = new List<GameObject>();

        [UsedImplicitly]
        private void Start()
        {
            StartCoroutine(FindTargetsWithDelay(LookDelay));
        }

        /// <summary>
        /// Finds the targets with delay.
        /// </summary>
        /// <returns>null</returns>
        /// <param name="delay">Delay.</param>
		[UsedImplicitly]
        private IEnumerator FindTargetsWithDelay(float delay)
        {
            while (true)
            {
                yield return new WaitForSeconds(delay);
                FindVisibleTargets();
            }
        }

        /// <summary>
        /// Finds the visible targets.
        /// </summary>
        private void FindVisibleTargets()
        {
            VisibleTargets.Clear();
            VisibleGuards.Clear();

            // Find targets within sphere radius
            VisibleTargets.AddRange(LocateTargetsWithinSphere(Physics.OverlapSphere(transform.position, ViewRadius, PlayerMask), PlayerMask));

            // Locate Guards within sphere radius
            VisibleGuards.AddRange(LocateTargetsWithinSphere(Physics.OverlapSphere(transform.position, ViewRadius, GuardMask), GuardMask));
        }

        /// <summary>
        /// Locates the targets within sphere which are not blocked by obstacles and within field of view angle.
        /// </summary>
        /// <returns>The targets within sphere.</returns>
        /// <param name="targetsWithinSphere">Visbible targets within sphere.</param>
        /// <param name="targetMask"></param>
        private IEnumerable<GameObject> LocateTargetsWithinSphere(Collider[] targetsWithinSphere, LayerMask targetMask)
        {
            // Confirm that targets within sphere are not blocked by obstacles and within field of view angle
            return (
                from targetCollider 
                in targetsWithinSphere
                let height = targetCollider.bounds.size.y
                let directionToTarget = (targetCollider.transform.position + Vector3.up * height - transform.position).normalized
                let angleToTarget = Vector3.Angle(transform.forward, directionToTarget)
                let distanceToTarget = Vector3.Distance(transform.position, targetCollider.transform.position + Vector3.up * height)
                where angleToTarget < ViewAngle / 2
                where !Physics.Raycast(transform.position, directionToTarget, distanceToTarget, ObstacleMask) && Physics.Raycast(transform.position, directionToTarget, distanceToTarget, targetMask)
                select targetCollider.gameObject).ToList();
        }

        /// <summary>
        /// Dirs from angle.
        /// </summary>
        /// <returns>The from angle.</returns>
        /// <param name="angleInDegrees">Angle in degrees.</param>
        /// <param name="angleIsGlobal">If set to <c>true</c> angle is global.</param>
        public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
                angleInDegrees += transform.eulerAngles.y;
            
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }
    }
}
