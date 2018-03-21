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
        public static void GuardOnCaughtPlayer() {
            if (OnGuardCaughtPlayer != null)
            {
                OnGuardCaughtPlayer();
            }
        }

        /// <summary>
        /// When player is in sight for longer than "timeToSpotPlayer" variable change state to alert.
        /// </summary>
        /// <param name="fov"></param>
        /// <param name="playerVisibleTimer"></param>
        /// <param name="timeToSpotPlayer"></param>
        public void SpotPlayer(FieldOfView fov, ref float playerVisibleTimer, float timeToSpotPlayer)
        {
            if(fov == null) return;

            if (fov.VisibleTargets.Count > 0) playerVisibleTimer += Time.deltaTime;
            else playerVisibleTimer -= Time.deltaTime;

            playerVisibleTimer = Mathf.Clamp(playerVisibleTimer, 0, timeToSpotPlayer);

            if (playerVisibleTimer >= timeToSpotPlayer) state = GuardUtil.State.Alert;
        }

        /// <summary>
        /// When player is in sight for longer than "timeToHearPlayer" variable change state to alert.
        /// </summary>
        /// <param name="hearing">Hearing.</param>
        /// <param name="playerHearedTimer">Player heared timer.</param>
        /// <param name="timeToHearPlayer">Time to hear player.</param>
        public void ListenForPlayer(Hearing hearing, ref float playerHearedTimer, float timeToHearPlayer) {
            if(hearing == null) return;

            if (hearing.HeardTargets.Count > 0) playerHearedTimer += Time.deltaTime;
            else playerHearedTimer -= Time.deltaTime;

            playerHearedTimer = Mathf.Clamp(playerHearedTimer, 0, timeToHearPlayer);
            if (playerHearedTimer >= timeToHearPlayer) state = GuardUtil.State.Alert;
        }

        /// <summary>
        /// Return true if can see the player.
        /// </summary>
        /// <returns><c>true</c>, if see player was caned, <c>false</c> otherwise.</returns>
        /// <param name="fov">Fov.</param>
        public static bool CanSeePlayer(FieldOfView fov)
        {
            return fov.VisibleTargets.Count > 0;
        }

        public static bool CanSeeGuard(FieldOfView fov)
        {
            return fov.VisibleGuards.Count > 0;
        }


        /// <summary>
        /// return true if can hear the player.
        /// </summary>
        /// <returns><c>true</c>, if hear player was caned, <c>false</c> otherwise.</returns>
        /// <param name="hearing">Hearing.</param>
        public static bool CanHearPlayer(Hearing hearing)
        {
            return hearing.HeardTargets.Count > 0;
        }

        /// <summary>
        /// Returns true if blocked by obstacle
        /// </summary>
        /// <returns><c>true</c>, if blocked by obstacle was ised, <c>false</c> otherwise.</returns>
        /// <param name="currentPosition">Current position.</param>
        /// <param name="targetPosition">Target position.</param>
        /// <param name="obstacles">Obstacles.</param>
		public static bool IsBlockedByObstacle(Vector3 currentPosition, Vector3 targetPosition, LayerMask obstacles)
		{
			var direction = (targetPosition - currentPosition).normalized;
			var distance = Vector3.Distance(currentPosition, targetPosition);

			if (Physics.Raycast(currentPosition, direction, distance, obstacles))
			{
				return true;
			}
			return false;
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

                while (!newPositionNode.Walkable)
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

                while (!newPositionNode.Walkable || Vector3.Distance(lastPositionNode.WorldPosition, newPositionNode.WorldPosition) <= 2.0f)
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

        public static void DrawTriggerGizmos(Collider[] triggers)
        {
            if (triggers.Length <= 0) return;
            foreach (var trigger in triggers)
            {
                // Get Collider
                var triggerCollider = trigger.GetComponent<Collider>();
                if (triggerCollider is BoxCollider)
                {
                    // Draw Box Collider
                    Gizmos.DrawWireCube(triggerCollider.bounds.center, triggerCollider.bounds.size);
                }
                else if (triggerCollider is SphereCollider)
                {
                    // Draw Sphere Collider
                    var col = (SphereCollider)triggerCollider;
                    Gizmos.DrawWireSphere(col.center, col.radius);
                }
            }
        }
    }
}
