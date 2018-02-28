using System;
using Assets.Scripts.AStar;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts
{
    public class GuardUtil : MonoBehaviour {
        public static event Action OnGuardCaughtPlayer;

        public enum State { Patrol, Alert, Investigate, Chase }

        public State state;

        /// <summary>
        /// Calls OnGuardCaughtPlayer action methods if any methods are attached
        /// </summary>
        public void GuardOnCaughtPlayer() {
            if (OnGuardCaughtPlayer != null)
            {
                OnGuardCaughtPlayer();
            }
        }

        /// <summary>
        /// When player is in sight for longer than "timeToSpotPlayer" variable change state to alert
        /// </summary>
        /// <param name="fov"></param>
        /// <param name="playerVisibleTimer"></param>
        /// <param name="timeToSpotPlayer"></param>
        public void SpotPlayer(FieldOfView fov, ref float playerVisibleTimer, float timeToSpotPlayer)
        {
            if (fov.VisibleTargets.Count > 0) playerVisibleTimer += Time.deltaTime;
            else playerVisibleTimer -= Time.deltaTime;

            playerVisibleTimer = Mathf.Clamp(playerVisibleTimer, 0, timeToSpotPlayer);

            if (playerVisibleTimer >= timeToSpotPlayer) state = GuardUtil.State.Alert;
        }

        /// <summary>
        /// Returns true if can see player (No Time Restraints)
        /// </summary>
        /// <param name="fov"></param>
        /// <returns></returns>
        public static bool CanSeePlayer(FieldOfView fov)
        {
            return fov.VisibleTargets.Count > 0;
        }

        /// <summary>
        /// Creates a randomly generated "walkable" vector3 position. <para />
        /// Returns default Vector3 if cannot create walkable position
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="dist"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        public static Vector3 CreateRandomWalkablePosition(Vector3 origin, float dist, AStar.Grid grid = null)
        {
            if (grid == null) grid = FindObjectOfType<AStar.Grid>();

            if (grid != null)
            {
                var randomPosition = CreateRandomPosition(origin, dist);
                var newPositionNode = grid.GetNodeFromWorldPoint(randomPosition);

                while (!newPositionNode.walkable)
                {
                    randomPosition = CreateRandomPosition(origin, dist);
                    newPositionNode = grid.GetNodeFromWorldPoint(randomPosition);
                }
                return randomPosition;
            }

            Debug.LogError("AStar.Grid object doesn't exist or cannot be found");
            return new Vector3();
        }

        /// <summary>
        /// Creates a randomly generated "walkable" vector3 position, remebering the last position <para />
        /// Returns default Vector3 if cannot create walkable position
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="dist"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        public static Vector3 CreateRandomWalkablePosition(Vector3 origin, float dist, ref Vector3 lastPos, AStar.Grid grid = null)
        {
            if (grid == null)
            {
                grid = FindObjectOfType<AStar.Grid>();
            }

            if (grid != null)
            {
                var randomPosition = CreateRandomPosition(origin, dist);
                var newPositionNode = grid.GetNodeFromWorldPoint(randomPosition);
                var lastPositionNode = grid.GetNodeFromWorldPoint(lastPos);

                while (!newPositionNode.walkable || Vector3.Distance(lastPositionNode.WorldPosition, newPositionNode.WorldPosition) <= 2.0f)
                {
                    randomPosition = CreateRandomPosition(origin, dist);
                    newPositionNode = grid.GetNodeFromWorldPoint(randomPosition);
                }

                lastPos = randomPosition;
                return randomPosition;
            }
            else
            {
                Debug.LogError("Grid doesn't exist");
                lastPos = new Vector3();
                return new Vector3();
            }

        }

        /// <summary>
        /// Creates a random vector3 position
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="dist"></param>
        /// <returns></returns>
        private static Vector3 CreateRandomPosition(Vector3 origin, float dist)
        {
            var randomPosition = Random.insideUnitSphere;
            randomPosition.y = 0;
            randomPosition.Normalize();
            randomPosition *= dist;

            return randomPosition + origin;
        }

        /// <summary>
        /// Draws a line to the next waypoint
        /// </summary>
        /// <param name="currentPosition"></param>
        /// <param name="waypoints"></param>
        /// <param name="waypointIndex"></param>
        public static void DrawNextWaypointLineGizmos(Vector3 currentPosition, GameObject[] waypoints, int waypointIndex)
        {
            if (waypoints == null) return;
            if (waypoints.Length < 1) return;

            Gizmos.color = Color.black;
            Gizmos.DrawLine(currentPosition, waypoints[waypointIndex].transform.position);
        }

        /// <summary>
        /// Draws lines between waypoints and spheres for the waypoints
        /// </summary>
        /// <param name="waypoints"></param>
        public static void DrawWaypointGizmos(GameObject[] waypoints)
        {
            if (waypoints == null) return;
            if (waypoints.Length < 1) return;

            var startPosition = waypoints[0].transform.position;
            var previousPosition = startPosition;

            foreach (var waypoint in waypoints)
            {
                Gizmos.DrawSphere(waypoint.transform.position, 0.3f);
                Gizmos.DrawLine(previousPosition, waypoint.transform.position);
                previousPosition = waypoint.transform.position;
            }

            Gizmos.DrawLine(previousPosition, startPosition);
        }
    }
}
