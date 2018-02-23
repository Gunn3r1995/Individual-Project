using System.Collections;
using System.Linq;
using Assets.Scripts.AStar;
using UnityEngine;

namespace Assets.Scripts
{
    public class Guard : MonoBehaviour
    {
        TESTING ERROR
        #region Variables
        public static event System.Action OnGuardCaughtPlayer;

        FieldOfView _FOV;
        private AudioSource _audioSource;
        public GameObject Player;
        public bool AutoTargetPlayer;
        public enum State { Patrol, Alert, Investigate, Chase }
        public State state;

        private AStar.Grid _grid;
		private GridAgent _gridAgent;

		#region Sight
		public float timeToSpotPlayer = 0.5f;
		private float playerVisibleTimer;
        #endregion

        #region Patrol
        public GameObject[] Waypoints;
		public bool RandomWaypoints;
		public float PatrolSpeed = 0.75f;
        public float PatrolWaitTime = 3.0f;

		private int _waypointIndex;
		private bool _patrolling;
        #endregion

        #region Alert
        //public Transform AlertGroup01;
        //public Transform AlertGroup02;
        //public Transform AlertGroup03;

		public float AlertReactionTime = 2.0f;

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
        public float ChaseTime = 20.0f;
		private bool _chasing;
        #endregion

        #endregion

		void Start()
        {
            _FOV = GetComponent<FieldOfView>();
            _audioSource = FindObjectOfType<AudioSource>();
            _grid = FindObjectOfType<AStar.Grid>();
            _gridAgent = GetComponent<GridAgent>();

            if (AutoTargetPlayer)
                Player = GameObject.FindGameObjectsWithTag("Player").Last();

            if (RandomWaypoints)
                _waypointIndex = Random.Range(0, Waypoints.Length);

            state = State.Patrol;
        }

        void Update()
        {
            // TODO FIX ME
			//AudioClip clip = Resources.Load<AudioClip>("Voices/huh_01");
			//_audioSource.PlayOneShot(clip);

            FSM();
        }

		private void FSM()
		{
            TEST = print("YUMMY")
            switch (state)
            {
                case State.Patrol:
                    SpotPlayer();
                    if(!_patrolling)
                        StartCoroutine(Patrol());
                    break;
                case State.Alert:
                    if (!_alerted)
                        StartCoroutine(Alert());
                    break;
                case State.Investigate:
                    if (!_investigating)
                        StartCoroutine(Investigate());
                    break;
                case State.Chase:
                    if (!_chasing)
                        StartCoroutine(Chase());
                    break;
            }
        }

        private IEnumerator Patrol()
        {
            _patrolling = true;
            print("Patrolling");

            // Goto first waypoint
            _gridAgent.SetSpeed(PatrolSpeed);
            _gridAgent.SetDestination(transform.position, Waypoints[0].transform.position);

		    while (state == State.Patrol)
		    {
                // Ensure has reached current waypoints destination
		        if (_gridAgent.HasPathFinished())
		        {
                    // Calculate next waypoint
                    if (RandomWaypoints)
		                _waypointIndex = Random.Range(0, Waypoints.Length);
		            else
		            {
		                _waypointIndex += 1;
		                if (_waypointIndex >= Waypoints.Length)
		                    _waypointIndex = 0;
		            }

                    // Setting the destination to next waypoint
                    _gridAgent.SetDestination(transform.position, Waypoints[_waypointIndex].transform.position);

                    // Stop the guard for PatrolWaitTime amount of time
		            _gridAgent.StopMoving();
		            yield return StartCoroutine(LookForPlayer(PatrolWaitTime));

                    // Continue patrolling at PatrolSpeed
                    _gridAgent.SetSpeed(PatrolSpeed);
                }
		        yield return null;
		    }
            _patrolling = false;
        }

        private IEnumerator Alert(){
            print("Alerted");
            _alerted = true;

            // Set alert spot to players location
            AlertSpot = Player.transform.position;

            // Set the destination to the alert spot
            transform.LookAt(AlertSpot);
			_gridAgent.SetDestination(transform.position, AlertSpot);

            // Stop moving and wait for 'AlertReactionTime' amount
            _gridAgent.StopMoving();
            yield return new WaitForSeconds(AlertReactionTime);

            while (state == State.Alert) {
				print("Alerted");
				_gridAgent.SetSpeed(InvestigateSpeed);

                if (_gridAgent.HasPathFinished())
                {
                    // On Alert Destination Reached

					// Work around to force stop the agent
                    _gridAgent.SetDestination(transform.position, CreateRandomWalkablePosition(transform.position, 5.0f, _grid));
					_gridAgent.StopMoving();

                    // Wait for 'InvestigateSpotTime' amount while looking for the player
					yield return StartCoroutine(SearchForPlayer(InvestigateSpotTime));
					state = State.Investigate;

					break;
                }

                // If can se player while alerted go straight to chase
                if (CanSeePlayer())
                    state = State.Chase;

               yield return null;
            }
            print("Finished Alert");
			_alerted = false;
        }

