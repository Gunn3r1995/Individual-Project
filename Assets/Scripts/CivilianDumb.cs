using System.Collections;
using System.Linq;
using Assets.Scripts.AStar;
using JetBrains.Annotations;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;

namespace Assets.Scripts
{
    public class CivilianDumb : MonoBehaviour {

        #region Variables
        [HideInInspector]
        public CivilianUtil CivilianUtil;

        private Sight _sight;
        public GameObject Player;
        public bool AutoTargetPlayer;

        private ThirdPersonCharacter _character;
        private Animator _animator;

        private AStar.Grid _grid;
        private GridAgent _gridAgent;

        #region Sight
        public float TimeToSpotPlayer = 0.5f;
        private float _playerVisibleTimer;
        #endregion

        #region Patrol
        public GameObject[] Waypoints;
        public bool RandomWaypoints;
        public float PatrolSpeed = 0.75f;
        public float PatrolWaitTime = 0.5f;

        private int _waypointIndex;
        private bool _patrolling;
        #endregion

        #region Evade
        public float EvadeSpeed = 1.5f;
        public float EvadeWaitTime = 3.0f;
        public float EvadeRadius = 30f;

        private bool _evading;
        #endregion

        #endregion

        [UsedImplicitly]
        private void Awake()
        {
            if (GetComponent<CivilianUtil>() == null) gameObject.AddComponent<CivilianUtil>();
            CivilianUtil = GetComponent<CivilianUtil>();
            _character = GetComponent<ThirdPersonCharacter>();
            _animator = GetComponent<Animator>();
            _sight = GetComponent<Sight>();
            _grid = FindObjectOfType<AStar.Grid>();
            _gridAgent = GetComponent<GridAgent>();
        }

        [UsedImplicitly]
        private void Start () {
            if (AutoTargetPlayer)
                Player = GameObject.FindGameObjectsWithTag("Player").Last();

            if (RandomWaypoints)
                _waypointIndex = Random.Range(0, Waypoints.Length);

            CivilianUtil.state = CivilianUtil.State.Patrol;
        }

        [UsedImplicitly]
        private void Update()
        {
            Fsm();
        }

        private void Fsm()
        {
            switch (CivilianUtil.state)
            {
                case CivilianUtil.State.Patrol:
                    CivilianUtil.SpotPlayer(_sight, ref _playerVisibleTimer, TimeToSpotPlayer);
                    if (!_patrolling)
                        StartCoroutine(Patrol());
                    break;
                case CivilianUtil.State.Evade:
                    if (!_evading)
                        StartCoroutine(Evade());
                    break;
            }
        }

        private IEnumerator Patrol()
        {
            _patrolling = true;

            while (CivilianUtil.state == CivilianUtil.State.Patrol)
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

                    // Stop walking animation
                    _animator.SetFloat("Forward", 0);

                    // Stop the civilian for PatrolWaitTime amount of time
                    yield return StartCoroutine(LookForPlayer(PatrolWaitTime));
                }
                yield return null;
            }

            _patrolling = false;
        }

        private IEnumerator Evade()
        {
            _evading = true;
            var speakTimer = 0f;

            var pos = transform.position;
            // Generate first waypoint
            var targetPosition = CivilianUtil.CreateRandomWalkablePosition(pos, EvadeRadius, _grid);

            // Go to first waypoint
            _gridAgent.Speed = EvadeSpeed;
            _gridAgent.RequestSetDestination(transform.position, targetPosition, false);

            while (CivilianUtil.state == CivilianUtil.State.Evade)
            {
                speakTimer += Time.deltaTime;

                // Run Away
                if (_gridAgent.HasPathFinished)
                {
                    if (CivilianUtil.CanSeePlayer(_sight))
                    {
                        // Generate waypoint
                        targetPosition = CivilianUtil.CreateRandomWalkablePosition(transform.position, EvadeRadius, _grid);

                        // Go to first waypoint
                        _gridAgent.Speed = EvadeSpeed;
                        _gridAgent.RequestSetDestination(transform.position, targetPosition, false);
                    }
                    else
                    {
                        CivilianUtil.state = CivilianUtil.State.Patrol;
                        yield break;
                    }
                }

                yield return null;
            }

            _evading = false;
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
            var timer = 0f;

            while (timer <= waitTime)
            {
                CivilianUtil.SpotPlayer(_sight, ref _playerVisibleTimer, TimeToSpotPlayer);
                timer += Time.deltaTime;
                yield return null;
            }
        }

        public void OnDrawGizmos()
        {
            if (Waypoints == null) return;

            if(RandomWaypoints)
                CivilianUtil.DrawWaypointSphereGizmos(Waypoints);
            else
                CivilianUtil.DrawWaypointGizmos(Waypoints);
            GuardUtil.DrawNextWaypointLineGizmos(transform.position, Waypoints, _waypointIndex);
        }
    }
}

