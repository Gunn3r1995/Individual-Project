using System.Collections;
using Assets.Scripts.AStar;
using UnityEngine;

namespace Assets.Scripts
{
    public class Guard : MonoBehaviour
    {
        #region Variables
        public GameObject Player;
        public bool AutoTargetPlayer;
        public enum State { Patrol, Alert, Investigate, Chase }
        public State state;

        private AStar.Grid _grid;
		private GridAgent _gridAgent;

		#region Sight
		public float heightMultiplier = 1.36f;
		public float sightDistance = 10f;
		public LayerMask viewMask;
		public float timeToSpotPlayer = 0.5f;
		public Light spotlight;

		private float playerVisibleTimer;
		private Color originalSpotlightColour;
		private float viewAngle;
        #endregion

        #region Patrol
        public GameObject[] Waypoints;
		public bool RandomWaypoints;
		public float PatrolSpeed = 0.75f;

		private int _waypointIndex;
		private bool _patrolling;
		#endregion

		#region Alert
		public float AlertReactionTime = 1.0f;

		private Vector3 AlertSpot;
		private bool _alerted;
		#endregion

		#region Investigate
		public float InvestigateSpeed = 1.0f;
		public float InvestigateTime = 60.0f;
        public float InvestigateSpotTime = 5.0f;
		public float WanderRadius = 5.0f;

		private bool _investigating;
		#endregion

		#region Chase
		public float ChaseSpeed = 2.0f;
		private bool _chasing;
        #endregion

        #endregion

        float timer = 0.0f;

		void Start()
        {
            _grid = FindObjectOfType<AStar.Grid>();
            _gridAgent = GetComponent<GridAgent>();

            if (AutoTargetPlayer)
                Player = GameObject.FindGameObjectsWithTag("Player")[0];

            if (RandomWaypoints)
                _waypointIndex = Random.Range(0, Waypoints.Length);

			state = State.Patrol;
			viewAngle = spotlight.spotAngle;
			originalSpotlightColour = spotlight.color;
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
					SpotPlayer();
					if (!_patrolling)
                        StartCoroutine("Patrol");
					break;
				case State.Alert:
                    if(!_alerted)
                        StartCoroutine("Alert");
					break;
				case State.Investigate:
                    if(!_investigating)
                        StartCoroutine("Investigate");
					break;
				case State.Chase:
                    if (!_chasing)
                        StartCoroutine("Chase");
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

                if (Vector3.Distance(transform.position, Waypoints[_waypointIndex].transform.position) <= 2.0f)
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
                }
                _gridAgent.SetDestination(transform.position, Waypoints[_waypointIndex].transform.position);
				yield return null;
			}
			_patrolling = false;
		}

        private IEnumerator Alert(){
            print("Alerted");
            _alerted = true;

			if (CanSeePlayer())
				AlertSpot = Player.transform.position;
            
            transform.LookAt(AlertSpot);
            _gridAgent.StopMoving();

            yield return new WaitForSeconds(AlertReactionTime);

            while(state == State.Alert) {
				if (CanSeePlayer())
					state = State.Chase;
                
                _gridAgent.StopMoving();
                if (Vector3.Distance(transform.position, AlertSpot) <= 2.0f)
				{
                    yield return StartCoroutine(LookForPlayer(InvestigateSpotTime));
                    state = State.Investigate;
                    break;
                }

				_gridAgent.SetSpeed(InvestigateSpeed);
				_gridAgent.SetDestination(transform.position, AlertSpot);

				yield return null;
            }
            _alerted = false;
        }

        private IEnumerator Investigate(){
            print("Investigating");
            _investigating = true;

            var lastPos = new Vector3(0, 0, 0);

            Vector3 targetPosition = CreateRandomWalkablePosition(AlertSpot, WanderRadius, ref lastPos);

            while(state == State.Investigate){
				timer += Time.deltaTime;

				if (CanSeePlayer())
					state = State.Chase;
                
                _gridAgent.StopMoving();
                if (Vector3.Distance(transform.position, targetPosition) <= 2.0f) {
                    yield return StartCoroutine(LookForPlayer(InvestigateSpotTime));
                    yield return new WaitForSeconds(InvestigateSpotTime);
                    timer += InvestigateSpotTime;
                    targetPosition = CreateRandomWalkablePosition(AlertSpot, WanderRadius, ref lastPos);	
                }

				_gridAgent.SetSpeed(InvestigateSpeed);
				_gridAgent.SetDestination(transform.position, targetPosition);

                if (timer >= InvestigateTime){
                    state = State.Patrol;
                    break;
                }

                yield return null;
            }
            _investigating = false;
        }

		private IEnumerator Chase()
		{
            print("Chasing");
            _chasing = true;

            while (state == State.Chase)
			{
                _gridAgent.SetSpeed(ChaseSpeed);
                _gridAgent.SetDestination(transform.position, Player.transform.position);

				yield return null;
			}
            _chasing = false;
		}

		void SpotPlayer()
		{
            if (CanSeePlayer())
            {
                playerVisibleTimer += Time.deltaTime;
            }
			else
				playerVisibleTimer -= Time.deltaTime;

			playerVisibleTimer = Mathf.Clamp(playerVisibleTimer, 0, timeToSpotPlayer);
			spotlight.color = Color.Lerp(originalSpotlightColour, Color.red, playerVisibleTimer / timeToSpotPlayer);

            if (playerVisibleTimer >= timeToSpotPlayer)
			{
                state = State.Alert;
			}
		}

		bool CanSeePlayer()
		{
			if (Vector3.Distance(transform.position, Player.transform.position) < sightDistance)
			{
                Vector3 dirToPlayer = (Player.transform.position - transform.position).normalized;
				float angleBetweenGuardAndPlayer = Vector3.Angle(transform.forward, dirToPlayer);
				if (angleBetweenGuardAndPlayer < viewAngle / 2f)
				{
                    if (!Physics.Linecast(transform.position, Player.transform.position, viewMask))
					{
						return true;
					}
				}
			}
			return false;
		}

        private IEnumerator LookForPlayer(float waitTime) {
            timer = 0;

            while(timer <= waitTime) {
				if (CanSeePlayer())
					state = State.Chase;

				timer += Time.deltaTime;
                yield return null;
            }
        }

		private Vector3 CreateRandomWalkablePosition(Vector3 origin, float dist, ref Vector3 lastPos, AStar.Grid grid = null)
		{
			if (grid == null)
			{
				grid = FindObjectOfType<AStar.Grid>();
			}

			if (grid != null)
			{
				Vector3 randomPosition = CreateRandomPosition(origin, dist);

				Node newPositionNode = grid.GetNodeFromWorldPoint(randomPosition);
                Node lastPositionNode = grid.GetNodeFromWorldPoint(lastPos);
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
			Vector3 randomPosition = Random.insideUnitSphere;
			randomPosition.y = 0;
			randomPosition.Normalize();
			randomPosition *= dist;

			return randomPosition + origin;
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

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + Vector3.up * heightMultiplier, transform.forward * sightDistance);
		}
    }
}