        private IEnumerator Investigate(){
            print("Investigating");
            _investigating = true;
			float timer = 0.0f;

            Vector3 lastPos = new Vector3(0, 0, 0);
            // Generate first waypoint and save the position
            Vector3 targetPosition = CreateRandomWalkablePosition(AlertSpot, WanderRadius, ref lastPos, _grid);

            // Go to first waypoint
            _gridAgent.SetSpeed(InvestigateSpeed);
            _gridAgent.SetDestination(transform.position, targetPosition);

			while(state == State.Investigate) {
                // Add to time
				timer += Time.deltaTime;

				// If can se player while investigating go straight to chase
				if (CanSeePlayer())
					state = State.Chase;

                if (_gridAgent.HasPathFinished())
                {
                    // Guard reached waypoint
                    // Create a new waypoint parsing in the last waypoint; by reference so that it keeps the 'lastPos' updated
                    targetPosition = CreateRandomWalkablePosition(AlertSpot, WanderRadius, ref lastPos);

                    // Set the destination and stop moving work around
                    _gridAgent.SetDestination(transform.position, targetPosition);
                    _gridAgent.StopMoving();

                    // Wait at waypoint for 'InvestigateSpotTime' amount
                    yield return StartCoroutine(SearchForPlayer(InvestigateSpotTime));
                    // Add the 'InvestigateSpotTime' amount to the timer
                    timer += InvestigateSpotTime;

                    // Start walking to next waypoint
                    _gridAgent.SetSpeed(InvestigateSpeed);
                }

                if (timer >= InvestigateTime){
                    // If investiage time reached go back to patrol
                    state = State.Patrol;
                    break;
                }

                yield return null;
            }
            print("Finished Investiagting");
            _investigating = false;
        }

        private IEnumerator Chase()
        {
            print("Chase");
            _chasing = true;

            var timer = 0f;

            Vector3 laspPos = Player.transform.position;
			transform.LookAt(laspPos);
			_gridAgent.SetDestination(transform.position, laspPos);
			_gridAgent.SetSpeed(ChaseSpeed);

            while (state == State.Chase)
            {
                timer += Time.deltaTime;

                if (CanSeePlayer())
                {
                    laspPos = Player.transform.position;
                    _gridAgent.StraightToDestination(Player.transform.position);
                } else {
                    _gridAgent.SetDestination(transform.position, laspPos);
                }

                if (Vector3.Distance(transform.position, Player.transform.position) <= 1.0f)
                {
                    if (OnGuardCaughtPlayer != null)
                    {
                        OnGuardCaughtPlayer();
                        _gridAgent.StopMoving();
                    }
                } else if (Vector3.Distance(transform.position, Player.transform.position) >= 20f) {
                    state = State.Investigate;
                    break;
                }

                if(timer >= ChaseTime) {
                    state = State.Investigate;
                    break;
                }

                yield return null;
            }
            _chasing = false;
        }

        private void SpotPlayer() {
            if (_FOV.VisibleTargets.Count > 0) playerVisibleTimer += Time.deltaTime;
            else playerVisibleTimer -= Time.deltaTime;

            playerVisibleTimer = Mathf.Clamp(playerVisibleTimer, 0, timeToSpotPlayer);

            if (playerVisibleTimer >= timeToSpotPlayer) state = State.Alert;
        }

		public bool CanSeePlayer()
		{
            return _FOV.VisibleTargets.Count > 0;
        }

		/// <summary>
        /// LookForPlayer:
        /// LookForPlayer is a method which adds the functionality of new WaitForSeconds, but with improved SpotPlayer();
        /// method call to ensure that the guard can see the player while the guard is waiting.
        /// </summary>
        /// <param name="waitTime"></param>
        /// <returns></returns>
        private IEnumerator LookForPlayer(float waitTime)
		{
            float timer = 0;

			while (timer <= waitTime)
			{
                SpotPlayer();
				timer += Time.deltaTime;
			    yield return null;
            }
        }

        private IEnumerator SearchForPlayer(float waitTime) {
            float timer = 0;

            while(timer <= waitTime) {
				if (CanSeePlayer())
					state = State.Chase;

				timer += Time.deltaTime;
                yield return null;
            }
        }

		private static Vector3 CreateRandomWalkablePosition(Vector3 origin, float dist, AStar.Grid grid = null)
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

		private static Vector3 CreateRandomWalkablePosition(Vector3 origin, float dist, ref Vector3 lastPos, AStar.Grid grid = null)
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
        }

        private bool DestinationReached(Vector3 currentPos, Vector3 targetPos)
        {
            var currentNode = _grid.GetNodeFromWorldPoint(currentPos);
            var targetNode = _grid.GetNodeFromWorldPoint(targetPos);

            if (currentNode != targetNode) return false;

            print("Position Reached");
            return true;
        }

    }
}