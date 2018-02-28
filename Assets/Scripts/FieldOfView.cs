using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts
{
    public class FieldOfView : MonoBehaviour
    {

        public float ViewRadius;
        [Range(0, 360)]
        public float ViewAngle;

        public LayerMask PlayerMask;
        public LayerMask GuardMask;
        public LayerMask ObstacleMask;
        public float LookDelay = 0.2f;

        [HideInInspector]
        public List<Transform> VisibleTargets = new List<Transform>();
        [HideInInspector]
        public List<Transform> VisibleGuards = new List<Transform>();

        private void Start()
        {
            StartCoroutine(FindTargetsWithDelay(LookDelay));
        }

        private IEnumerator FindTargetsWithDelay(float delay)
        {
            while (true)
            {
                yield return new WaitForSeconds(delay);
                FindVisibleTargets();
            }
        }

        private void FindVisibleTargets()
        {
            VisibleTargets.Clear();

            // Find targets within sphere radius
            VisibleTargets.AddRange(LocateTargetsWithinSphere(Physics.OverlapSphere(transform.position, ViewRadius, PlayerMask)));

            // Locate Guards within sphere radius
            VisibleTargets.AddRange(LocateTargetsWithinSphere(Physics.OverlapSphere(transform.position, ViewRadius, GuardMask)));
        }

        private IEnumerable<Transform> LocateTargetsWithinSphere(Collider[] targetsWithinSphere)
        {
            return (
                from targetCollider 
                in targetsWithinSphere
                let directionToTarget = (targetCollider.transform.position - transform.position).normalized
                let angleToTarget = Vector3.Angle(transform.forward, directionToTarget)
                let distanceToTarget = Vector3.Distance(transform.position, targetCollider.transform.position)
                where angleToTarget < ViewAngle / 2
                where !Physics.Raycast(transform.position, directionToTarget, distanceToTarget, ObstacleMask)
                select targetCollider.transform).ToList();
        }

        public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += transform.eulerAngles.y;
            }
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }

    }
}
