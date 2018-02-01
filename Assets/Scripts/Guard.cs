using System.Collections;
using Assets.Scripts.AStar;
using UnityEngine;

namespace Assets.Scripts
{
    public class Guard : MonoBehaviour
    {
        private GridAgent _gridAgent;
        public enum State { Patrol, Alert, Search, Chase }
        public State state;

		// Patrolling
		public GameObject[] Waypoints;
		public bool RandomWaypoints;
		public float PatrolSpeed = 1.0f;
		private int _waypointIndex = 0;

		private bool _patrolling;

        void Start()
        {
			state = State.Patrol;
            _gridAgent = GetComponent<GridAgent>();
        }

        void Update()
        {
            FSM();
        }

		private void FSM()
		{
			switch (state)
			{
				case State.Patrol:
					if (!_patrolling)
					{
                        StartCoroutine(Patrol());
					}
					break;
				case State.Alert:
					break;
				case State.Search:
					print("Searching");
					break;
				case State.Chase:
					print("Chase State");
					break;
			}
		}

		private IEnumerator Patrol()
		{
			print("Patrolling");
			_patrolling = true;

			while (state == State.Patrol)
			{
                _gridAgent.SetSpeed(PatrolSpeed);
				var distance = Vector3.Distance(transform.position, Waypoints[_waypointIndex].transform.position);
				if (distance >= 2.0f)
				{
                    _gridAgent.SetDestination(transform.position, Waypoints[_waypointIndex].transform.position);
				}
				if (distance <= 2.0f)
				{
                    if (RandomWaypoints)
                    {
                        _waypointIndex = Random.Range(0, Waypoints.Length);
                    }
                    else
                    {
                        _waypointIndex += 1;
                        if (_waypointIndex >= Waypoints.Length)
                        {
                            _waypointIndex = 0;
                        }
                    }
                    _gridAgent.SetDestination(transform.position, Waypoints[_waypointIndex].transform.position);
				}
				else
				{
                    _gridAgent.StopMoving(false, false);
				}

				yield return null;
			}

			_patrolling = false;
		}

		public void OnDrawGizmos()
		{
            if (Waypoints.Length >= 1)
            {
                var startPosition = Waypoints[0].transform.position;
                var previousPosition = startPosition;

                foreach (var waypoint in Waypoints)
                {
                    Gizmos.DrawSphere(waypoint.transform.position, 0.3f);
                    Gizmos.DrawLine(previousPosition, waypoint.transform.position);
                    previousPosition = waypoint.transform.position;
                }

                Gizmos.DrawLine(previousPosition, startPosition);
            }
		}
    }
}