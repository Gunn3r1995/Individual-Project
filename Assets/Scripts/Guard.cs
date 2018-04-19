using System.Collections;
using System.Linq;
using Assets.Scripts.AStar;
using JetBrains.Annotations;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;

namespace Assets.Scripts
{
    public class Guard : MonoBehaviour
    {
        #region Variables
        [HideInInspector]
        public GuardUtil GuardUtil;

        private Sight _sight;
        public GameObject Player;
        public bool AutoTargetPlayer;

		private ThirdPersonCharacter _character;
        private Animator _animator;

        private AStar.Grid _grid;
		private GridAgent _gridAgent;

        #region Patrol
        public GameObject[] Waypoints;
		public bool RandomWaypoints;
		public float PatrolSpeed = 0.75f;
        public float PatrolWaitTime = 3.0f;

		private int _waypointIndex;
		private bool _patrolling;
        #endregion

		#region Chase
		public float ChaseSpeed = 2.0f;
        public float ChaseTime = 20.0f;
		private bool _chasing;
		#endregion

		#endregion

		[UsedImplicitly]
		private void Awake()
		{
            if(GetComponent<GuardUtil>() == null) gameObject.AddComponent<GuardUtil>();
		    GuardUtil = GetComponent<GuardUtil>();
            _character = GetComponent<ThirdPersonCharacter>();
            _animator = GetComponent<Animator>();
		    _sight = GetComponent<Sight>();
		    _grid = FindObjectOfType<AStar.Grid>();
		    _gridAgent = GetComponent<GridAgent>();
        }

        [UsedImplicitly]
        private void Start()
		{
            if (AutoTargetPlayer)
                Player = GameObject.FindGameObjectsWithTag("Player").Last();

            if (RandomWaypoints)
                _waypointIndex = Random.Range(0, Waypoints.Length);

            GuardUtil.state = GuardUtil.State.Patrol;
        }

        [UsedImplicitly]
        private void Update()
        {
            Fsm();
        }

		private void Fsm()
		{
            switch (GuardUtil.state)
            {
                case GuardUtil.State.Patrol:
                    if (!_patrolling)
                        StartCoroutine(Patrol());
                    break;
                case GuardUtil.State.Chase:
                    if (!_chasing)
                        StartCoroutine(Chase());
                    break;
                default:
                    GuardUtil.state = GuardUtil.State.Patrol;
                    break;
            }
        }

        private IEnumerator Patrol()
        {
            _patrolling = true;
            print("Patrolling");

            while (GuardUtil.state == GuardUtil.State.Patrol)
		    {
                // Walk straight to the next waypoint
                transform.position = Vector3.MoveTowards(transform.position, Waypoints[_waypointIndex].transform.position, PatrolSpeed * Time.deltaTime);
                _character.Move((Waypoints[_waypointIndex].transform.position - transform.position).normalized * PatrolSpeed, false, false);

                // Ensure has reached current waypoints destination
                if (Vector3.Distance(transform.position, Waypoints[_waypointIndex].transform.position) <= 0.1f)
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
                }

                if(GuardUtil.CanSeePlayer(_sight)) {
                    GuardUtil.state = GuardUtil.State.Chase;
                    break;
                }

		        yield return null;
		    }

            _patrolling = false;
        }

        private IEnumerator Chase()
        {
            print("Chase");
            _chasing = true;

            var timer = 0f;

            transform.LookAt(Player.transform.position);
            _gridAgent.Speed = ChaseSpeed;

            while (GuardUtil.state == GuardUtil.State.Chase)
            {
                timer += Time.deltaTime;

                _gridAgent.StraightToDestination(Player.transform.position);

                if (Vector3.Distance(transform.position, Player.transform.position) <= 1.0f)
                {
					_gridAgent.StopMoving();
                    GuardUtil.GuardOnCaughtPlayer();
                }

                if(timer >= ChaseTime)
                {
                    GuardUtil.state = GuardUtil.State.Patrol;
                    break;
                }

                yield return null;
            }
            _chasing = false;
        }

		public void OnDrawGizmos()
		{
		    GuardUtil.DrawWaypointGizmos(Waypoints);
		    GuardUtil.DrawNextWaypointLineGizmos(transform.position, Waypoints, _waypointIndex);
		}

    }
}