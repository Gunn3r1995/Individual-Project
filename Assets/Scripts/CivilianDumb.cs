using System.Collections;
using System.Linq;
using Assets.Scripts.AStar;
using JetBrains.Annotations;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;
using Grid = Assets.Scripts.AStar.Grid;

namespace Assets.Scripts
{
    public class CivilianDumb : MonoBehaviour {

        #region Variables
        [HideInInspector]
        public CivilianUtil CivilianUtil;

        private FieldOfView _fov;
        public GameObject Player;
        public bool AutoTargetPlayer;

        private ThirdPersonCharacter _character;
        private Animator _animator;

        private Grid _grid;
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
            _fov = GetComponent<FieldOfView>();
            _grid = FindObjectOfType<Grid>();
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
                    CivilianUtil.SpotPlayer(_fov, ref _playerVisibleTimer, TimeToSpotPlayer);
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
            var lastPos = Player.transform.position;

            // Generate first waypoint
            var targetPosition = CivilianUtil.CreateRandomWalkablePosition(transform.position, EvadeRadius, _grid);

            // Go to first waypoint
            _gridAgent.Speed = EvadeSpeed;
            _gridAgent.RequestSetDestination(transform.position, targetPosition);

            while (CivilianUtil.state == CivilianUtil.State.Evade)
            {
                // Run Away
                if (_gridAgent.HasPathFinished)
                {
                    if (CivilianUtil.CanSeePlayer(_fov))
                    {
                        // Generate waypoint
                        targetPosition = CivilianUtil.CreateRandomWalkablePosition(transform.position, EvadeRadius, _grid);

                        // Go to first waypoint
                        _gridAgent.Speed = EvadeSpeed;
                        _gridAgent.RequestSetDestination(transform.position, targetPosition);
                    }
                    else
                    {
                        CivilianUtil.state = CivilianUtil.State.Patrol;
                        yield break;
                    }
                }

                // Tell Any Guards
                if (CivilianUtil.CanSeeGuard(_fov))
                {
                    print("GUARD SEEN");
                    foreach (var guard in _fov.VisibleGuards)
                    {
                        print("LAST POSITION:" + lastPos);
                        guard.GetComponent<GuardUtil>().state = GuardUtil.State.Investigate;
                        guard.GetComponent<GuardTrained>().LastPos = lastPos;
                        guard.GetComponent<GuardTrained>().Disabled = false;
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
                CivilianUtil.SpotPlayer(_fov, ref _playerVisibleTimer, TimeToSpotPlayer);
                timer += Time.deltaTime;
                yield return null;
            }
        }

        public void OnDrawGizmos()
        {
            if (Waypoints != null)
                CivilianUtil.DrawWaypointSphereGizmos(Waypoints);
        }
    }
}

