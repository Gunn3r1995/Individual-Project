using System.Collections;
using System.Linq;
using Assets.Scripts.AStar;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;

namespace Assets.Scripts
{
    public class Guard : MonoBehaviour
    {
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
        public float StoppingDistance = 2.0f;

		private int _waypointIndex;
		private bool _patrolling;
        #endregion

        #region Alert
        public Transform AlertGroup01;
        public Transform AlertGroup02;
        public Transform AlertGroup03;

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
		private bool _chasing;
        #endregion

        #endregion

        float timer = 0.0f;

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

            SetAlertInactive();
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
            switch (state)
            {
                case State.Patrol:
                    SpotPlayer();
                    if(!_patrolling)
                        StartCoroutine(Patrol());
                    break;
                case State.Alert:
                    if (!_alerted)
                        StartCoroutine("Alert");
                    break;
                case State.Investigate:
                    if (!_investigating)
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
            _patrolling = true;
            print("Patrolling");
            //SetAlertInactive();

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
            _gridAgent.StopMoving();

            if (CanSeePlayer())
				AlertSpot = Player.transform.position;
            
            transform.LookAt(AlertSpot);

            yield return new WaitForSeconds(AlertReactionTime);

            _gridAgent.SetSpeed(InvestigateSpeed);
            _gridAgent.SetDestination(transform.position, AlertSpot);
            

           
 
            while (state == State.Alert) {
				if (CanSeePlayer())
					state = State.Chase;
                
                if (_gridAgent.HasPathFinished())
				{
                    //_gridAgent.StopMoving();
                    //yield return StartCoroutine(SearchForPlayer(InvestigateSpotTime));
				    state = State.Investigate;
				}
				yield return null;
            }
            _alerted = false;
        }

        private IEnumerator Investigate(){
            print("Investigating");
            _investigating = true;

            var lastPos = new Vector3(0, 0, 0);
            Vector3 targetPosition = CreateRandomWalkablePosition(AlertSpot, WanderRadius, ref lastPos);

            _gridAgent.SetSpeed(InvestigateSpeed);
            _gridAgent.SetDestination(transform.position, targetPosition);
            
            while(state == State.Investigate){
				timer += Time.deltaTime;

				if (CanSeePlayer())
					state = State.Chase;

                if (_gridAgent.HasPathFinished())
                {
                    print("Investigate path found");
                    targetPosition = CreateRandomWalkablePosition(AlertSpot, WanderRadius, ref lastPos);

                    _gridAgent.SetDestination(transform.position, targetPosition);

                    _gridAgent.StopMoving();
                    yield return StartCoroutine(SearchForPlayer(InvestigateSpotTime));
                    timer += InvestigateSpotTime;

                    print("Investigate path started");
                    _gridAgent.SetSpeed(InvestigateSpeed);
                }

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

                if (CanSeePlayer())
                {
                    transform.LookAt(Player.transform);
                    _gridAgent.SetDestination(transform.position, Player.transform.position);

                    if (Vector3.Distance(transform.position, Player.transform.position) >= 2.0f)
                    {
                        if (OnGuardCaughtPlayer != null)
                        {
                            OnGuardCaughtPlayer();
                        }
                    }
                }

                yield return null;
            }
            _chasing = false;
        }

        private void SpotPlayer() {
            if(_FOV.VisibleTargets.Count > 0) {
                playerVisibleTimer += Time.deltaTime;

			} else  {
                playerVisibleTimer -= Time.deltaTime;
            }

			playerVisibleTimer = Mathf.Clamp(playerVisibleTimer, 0, timeToSpotPlayer);

            foreach(Transform child in AlertGroup01) {
                child.GetComponent<Renderer>().material.color = Color.Lerp(Color.white, Color.red, playerVisibleTimer / timeToSpotPlayer);
            }

			foreach (Transform child in AlertGroup02)
			{
				child.GetComponent<Renderer>().material.color = Color.Lerp(Color.white, Color.red, playerVisibleTimer / timeToSpotPlayer);
			}
			foreach (Transform child in AlertGroup03)
			{
				child.GetComponent<Renderer>().material.color = Color.Lerp(Color.white, Color.red, playerVisibleTimer / timeToSpotPlayer);
			}

            if(playerVisibleTimer >=  timeToSpotPlayer/3) {
                AlertGroup01.gameObject.SetActive(true);
                AlertGroup01.gameObject.SetActive(true);
                
            } else { AlertGroup01.gameObject.SetActive(false);}

            if  ( playerVisibleTimer >= (timeToSpotPlayer / 3) * 2) {
                AlertGroup02.gameObject.SetActive(true);

            } else { AlertGroup02.gameObject.SetActive(false); }

            if (playerVisibleTimer >= timeToSpotPlayer) {
                AlertGroup03.gameObject.SetActive(true);
				state = State.Alert;
            } else { AlertGroup03.gameObject.SetActive(false); }
        }

		bool CanSeePlayer()
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
			timer = 0;

			while (timer <= waitTime)
			{
                SpotPlayer();
				timer += Time.deltaTime;
			    yield return null;
            }
        }

        private IEnumerator SearchForPlayer(float waitTime) {
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

        private void SetAlertInactive() {
            AlertGroup01.gameObject.SetActive(false);
            AlertGroup02.gameObject.SetActive(false);
            AlertGroup03.gameObject.SetActive(false);
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