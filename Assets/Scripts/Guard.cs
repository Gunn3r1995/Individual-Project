using System.Collections;
using Assets.Scripts.AStar;
using UnityEngine;

namespace Assets.Scripts
{
    public class Guard : MonoBehaviour
    {
        
        public GameObject Player;
        public bool AutoTargetPlayer;
        public enum State { Patrol, Alert, Investigate, Chase }
        public State state;

        private AStar.Grid _grid;
		private GridAgent _gridAgent;

		// Sight
		public float heightMultiplier = 1.36f;
		public float sightDistance = 10f;
		public LayerMask viewMask;
		public float timeToSpotPlayer = 0.5f;
		public Light spotlight;

		private float playerVisibleTimer;
		private Color originalSpotlightColour;
		private float viewAngle;

		// Patrolling
		public GameObject[] Waypoints;
		public bool RandomWaypoints;
		public float PatrolSpeed = 0.75f;

		private int _waypointIndex;
		private bool _patrolling;

        // Alerted
        public float AlertReactionTime = 1.0f;

		private Vector3 AlertSpot;
		private bool _alerted;

        // Investigating
        public float InvestigateSpeed = 1.0f;
        public float InvestigateTime = 60.0f;
        public float WanderRadius = 10.0f;

		private bool _investigating;

        // Chasing
        public float ChaseSpeed = 2.0f;
		private bool _chasing;

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

                if (HasReachedDestintion(transform.position, Waypoints[_waypointIndex].transform.position))
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

            _gridAgent.SetSpeed(0);
            _gridAgent.StopMoving();

            yield return StartCoroutine(Wait(AlertReactionTime));

            while(state == State.Alert){
                if(CanSeePlayer()) {
                    state = State.Chase;
                } else {
                    state = State.Investigate;
                }

                yield return null;
            }
            _alerted = false;
        }

        private IEnumerator Investigate(){
            print("Investigating");
            _investigating = true;

            var posLagTimer = 3.0f;

            var alertSpotReached = false;
            var timer = 0.0f;
            var posLag = 0.0f;

            Vector3 targetPosition = RandomNavSphere(AlertSpot, WanderRadius);

            while(state == State.Investigate){
                if (!alertSpotReached)
                {
                    if (HasReachedDestintion(transform.position, AlertSpot))
                    {
                        posLag += Time.deltaTime;
                        _gridAgent.StopMoving();
						if (posLag >= posLagTimer)
						{
                            _gridAgent.SetSpeed(InvestigateSpeed);
							alertSpotReached = true;
                            posLag = 0.0f;
						}
                    }
                    else
                    {
                        _gridAgent.SetSpeed(InvestigateSpeed);
                        _gridAgent.SetDestination(transform.position, AlertSpot);
                    }
                } else {
					timer += Time.deltaTime;

                    if(HasReachedDestintion(transform.position, targetPosition)) {
						transform.LookAt(targetPosition);
						posLag += Time.deltaTime;
						_gridAgent.StopMoving();

						if (posLag >= posLagTimer)
						{
							targetPosition = RandomNavSphere(AlertSpot, WanderRadius);
							posLag = 0.0f;
						}
                    } else {
                        _gridAgent.SetSpeed(InvestigateSpeed);
                        _gridAgent.SetDestination(transform.position, targetPosition);
                    }

                    if (timer >= InvestigateTime){
                        print("Times Up");
                        state = State.Patrol;
                    }
                }

				if (CanSeePlayer())
					state = State.Chase;

                yield return null;
            }
            _investigating = false;
        }

		private bool HasReachedDestintion(Vector3 current, Vector3 target)
		{
			if (_grid != null)
			{
				Node currentNode = _grid.GetNodeFromWorldPoint(current);
				Node targetNode = _grid.GetNodeFromWorldPoint(target);

                if (currentNode.WorldPosition == targetNode.WorldPosition)
                {
                    return true;
                }
			}
			return false;
		}

        private Vector3 RandomNavSphere(Vector3 origin, float dist)
		{
            if(_grid != null){
				//Create Nav Sphere where position is walkable.
                Vector3 randDirection = Random.insideUnitSphere * dist;
                Vector3 newPos = origin += randDirection;
                newPos.y = 0;

                Node node = _grid.GetNodeFromWorldPoint(newPos);
                while(HasReachedDestintion(transform.position, newPos)){
					randDirection = Random.insideUnitSphere * dist;
					newPos = origin += randDirection;
					newPos.y = 0;
                }

                if(!node.walkable) {
                    while (true) {
                        randDirection = Random.insideUnitSphere * dist;
                        newPos = origin += randDirection;
						newPos.y = 0;

                        node = _grid.GetNodeFromWorldPoint(newPos);
                        if(node.walkable && !HasReachedDestintion(transform.position, newPos)) {
                            return newPos;
                        }
                    }
                }
                return newPos;
            } else {
                return new Vector3();
            }
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

		IEnumerator Wait(float waitTime)
		{
			yield return new WaitForSeconds(waitTime);
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