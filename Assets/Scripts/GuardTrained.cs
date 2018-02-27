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

        [HideInInspector]
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

	    private Vector3 lastPos;


        public AudioClip TestAudioClip;

        #endregion

	    [UsedImplicitly]
	    private void Awake()
	    {
	        if (GetComponent<GuardUtil>() == null) gameObject.AddComponent<GuardUtil>();
	        GuardUtil = GetComponent<GuardUtil>();
	        _fov = GetComponent<FieldOfView>();
	        _grid = FindObjectOfType<AStar.Grid>();
	        _gridAgent = GetComponent<GridAgent>();
	        _audioSource = GetComponent<AudioSource>();
        }

	    [UsedImplicitly]
	    private void Start()
	    {
	        if (AutoTargetPlayer)
	            Player = GameObject.FindGameObjectsWithTag("Player").Last();

	        if (RandomWaypoints)
	            _waypointIndex = Random.Range(0, Waypoints.Length);

	        GuardUtil.state = GuardUtil.State.Patrol;
	        _audioSource.clip = TestAudioClip;
        }

	    [UsedImplicitly]
	    private void FixedUpdate()
	    {
            //_audioSource.Play();
            //_audioSource.PlayOneShot(TestAudioClip);


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
                    print("FSM alert");
                    if (!_alerted)
						StartCoroutine(Alert());
					break;
				case GuardUtil.State.Investigate:
                    print("FSM: Investigate");
                    if (!_investigating)
						StartCoroutine(Investigate());
					break;
				case GuardUtil.State.Chase:
                    print("FSM: chase at speed: " + _gridAgent.Speed);
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
			_gridAgent.SetDestination(transform.position, Waypoints[_waypointIndex].transform.position);

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
			print("Alert");
			_alerted = true;

			// Set alert spot to players location
            if(GuardUtil.CanSeePlayer(_fov)) _alertSpot = Player.transform.position;

            //var sound = Resources.Load<AudioClip>("Voices/Huh_what_was_that");
            print("Playing Sound");
            _audioSource.PlayOneShot(TestAudioClip);

			// Set the destination to the alert spot
			transform.LookAt(_alertSpot);
			_gridAgent.SetDestination(transform.position, _alertSpot);

			// Stop moving and wait for 'AlertReactionTime' amount
			_gridAgent.StopMoving();
			yield return new WaitForSeconds(AlertReactionTime);

		    _gridAgent.Speed = InvestigateSpeed;



            while (GuardUtil.state == GuardUtil.State.Alert)
			{
				print("Alerted");

			    // If can se player while alerted go straight to chase
			    if (GuardUtil.CanSeePlayer(_fov))
			    {
			        GuardUtil.state = GuardUtil.State.Chase;
			    } else if (_gridAgent.HasPathFinished)
				{
					// On Alert Destination Reached

					// Work around to force stop the agent
					_gridAgent.SetDestination(transform.position, GuardUtil.CreateRandomWalkablePosition(transform.position, 5.0f, _grid));
					_gridAgent.StopMoving();

					// Wait for 'InvestigateSpotTime' amount while looking for the player
					yield return StartCoroutine(SearchForPlayer(InvestigateSpotTime));
				    GuardUtil.state = GuardUtil.State.Investigate;
				}
			    yield return null;
			}

			print("Finished Alert");
			_alerted = false;
		}

		private IEnumerator Investigate()
		{
			print("Investigating");
			_investigating = true;
			var timer = 0.0f;


		    var investLastPos = lastPos;
			// Generate first waypoint and save the position
			var targetPosition = GuardUtil.CreateRandomWalkablePosition(_alertSpot, WanderRadius, ref investLastPos, _grid);

			// Go to first waypoint
			_gridAgent.Speed = InvestigateSpeed;
			_gridAgent.SetDestination(transform.position, targetPosition);

			while (GuardUtil.state == GuardUtil.State.Investigate)
			{
                print("Investigating");
				// Add to time
				timer += Time.deltaTime;

				// If can se player while investigating go straight to chase
			    if (GuardUtil.CanSeePlayer(_fov))
			    {
			        GuardUtil.state = GuardUtil.State.Chase;
                    _gridAgent.StopMoving();
                    break;
			    }

			    if (_gridAgent.HasPathFinished)
				{
                    print("FINISHED INVEST PATH");
					// Guard reached waypoint
					// Create a new waypoint parsing in the last waypoint; by reference so that it keeps the 'lastPos' updated
					targetPosition = GuardUtil.CreateRandomWalkablePosition(_alertSpot, WanderRadius, ref investLastPos);

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
		    var GoingToLastPos = false;

			lastPos = Player.transform.position;
			transform.LookAt(lastPos);
			_gridAgent.SetDestination(transform.position, lastPos);
		    _gridAgent.Speed = ChaseSpeed;

			while (GuardUtil.state == GuardUtil.State.Chase)
			{
				timer += Time.deltaTime;
			    _gridAgent.Speed = ChaseSpeed;
                
			    if (GuardUtil.CanSeePlayer(_fov))
			    {
                    _gridAgent.StopAllCoroutines();
			        GoingToLastPos = false;
			        lastPos = Player.transform.position;
			        _gridAgent.StraightToDestination(Player.transform.position);
			    } else
			    {
			        if (!GoingToLastPos)
			        {
			            print("Cannot see player");
			            _gridAgent.SetDestination(transform.position, lastPos);
			            GoingToLastPos = true;
			        }

			        if (_gridAgent.HasPathFinished)
			        {
			            print("Got to last seen position");
			            _alertSpot = lastPos;
			            GuardUtil.state = GuardUtil.State.Investigate;
                        break;
			        }
                }

                if (Vector3.Distance(transform.position, Player.transform.position) <= 1.0f)
				{
					_gridAgent.StopMoving();
					GuardUtil.GuardOnCaughtPlayer();
				}
				//else if (Vector3.Distance(transform.position, Player.transform.position) >= 20f)
				//{
    //                print("");
				//	GuardUtil.state = GuardUtil.State.Investigate;
				//	break;
				//}

				if (timer >= ChaseTime)
				{
                    print("Chase time up");
				    _alertSpot = lastPos;
					GuardUtil.state = GuardUtil.State.Investigate;
					break;
				}

				yield return null;
			}
            print("Finsihed Chasing");
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
		}
	}
}