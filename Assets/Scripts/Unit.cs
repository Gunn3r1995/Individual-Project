using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class Unit : MonoBehaviour
    {
        public Transform Target;
        private float speed = 10;
        private Vector3[] path;
        private int targetIndex;

        void Start()
        {
            print("Position: " + transform.localPosition);
            print("Position: " + transform.position);

            print("Target: " + Target.position);
            PathRequestManager.RequestPath(this.transform.position, Target.position, OnPathFound);
        }

        public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
        {
            print("Path Found");
            if (pathSuccessful)
            {
                path = newPath;
                StopCoroutine("FollowPath");
                StartCoroutine("FollowPath");
            }
        }

        private IEnumerator FollowPath()
        {
            print("Follow Path");
            Vector3 currentWaypoint = path[0];

            while (true)
            {
                if (transform.position == currentWaypoint)
                {
                    targetIndex++;
                    if (targetIndex >= path.Length)
                    {
                        yield break;
                    }
                    currentWaypoint = path[targetIndex];
                }
                transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, speed * Time.deltaTime);
                yield return null;
            }
        }

        public void OnDrawGizmos()
        {
            if (path != null)
            {
                for (int i = targetIndex; i < path.Length; i++)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawCube(path[i], Vector3.one);

                    Gizmos.DrawLine(i == targetIndex ? transform.position : path[i - 1], path[i]);
                }
            }
        }
    }
}
