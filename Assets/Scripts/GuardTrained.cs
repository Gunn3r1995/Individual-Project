using System.Collections;
using System.Linq;
using Assets.Scripts.AStar;
using JetBrains.Annotations;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;

namespace Assets.Scripts
{
	public class GuardTrained : MonoBehaviour
	{
		#region Variables

		public GuardUtil GuardUtil;
		private FieldOfView _fov;
		private AudioSource _audioSource;
		public GameObject Player;
		public bool AutoTargetPlayer;
		//public enum State { Patrol, Alert, Investigate, Chase }
		//public GuardUtil.State state;

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
		//public Transform AlertGroup01;
		//public Transform AlertGroup02;
		//public Transform AlertGroup03;

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
	        if (GetComponent<GuardUtil>() == null) gameObject.AddComponent<GuardUtil>();
	        GuardUtil = GetComponent<GuardUtil>();
	        _fov = GetComponent<FieldOfView>();
	        _grid = FindObjectOfType<AStar.Grid>();
	        _gridAgent = GetComponent<GridAgent>();
	        _audioSource = FindObjectOfType<AudioSource>();
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
			// TODO FIX ME
			//AudioClip clip = Resources.Load<AudioClip>("Voices/huh_01");
			//_audioSource.PlayOneShot(clip);

			Fsm();
		}

		private void Fsm()
		{
			switch (GuardUtil.state)
			{
				case GuardUtil.State.Patrol:
					GuardUtil.SpotPlayer(_fov, ref _playerVisibleTimer, TimeToSpotPlayer);
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

			// Goto first waypoint
			_gridAgent.Speed = PatrolSpeed;
			_gridAgent.SetDestination(transform.position, Waypoints[0].transform.position);

			while (GuardUtil.state == GuardUtil.State.Patrol)
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
					_gridAgent.SetDestination(transform.position, Waypoints[_waypointIndex].transform.position);

					// Stop the guard for PatrolWaitTime amount of time
					_gridAgent.StopMoving();
					yield return StartCoroutine(LookForPlayer(PatrolWaitTime));

					// Continue patrolling at PatrolSpeed
					_gridAgent.Speed = PatrolSpeed;
				}
				yield return null;
			}
			_patrolling = false;
		}

		private IEnumerator Alert()
		{
			print("Alerted");
			_alerted = true;

			// Set alert spot to players location
			_alertSpot = Player.transform.position;

			// Set the destination to the alert spot
			transform.LookAt(_alertSpot);
			_gridAgent.SetDestination(transform.position, _alertSpot);

			// Stop moving and wait for 'AlertReactionTime' amount
			_gridAgent.StopMoving();
			yield return new WaitForSeconds(AlertReactionTime);

			while (GuardUtil.state == GuardUtil.State.Alert)
			{
				print("Alerted");
			    _gridAgent.Speed = InvestigateSpeed;

				if (_gridAgent.HasPathFinished)
				{
					// On Alert Destination Reached

					// Work around to force stop the agent
					_gridAgent.SetDestination(transform.position, GuardUtil.CreateRandomWalkablePosition(transform.position, 5.0f, _grid));
					_gridAgent.StopMoving();

					// Wait for 'InvestigateSpotTime' amount while looking for the player
					yield return StartCoroutine(SearchForPlayer(InvestigateSpotTime));
					GuardUtil.state = GuardUtil.State.Investigate;

					break;
				}

				// If can se player while alerted go straight to chase
				if (GuardUtil.CanSeePlayer(_fov))
					GuardUtil.state = GuardUtil.State.Chase;

				yield return null;
			}
			print("Finished Alert");
			_alerted = false;
		}

		private IEnumerator Investigate()
		{
			print("Investigating");
			_investigating = true;
			float timer = 0.0f;

			Vector3 lastPos = new Vector3(0, 0, 0);
			// Generate first waypoint and save the position
			Vector3 targetPosition = GuardUtil.CreateRandomWalkablePosition(_alertSpot, WanderRadius, ref lastPos, _grid);

			// Go to first waypoint
			_gridAgent.Speed = InvestigateSpeed;
			_gridAgent.SetDestination(transform.position, targetPosition);

			while (GuardUtil.state == GuardUtil.State.Investigate)
			{
				// Add to time
				timer += Time.deltaTime;

				// If can se player while investigating go straight to chase
				if (GuardUtil.CanSeePlayer(_fov))
					GuardUtil.state = GuardUtil.State.Chase;

				if (_gridAgent.HasPathFinished)
				{
					// Guard reached waypoint
					// Create a new waypoint parsing in the last waypoint; by reference so that it keeps the 'lastPos' updated
					targetPosition = GuardUtil.CreateRandomWalkablePosition(_alertSpot, WanderRadius, ref lastPos);

					// Set the destination and stop moving work around
					_gridAgent.SetDestination(transform.position, targetPosition);
					_gridAgent.StopMoving();

					// Wait at waypoint for 'InvestigateSpotTime' amount
					yield return StartCoroutine(SearchForPlayer(InvestigateSpotTime));
					// Add the 'InvestigateSpotTime' amount to the timer
					timer += InvestigateSpotTime;

					// Start walking to next waypoint
				    _gridAgent.Speed = InvestigateSpeed;
				}

				if (timer >= InvestigateTime)
				{
					// If investiage time reached go back to patrol
					GuardUtil.state = GuardUtil.State.Patrol;
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
		    _gridAgent.Speed = ChaseSpeed;

			while (GuardUtil.state == GuardUtil.State.Chase)
			{
				timer += Time.deltaTime;

				if (GuardUtil.CanSeePlayer(_fov))
				{
					laspPos = Player.transform.position;
					_gridAgent.StraightToDestination(Player.transform.position);
				}
				else
				{
					_gridAgent.SetDestination(transform.position, laspPos);
				}

				if (Vector3.Distance(transform.position, Player.transform.position) <= 1.0f)
				{
					_gridAgent.StopMoving();
					GuardUtil.GuardOnCaughtPlayer();
				}
				else if (Vector3.Distance(transform.position, Player.transform.position) >= 20f)
				{
					GuardUtil.state = GuardUtil.State.Investigate;
					break;
				}

				if (timer >= ChaseTime)
				{
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
	            GuardUtil.SpotPlayer(_fov, ref _playerVisibleTimer, TimeToSpotPlayer);
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

	        while (timer <= waitTime)
	        {
	            if (GuardUtil.CanSeePlayer(_fov))
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