﻿using System.Collections;
 using System.Collections.Generic;
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

		#region Sight
		public float TimeToSpotPlayer = 0.5f;
		private float _playerVisibleTimer;
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
		public float AlertReactionTime = 2.0f;

		private Vector3 _alertSpot;
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
                    GuardUtil.SpotPlayer(_sight, ref _playerVisibleTimer, TimeToSpotPlayer);
                    if (!_patrolling)
                        StartCoroutine(Patrol());
                    break;
                case GuardUtil.State.Alert:
                    if (!_alerted)
                        StartCoroutine(Alert());
                    break;
                case GuardUtil.State.Investigate:
                    if (!_investigating)
                        StartCoroutine(Investigate());
                    break;
                case GuardUtil.State.Chase:
                    if (!_chasing)
                        StartCoroutine(Chase());
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

                    // Stop walking animation
                    _animator.SetFloat("Forward", 0);

                    // Stop the guard for PatrolWaitTime amount of time
		            yield return StartCoroutine(LookForPlayer(PatrolWaitTime));
                }
		        yield return null;
		    }

            _patrolling = false;
        }

        private IEnumerator Alert(){
            print("Alerted");
            _alerted = true;

            // Set alert spot to players location
            _alertSpot = Player.transform.position;

            // Set the destination to the alert spot
            transform.LookAt(_alertSpot);
            _gridAgent.StraightToDestination(_alertSpot);

            // Stop moving and wait for 'AlertReactionTime' amount
            _gridAgent.StopMoving();
            yield return new WaitForSeconds(AlertReactionTime);

            var timer = 0f;
            while (GuardUtil.state == GuardUtil.State.Alert) {
			    print("Alerted");
                timer += Time.deltaTime;
				_gridAgent.Speed = InvestigateSpeed;

                if (Vector3.Distance(transform.position, _alertSpot) <= 0.2f)
                {
                    // On Alert Destination Reached

					// stop the agent
					_gridAgent.StopMoving();

                    // Wait for 'InvestigateSpotTime' amount while looking for the player
					yield return StartCoroutine(SearchForPlayer(InvestigateSpotTime));
                    GuardUtil.state = GuardUtil.State.Investigate;

					break;
                }

                // If can se player while alerted go straight to chase
                if (GuardUtil.CanSeePlayer(_sight))
                {
                    GuardUtil.state = GuardUtil.State.Chase;
                    break;
                }

                if (timer >= 10f)
                {
                    GuardUtil.state = GuardUtil.State.Investigate;
                }

                yield return null;
            }
			_alerted = false;
        }

        private IEnumerator Investigate(){
            print("Investigating");
            _investigating = true;
			var timer = 0.0f;
            var lastPos = new Vector3(0, 0, 0);

            // Generate first waypoint and save the position
            var targetPosition = GuardUtil.CreateRandomWalkablePosition(_alertSpot, WanderRadius, ref lastPos, _grid);

            // Go to first waypoint
            _gridAgent.Speed = InvestigateSpeed;
            _gridAgent.RequestSetDestination(transform.position, targetPosition, false);

            while(GuardUtil.state == GuardUtil.State.Investigate) {
                // Add to time
				timer += Time.deltaTime;

				// If can se player while investigating go straight to chase
				if (GuardUtil.CanSeePlayer(_sight))
                    GuardUtil.state = GuardUtil.State.Chase;

                if (_gridAgent.HasPathFinished)
                {
                    // Guard reached waypoint
                    // Create a new waypoint parsing in the last waypoint; by reference so that it keeps the 'lastPos' updated
                    targetPosition = GuardUtil.CreateRandomWalkablePosition(_alertSpot, WanderRadius, ref lastPos);

                    // Set the destination and stop moving work around
                    _gridAgent.RequestSetDestination(transform.position, targetPosition, false);
                    _gridAgent.StopMoving();

                    // Wait at waypoint for 'InvestigateSpotTime' amount
                    yield return StartCoroutine(SearchForPlayer(InvestigateSpotTime));
                    // Add the 'InvestigateSpotTime' amount to the timer
                    timer += InvestigateSpotTime;

                    // Start walking to next waypoint
                    _gridAgent.Speed = InvestigateSpeed;
                }

                if (timer >= InvestigateTime){
                    // If investiage time reached go back to patrol
                    GuardUtil.state = GuardUtil.State.Patrol;
                    break;
                }

                yield return null;
            }

            _investigating = false;
        }

        private IEnumerator Chase()
        {
            print("Chase");
            _chasing = true;

            var timer = 0f;

            var laspPos = Player.transform.position;
			transform.LookAt(laspPos);
            _gridAgent.Speed = ChaseSpeed;

            while (GuardUtil.state == GuardUtil.State.Chase)
            {
                timer += Time.deltaTime;

                if (GuardUtil.CanSeePlayer(_sight))
                {
                    laspPos = Player.transform.position;
                    _gridAgent.StraightToDestination(Player.transform.position);
                }

                if (Vector3.Distance(transform.position, Player.transform.position) <= 1.0f)
                {
					_gridAgent.StopMoving();
                    GuardUtil.GuardOnCaughtPlayer();
                }

                if(timer >= ChaseTime)
                {
                    _alertSpot = laspPos;
                    GuardUtil.state = GuardUtil.State.Investigate;
                    break;
                }

                yield return null;
            }
            _chasing = false;
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
                GuardUtil.SpotPlayer(_sight, ref _playerVisibleTimer, TimeToSpotPlayer);
                timer += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Adds functionaility for new WaitForSeconds, but with improved can see player checks to change state
        /// to chase if player is detected.
        /// </summary>
        /// <param name="waitTime"></param>
        /// <returns></returns>
        private IEnumerator SearchForPlayer(float waitTime)
        {
            var timer = 0f;

            while(timer <= waitTime) {
				if (GuardUtil.CanSeePlayer(_sight))
                    GuardUtil.state = GuardUtil.State.Chase;

				timer += Time.deltaTime;
                yield return null;
            }
        }

		public void OnDrawGizmos()
		{
		    GuardUtil.DrawWaypointGizmos(Waypoints);
		    GuardUtil.DrawNextWaypointLineGizmos(transform.position, Waypoints, _waypointIndex);
		}

    }
}