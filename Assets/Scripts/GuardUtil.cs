using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using Assets.Scripts.AStar;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts
{
    public class GuardUtil : MonoBehaviour {
        public static event Action OnGuardCaughtPlayer;

        public enum State { Patrol, Alert, Investigate, Chase }

        public State state { get; set; }

        /// <summary>
        /// Calls OnGuardCaughtPlayer action methods if any methods are attached
        /// </summary>
        public void GuardOnCaughtPlayer() {
            if (OnGuardCaughtPlayer != null)
            {
                OnGuardCaughtPlayer();
            }
        }

        public void SpotPlayer(FieldOfView fov, ref float playerVisibleTimer, float timeToSpotPlayer)
        {
            if (fov.VisibleTargets.Count > 0) playerVisibleTimer += Time.deltaTime;
            else playerVisibleTimer -= Time.deltaTime;

            playerVisibleTimer = Mathf.Clamp(playerVisibleTimer, 0, timeToSpotPlayer);

            if (playerVisibleTimer >= timeToSpotPlayer) state = GuardUtil.State.Alert;
        }

        public static bool CanSeePlayer(FieldOfView fov)
        {
            return fov.VisibleTargets.Count > 0;
        }

        public static Vector3 CreateRandomWalkablePosition(Vector3 origin, float dist, AStar.Grid grid = null)
        {
            if (grid == null)
            {
                grid = FindObjectOfType<AStar.Grid>();
            }

            if (grid != null)
            {
                Vector3 randomPosition = CreateRandomPosition(origin, dist);

                Node newPositionNode = grid.GetNodeFromWorldPoint(randomPosition);
                while (!newPositionNode.walkable)
                {
                    randomPosition = CreateRandomPosition(origin, dist);
                    newPositionNode = grid.GetNodeFromWorldPoint(randomPosition);
                }
                return randomPosition;
            }
            else
            {
                Debug.LogError("Grid doesn't exist");
                return new Vector3();
            }

        }

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

        public static Vector3 CreateRandomPosition(Vector3 origin, float dist)
        {
            var randomPosition = Random.insideUnitSphere;
            randomPosition.y = 0;
            randomPosition.Normalize();
            randomPosition *= dist;

            return randomPosition + origin;
        }

        public static void DrawNextWaypointLineGizmos(Vector3 currentPosition, GameObject[] waypoints, int waypointIndex)
        {
            if (waypoints == null || waypoints.Length < 1) return;

            Gizmos.color = Color.black;
            Gizmos.DrawLine(currentPosition, waypoints[waypointIndex].transform.position);
        }

        public static void DrawWaypointGizmos(GameObject[] waypoints)
        {
            if (waypoints == null || waypoints.Length < 1) return;

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

        //private bool DestinationReached(Vector3 currentPos, Vector3 targetPos)
        //{
        //    var currentNode = _grid.GetNodeFromWorldPoint(currentPos);
        //    var targetNode = _grid.GetNodeFromWorldPoint(targetPos);

        //    if (currentNode != targetNode) return false;

        //    print("Position Reached");
        //    return true;
        //}
    }
}
