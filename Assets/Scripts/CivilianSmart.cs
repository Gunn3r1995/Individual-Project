using System;
using System.Collections;
using System.Linq;
using Assets.Scripts.AStar;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts
{
    public class CivilianSmart : MonoBehaviour {

        #region Variables
        [HideInInspector]
        public CivilianUtil CivilianUtil;

        private AudioSource _audioSource;

        private Sight _sight;
        public GameObject Player;
        public bool AutoTargetPlayer;

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
        public float EvadeRadius = 30f;

        private bool _evading;
        #endregion

        #region Voice
        public VoicesDatabase VoicesDatabase;
        #endregion

        #endregion

        [UsedImplicitly]
        private void Awake()
        {
            if (GetComponent<CivilianUtil>() == null) gameObject.AddComponent<CivilianUtil>();
            CivilianUtil = GetComponent<CivilianUtil>();
            _sight = GetComponent<Sight>();
            _grid = FindObjectOfType<AStar.Grid>();
            _gridAgent = GetComponent<GridAgent>();
            _audioSource = GetComponent<AudioSource>();
        }

        [UsedImplicitly]
        private void Start () {
            if (AutoTargetPlayer)
                Player = GameObject.FindGameObjectsWithTag("Player").Last();

            if (RandomWaypoints)
                _waypointIndex = Random.Range(0, Waypoints.Length);

            CivilianUtil.state = CivilianUtil.State.Wander;
        }

        [UsedImplicitly]
        private void Update()
        {
            //print("Can See Player: " + CivilianUtil.CanSeePlayer(_fov));
            Fsm();
        }

        private void Fsm()
        {
            switch (CivilianUtil.state)
            {
                case CivilianUtil.State.Wander:
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
            print("Patrol");
            _patrolling = true;

            // Goto first waypoint
            _gridAgent.Speed = PatrolSpeed;
            _gridAgent.RequestSetDestination(transform.position, Waypoints[_waypointIndex].transform.position, true);

            while (CivilianUtil.state == CivilianUtil.State.Wander)
            {
                // Ensure has reached current waypoints destination
                if (_gridAgent.HasPathFinished)
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
                    _gridAgent.RequestSetDestination(transform.position, Waypoints[_waypointIndex].transform.position, true);

                    // Stop the guard for PatrolWaitTime amount of time
                    _gridAgent.StopMoving();
                    yield return StartCoroutine(LookForPlayer(PatrolWaitTime));

                    // Continue patrolling at PatrolSpeed
                    _gridAgent.Speed = PatrolSpeed;
                }

                yield return null;
            }

            print("Finished Patrolling");
            _patrolling = false;
        }

        private IEnumerator Evade()
        {
            print("Evade");
            _evading = true;
            var lastPos = Player.transform.position;
            var randomTime = Random.Range(5f, 15f);
            var speakTimer = 0f;

            // Generate first waypoint
            var targetPosition = CivilianUtil.CreateRandomWalkablePosition(this.transform.position, EvadeRadius, _grid);

            // Go to first waypoint
            _gridAgent.Speed = EvadeSpeed;
            _gridAgent.RequestSetDestination(this.transform.position, targetPosition, true);

            yield return null;

            AttemptPlayRandomStateSound(false);

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
                        _gridAgent.RequestSetDestination(transform.position, targetPosition, true);
                    }
                    else
                    {
                        CivilianUtil.state = CivilianUtil.State.Wander;
                        break;
                    }
                }

                if (speakTimer >= randomTime)
                {
                    // Speak
                    AttemptPlayRandomStateSound(false);

                    speakTimer = 0;
                    randomTime = Random.Range(5f, 15f);
                }

                // Tell Any Guards
                if (CivilianUtil.CanSeeGuard(_sight))
                {
                    var alreadySpoken = false;
                    foreach (var guard in _sight.VisibleGuards)
                    {
                        if (guard.GetComponent<GuardUtil>().state == GuardUtil.State.Investigate) continue;

                        // Ask For Help
                        if (!alreadySpoken)
                        {
                            if (VoicesDatabase != null)
                            {
                                var helpClip = VoicesDatabase.GetRandomCivilianEvadeGuardSightedClip();
                                if (helpClip != null && !_audioSource.isPlaying)
                                    _audioSource.PlayOneShot(helpClip);
                            }
                            alreadySpoken = true;
                        }

                        guard.GetComponent<GuardUtil>().state = GuardUtil.State.Investigate;
                        guard.GetComponent<GuardTrained>().LastPos = lastPos;
                        guard.GetComponent<GuardTrained>().Disabled = false;
                    }
                }

                yield return null;
            }

            print("Finished Evading");
            _evading = false;
        }

        private void AttemptPlayRandomStateSound(bool guardSighted)
        {
            if (VoicesDatabase == null) return;
            if (_audioSource == null) return;
            if (_audioSource.isPlaying) return;

            switch (CivilianUtil.state)
            {
                case CivilianUtil.State.Wander:
                    break;
                case CivilianUtil.State.Evade:
                    if (!guardSighted)
                    {
                        var evadeAudioClip = VoicesDatabase.GetRandomCivilianEvadeClip();
                        if (evadeAudioClip != null && !_audioSource.isPlaying)
                            _audioSource.PlayOneShot(evadeAudioClip);
                    }
                    else
                    {
                        var evadeGuardSightedClip = VoicesDatabase.GetRandomCivilianEvadeGuardSightedClip();
                        if (evadeGuardSightedClip != null && !_audioSource.isPlaying)
                            _audioSource.PlayOneShot(evadeGuardSightedClip);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// LookForPlayer:
        /// LookForPlayer is a method which adds the functionality of new WaitForSeconds, but with improved SpotPlayer();
        /// method call to ensure that the civilian can see the player while the civilian is waiting.
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

            if (RandomWaypoints)
                CivilianUtil.DrawWaypointSphereGizmos(Waypoints);
            else
                CivilianUtil.DrawWaypointGizmos(Waypoints);
        }
    }
}

