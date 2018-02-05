using System.Collections;
using Assets.Scripts.AStar;
using UnityEngine;

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
            _FOV = FindObjectOfType<FieldOfView>();
            _audioSource = FindObjectOfType<AudioSource>();
            _grid = FindObjectOfType<AStar.Grid>();
            _gridAgent = GetComponent<GridAgent>();

            if (AutoTargetPlayer)
                Player = GameObject.FindGameObjectsWithTag("Player")[0];

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

            SetAlertInactive();

			while (state == State.Patrol)
			{
                _gridAgent.SetSpeed(PatrolSpeed);

                if (Vector3.Distance(transform.position, Waypoints[_waypointIndex].transform.position) <= 2.0f)
                {
                    _gridAgent.StopMoving();
                    yield return StartCoroutine(LookForPlayer(PatrolWaitTime));
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
                    yield return StartCoroutine(SearchForPlayer(InvestigateSpotTime));
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
                    yield return StartCoroutine(SearchForPlayer(InvestigateSpotTime));
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

        void SpotPlayer() {

            if(_FOV.visibleTargets.Count > 0) {
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
            return _FOV.visibleTargets.Count > 0;
        }

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
    }
}