using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public class FieldOfView : MonoBehaviour
    {

        public float ViewRadius;
        [Range(0, 360)]
        public float ViewAngle;

        public LayerMask PlayerMask;
        public LayerMask ObstacleMask;
        public float LookDelay = 0.2f;

        [HideInInspector]
        public List<Transform> VisibleTargets = new List<Transform>();

        private void Start()
        {
            StartCoroutine(FindTargetsWithDelay(LookDelay));
        }

        IEnumerator FindTargetsWithDelay(float delay)
        {
            while (true)
            {
                yield return new WaitForSeconds(delay);
                FindVisibleTargets();
            }
        }

        void FindVisibleTargets()
        {
            VisibleTargets.Clear();

            // Find targets within sphere radius
            VisibleTargets.AddRange(LocateTargetsWithinSphere(Physics.OverlapSphere(transform.position, ViewRadius, PlayerMask)));
        }

        private List<Transform> LocateTargetsWithinSphere(Collider[] targetsWithinSphere) {
            List<Transform> list = new List<Transform>();

            foreach(Collider targetCollider in targetsWithinSphere) {
                // Get the target transform
                var directionToTarget = (targetCollider.transform.position - transform.position).normalized;
                var angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
                var distanceToTarget = Vector3.Distance(transform.position, targetCollider.transform.position);

                // Check within View Angle
                if(angleToTarget < ViewAngle/2) {
                    // Ensure no obstacles blocking the view using raycast
                    if(!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, ObstacleMask)) {
                        list.Add(targetCollider.transform);
                    }
                }
            }

            return list;
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
