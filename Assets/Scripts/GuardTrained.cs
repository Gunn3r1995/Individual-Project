using System.Collections;
using System.Linq;
using Assets.Scripts.AStar;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts
{
	public class GuardTrained : MonoBehaviour
	{
		//TODO Alter CanSeePlayer for using transform from array
		//TODO Alter CanHearPlayer for using transform from array

        //TODO Memory of last locations
        //TODO If already seen player then quicker reaction time (More Alerted)

		#region Variables

		[HideInInspector]
		public GuardUtil GuardUtil;

		private AudioSource _audioSource;
		public GameObject Player;
		public bool AutoTargetPlayer;

		private AStar.Grid _grid;
		private GridAgent _gridAgent;
	    public bool Disabled { get; set; }
	    public Collider[] Triggers;

		#region Sight
		private FieldOfView _fov;
        public float TimeToSpotPlayer = 0.5f;
		private float _playerVisibleTimer;
	    private float _guardVisibleTimer;
        #endregion

        #region Hearing
        private Hearing _hearing;
        public float TimeToHearPlayer = 0.2f;
        private float _playerHearedTimer;
        #endregion

        #region Patrol
        public GameObject[] Waypoints;
		public bool RandomWaypoints;
		public float PatrolSpeed = 0.75f;
		public float PatrolWaitTime = 3.0f;

		private int _waypointIndex;
		private bool _patrolling;
        #endregion

	    #region Rendezvous
	    public float RendezvousChance;
	    public float RedezvousWaitTime = 5.0f;

	    private float _redezvousTimer = 5.0f;
        [HideInInspector]
	    public bool RequestRendezvous = false;
	    #endregion

        #region Alert
        public float AlertReactionTime = 2.0f;
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

	    #region Chase
	    private bool _standing;

	    #endregion

        #region Voice
        public VoicesDatabase VoicesDatabase;
	    #endregion

        [HideInInspector]
	    public Vector3 LastPos;
        private Vector3 _lastPosTracked;

        #endregion


        [UsedImplicitly]
	    private void Awake()
	    {
	        if (GetComponent<GuardUtil>() == null) gameObject.AddComponent<GuardUtil>();
	        GuardUtil = GetComponent<GuardUtil>();
	        _fov = GetComponent<FieldOfView>();
            _hearing = GetComponent<Hearing>();
	        _grid = FindObjectOfType<AStar.Grid>();
	        _gridAgent = GetComponent<GridAgent>();
	        _audioSource = GetComponent<AudioSource>();
        }

	    [UsedImplicitly]
	    private void Start()
	    {
	        if (AutoTargetPlayer || Player == null)
	            Player = GameObject.FindGameObjectsWithTag("Player").Last();

	        if (RandomWaypoints)
	            _waypointIndex = Random.Range(0, Waypoints.Length);

	        GuardUtil.state = GuardUtil.State.Patrol;

	        if (Triggers.Length > 0)
	        {
	            FindObjectOfType<PlayerController>().OnPlayerEnterGuardTrigger += TriggerAction;
	            Disabled |= Triggers.Length > 0;
            }
	    }

	    private void TriggerAction(Collider col)
	    {
	        foreach (var trigger in Triggers)
	        {
	            if (trigger != col) continue;
	            Disabled = false;
	        }
	    }

	    [UsedImplicitly]
	    private void FixedUpdate()
	    {
	        if (Disabled)
	        {
	            GuardUtil.SpotPlayer(_fov, ref _playerVisibleTimer, TimeToSpotPlayer);
	            Disabled &= (!GuardUtil.CanSeePlayer(_fov) && !GuardUtil.CanHearPlayer(_hearing));
	        } else Fsm();
		}

	    private void Fsm()
	    {
	        _redezvousTimer += Time.deltaTime;

            switch (GuardUtil.state)
			{
				case GuardUtil.State.Patrol:
					GuardUtil.SpotPlayer(_fov, ref _playerVisibleTimer, TimeToSpotPlayer);
                    GuardUtil.ListenForPlayer(_hearing, ref _playerHearedTimer, TimeToHearPlayer);

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
                case GuardUtil.State.Standing:
                    if (!_standing)
                        StartCoroutine(Stand());
                    break;
                default:
                    GuardUtil.SpotPlayer(_fov, ref _playerVisibleTimer, TimeToSpotPlayer);
                    GuardUtil.ListenForPlayer(_hearing, ref _playerHearedTimer, TimeToHearPlayer);
                    if (!_patrolling)
                        StartCoroutine(Patrol());
                    break;
			}
		}

		private IEnumerator Patrol()
		{
			_patrolling = true;
			print("Patrolling");

            // Goto first waypoint
            _gridAgent.Speed = PatrolSpeed;
			_gridAgent.RequestSetDestination(transform.position, Waypoints[_waypointIndex].transform.position);

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
					_gridAgent.RequestSetDestination(transform.position, Waypoints[_waypointIndex].transform.position);

					// Stop the guard for PatrolWaitTime amount of time
					_gridAgent.StopMoving();
					yield return StartCoroutine(LookForPlayer(PatrolWaitTime));

					// Continue patrolling at PatrolSpeed
					_gridAgent.Speed = PatrolSpeed;
				}

			    _redezvousTimer += Time.deltaTime;
			    if (GuardUtil.CanSeeGuard(_fov) && _redezvousTimer >= RedezvousWaitTime)
			    {
			        if (Vector3.Distance(transform.position, _fov.VisibleGuards[0].GetComponent<GuardTrained>().transform.position) <= 4.0f)
			        {
                        print("Guard Talk");

			            _gridAgent.Speed = 0;
                        transform.LookAt(_fov.VisibleGuards[0].GetComponent<GuardTrained>().transform);

			            yield return StartCoroutine(TalkToGuard(RedezvousWaitTime));
			            _redezvousTimer = 0f;
			            _gridAgent.Speed = PatrolSpeed;
			        }
                }

				yield return null;
			}
			_patrolling = false;
		}

	    //private IEnumerator Redezvous()
	    //{
	    //    _rendezvous = true;
     //       print("Rendezvous");
	    //    var guard = _fov.VisibleGuards[0].GetComponent<GuardTrained>();
	        
     //       if (guard != null)
	    //    {
	    //        if (guard.RequestRendezvous)
	    //        {
     //               // Look At Other Guard
     //               transform.LookAt(guard.transform);
     //               // Go towards other guard
     //               _gridAgent.RequestSetDestination(transform.position, guard.transform.position);
	    //            yield return null;

	    //            while (GuardUtil.state == GuardUtil.State.Redezvous)
	    //            {
     //                   if (_gridAgent.HasPathFinished || Vector3.Distance(transform.position, guard.transform.position) <= 2.0f)
	    //                {
	    //                    transform.LookAt(guard.transform);
     //                       _gridAgent.StopMoving();

	    //                    yield return StartCoroutine(TalkToGuard(RedezvousWaitTime));

	    //                    GuardUtil.state = GuardUtil.State.Patrol;
	    //                    RequestRendezvous = false;
	    //                }
	    //                yield return null;
	    //            }
     //           }
     //       }
	    //    _redezvousTimer = 0f;
	    //    _rendezvous = false;
	    //}

		private IEnumerator Alert()
		{
			print("Alert");
			_alerted = true;
		    var alertSpot = new Vector3();

            // Set alert spot to players location
            if (GuardUtil.CanSeePlayer(_fov)) alertSpot = Player.transform.position;
            else if (GuardUtil.CanHearPlayer(_hearing)) alertSpot = Player.transform.position;

            // Play state sound
		    AttemptPlayRandomStateSound(false);

		    // Set the destination to the alert spot
			transform.LookAt(alertSpot);
			_gridAgent.RequestSetDestination(transform.position, alertSpot);

			// Stop moving and wait for 'AlertReactionTime' amount
			_gridAgent.StopMoving();
			yield return new WaitForSeconds(AlertReactionTime);

		    _gridAgent.Speed = InvestigateSpeed;

            while (GuardUtil.state == GuardUtil.State.Alert)
			{
				print("Alerted");

			    // If can se player while alerted go straight to chase
                if (GuardUtil.CanSeePlayer(_fov) || GuardUtil.CanHearPlayer(_hearing))
			    {
			        GuardUtil.state = GuardUtil.State.Chase;
			    } else if (_gridAgent.HasPathFinished)
				{
					// On Alert Destination Reached

					// Work around to force stop the agent
					_gridAgent.RequestSetDestination(transform.position, GuardUtil.CreateRandomWalkablePosition(transform.position, 5.0f, _grid));
					_gridAgent.StopMoving();

					// Wait for 'InvestigateSpotTime' amount while looking for the player
					yield return StartCoroutine(SearchForPlayer(InvestigateSpotTime));
				    LastPos = alertSpot;
				    GuardUtil.state = GuardUtil.State.Investigate;
				}
			    yield return null;
			}

			print("Finished Alert");
			_alerted = false;
		}

	    private void AttemptPlayRandomStateSound(bool finishClips)
	    {
	        if (VoicesDatabase == null) return;
            if(_audioSource == null) return;
	        if(_audioSource.isPlaying) return;

            switch (GuardUtil.state)
            {
                case GuardUtil.State.Patrol:
                    break;
                case GuardUtil.State.Alert:
                    var alertClip = VoicesDatabase.GetRandomAlertClip();
                    if(alertClip != null && !_audioSource.isPlaying)
                        _audioSource.PlayOneShot(alertClip);
                    break;
                case GuardUtil.State.Investigate:
                    if (!finishClips)
                    {
                        var investigateClip = VoicesDatabase.GetRandomInvestigateClip();
                        if (investigateClip != null && !_audioSource.isPlaying)
                            _audioSource.PlayOneShot(investigateClip);
                    }
                    else
                    {
                        var investigateFinishClip = VoicesDatabase.GetRandomInvestigateFinishClip();
                        if (investigateFinishClip != null && !_audioSource.isPlaying)
                            _audioSource.PlayOneShot(investigateFinishClip);
                    }
                    break;
                case GuardUtil.State.Chase:
                    if (!finishClips)
                    {
                        var chaseClip = VoicesDatabase.GetRandomChaseClip();
                        if (chaseClip != null && !_audioSource.isPlaying)
                            _audioSource.PlayOneShot(chaseClip);
                    }
                    else
                    {
                        var chaseFinishClips = VoicesDatabase.GetRandomChaseFinishClip();
                        if (chaseFinishClips != null && !_audioSource.isPlaying)
                            _audioSource.PlayOneShot(chaseFinishClips);
                    }
                    break;
                default:
                    break;
            }
        }

        private IEnumerator Investigate()
		{
			print("Investigating");
			_investigating = true;
			var timer = 0.0f;
		    var speakTimer = 0.0f;
		    var randomTime = Random.Range(5f, 15f);

            var investLastPos = LastPos;
            print("Last Pos:" + LastPos);
		    print("Invest Last Pos:" + investLastPos);

            // Generate first waypoint and save the position
            var direction = (transform.forward * 5 - transform.position).normalized;
            var distance = Vector3.Distance(transform.position, LastPos + transform.forward * 5);

            Vector3 targetPosition;
		    if (!Physics.Raycast(transform.position, direction, distance, _fov.ObstacleMask))
		    {
		        targetPosition = LastPos + transform.forward * 5;
                print("Forward target position:" + targetPosition);
		    }
		    else
		    {
		        targetPosition = GuardUtil.CreateRandomWalkablePosition(LastPos, WanderRadius, ref investLastPos, _grid);
		        print("Target Position: " + targetPosition);
            }

		    print("Target Position: " + targetPosition);

			// Go to first waypoint
			_gridAgent.Speed = InvestigateSpeed;
			_gridAgent.RequestSetDestination(transform.position, targetPosition);

			while (GuardUtil.state == GuardUtil.State.Investigate)
			{
                print("Investigating");
				// Add to time
				timer += Time.deltaTime;
			    speakTimer += Time.deltaTime;

                if (GuardUtil.CanSeeGuard(_fov))
			    {
			        var alreadySpoken = false;
			        foreach (var visibleGuard in _fov.VisibleGuards)
			        {
			            if (visibleGuard.GetComponent<GuardUtil>().state == GuardUtil.State.Investigate) continue;

                        // Ask For Help
			            if (!alreadySpoken)
			            {
			                if (VoicesDatabase != null)
			                {
			                    var helpClip = VoicesDatabase.GetRandomInvestigateAskForHelpClip();
			                    if (helpClip != null && !_audioSource.isPlaying)
			                        _audioSource.PlayOneShot(helpClip);
			                }
			                alreadySpoken = true;
			            }

			            visibleGuard.GetComponent<GuardUtil>().state = GuardUtil.State.Investigate;
			            visibleGuard.GetComponent<GuardTrained>().LastPos = LastPos;
			        }
			    }

                // If can se player while investigating go straight to chase
                if (GuardUtil.CanSeePlayer(_fov) || GuardUtil.CanHearPlayer(_hearing))
			    {
                    LastPos = Player.transform.position;
			        GuardUtil.state = GuardUtil.State.Chase;
                    _gridAgent.StopMoving();
                    break;
			    }

			    if (_gridAgent.HasPathFinished)
				{
				    // Play Investigate Voices
				    AttemptPlayRandomStateSound(false);

                    // Guard reached waypoint
                    // Create a new waypoint parsing in the last waypoint; by reference so that it keeps the 'lastPos' updated
                    targetPosition = GuardUtil.CreateRandomWalkablePosition(LastPos, WanderRadius, ref investLastPos);

					// Set the destination and stop moving work around
					_gridAgent.RequestSetDestination(transform.position, targetPosition);
					_gridAgent.StopMoving();

					// Wait at waypoint for 'InvestigateSpotTime' amount
					yield return StartCoroutine(SearchForPlayer(InvestigateSpotTime));
					// Add the 'InvestigateSpotTime' amount to the timer
					timer += InvestigateSpotTime;

					// Start walking to next waypoint
				    _gridAgent.Speed = InvestigateSpeed;
				}

			    if (speakTimer >= randomTime)
			    {
			        // Speak
                    AttemptPlayRandomStateSound(false);

			        speakTimer = 0;
			        randomTime = Random.Range(5f, 15f);
			    }

				if (timer >= InvestigateTime)
				{
				    // Play Investigate Voices
				    AttemptPlayRandomStateSound(true);

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
			LastPos = Player.transform.position;
			_lastPosTracked = Player.transform.position + Player.transform.forward * 5.0f;

            var timer = 0.0f;
		    var speakTimer = 0.0f;
		    var RandomTime = Random.Range(5f, 15f);

            var blockedByObstacle = false;
		    var goingToLastPosition = false;

            AttemptPlayRandomStateSound(false);
		    var alreadySpoken = false;

            while (GuardUtil.state == GuardUtil.State.Chase)
			{
				timer += Time.deltaTime;
			    speakTimer += Time.deltaTime;

			    _gridAgent.Speed = ChaseSpeed;

                if (GuardUtil.CanSeeGuard(_fov))
                {
                    foreach (var visibleGuard in _fov.VisibleGuards)
                    {
                        if (visibleGuard.GetComponent<GuardUtil>().state == GuardUtil.State.Chase) continue;

                        // Ask For Help
                        if (!alreadySpoken)
                        {
                            if (VoicesDatabase != null)
                            {
                                var helpClip = VoicesDatabase.GetRandomChaseAskForHelpClip();
                                if (helpClip != null && !_audioSource.isPlaying)
                                    _audioSource.PlayOneShot(helpClip);
                            }
                            alreadySpoken = true;
                        }

                        visibleGuard.GetComponent<GuardUtil>().state = GuardUtil.State.Chase;
                        visibleGuard.GetComponent<GuardTrained>().LastPos = LastPos;
                    }
                }

                if (GuardUtil.CanSeePlayer(_fov) || GuardUtil.CanHearPlayer(_hearing))
                {
                    LastPos = Player.transform.position;
                    _lastPosTracked = Player.transform.position + Player.transform.forward * 5.0f;
                    goingToLastPosition = false;

                    if (!GuardUtil.IsBlockedByObstacle(transform.position, Player.transform.position, _fov.ObstacleMask))
                    {
                        _gridAgent.StopAllCoroutines();
                        _gridAgent.StraightToDestination(Player.transform.position);
                    }
                    else
                    {
                        if (!blockedByObstacle)
                        {
                            print("Going around obstacle");
                            blockedByObstacle = true;
                            _gridAgent.RequestSetDestination(transform.position, Player.transform.position);
                        }

                        while (blockedByObstacle)
                        {
                            if (_gridAgent.HasPathFinished || Vector3.Distance(transform.position, LastPos) <= 1.0f)
                            {
                                blockedByObstacle = false;
                                break;
                            }
                            if (!GuardUtil.IsBlockedByObstacle(transform.position, Player.transform.position, _fov.ObstacleMask))
                            {
                                _gridAgent.StopAllCoroutines();
                                blockedByObstacle = false;
                                break;
                            }
                            yield return null;
                        }
                    }
                }
                else
                {
                    if (!GuardUtil.IsBlockedByObstacle(transform.position, _lastPosTracked, _fov.ObstacleMask))
                    {
                        if (!goingToLastPosition)
						{
						    print("Going to last forseen sighting");
                            goingToLastPosition = true;
                            _gridAgent.RequestSetDestination(transform.position, _lastPosTracked);
						}

                        var tempTimer = 0.0f;
                        while (goingToLastPosition)
                        {
                            tempTimer += Time.deltaTime;

                            if (_gridAgent.HasPathFinished)
                            {
                                _gridAgent.StopMoving();
                                GuardUtil.state = GuardUtil.State.Investigate;

                                AttemptPlayRandomStateSound(true);
                                break;
                            }

                            if (tempTimer >= InvestigateSpotTime)
                            {
                                print("Chase time up");
                                _gridAgent.StopMoving();
                                GuardUtil.state = GuardUtil.State.Investigate;

                                AttemptPlayRandomStateSound(true);
                                goingToLastPosition = false;
                                break;
                            }

                            if (GuardUtil.CanSeePlayer(_fov) || GuardUtil.CanHearPlayer(_hearing)) {
								_gridAgent.StopAllCoroutines();
                                goingToLastPosition = false;
                                break;
                            }

							yield return null;
						}
                    }
                    else {
						if (!goingToLastPosition)
						{
                            print("Going to last sighting");
							goingToLastPosition = true;
                            _gridAgent.RequestSetDestination(transform.position, LastPos);
						}

                        var tempTimer = 0.0f;
                        while (goingToLastPosition)
                        {
                            tempTimer += Time.deltaTime;

                            if (_gridAgent.HasPathFinished)
                            {
                                _gridAgent.StopMoving();
                                GuardUtil.state = GuardUtil.State.Investigate;

                                AttemptPlayRandomStateSound(true);
                                break;
                            }

                            if (GuardUtil.CanSeePlayer(_fov) || GuardUtil.CanHearPlayer(_hearing))
							{
								_gridAgent.StopAllCoroutines();
								goingToLastPosition = false;
							    break;
                            }

                            if (tempTimer >= InvestigateSpotTime)
                            {
                                print("Chase time up");
                                _gridAgent.StopMoving();
                                GuardUtil.state = GuardUtil.State.Investigate;

                                AttemptPlayRandomStateSound(true);
                                goingToLastPosition = false;
                                break;
                            }

                            yield return null;
						}
                    }
                }

                if (Vector3.Distance(transform.position, Player.transform.position) <= 1.0f)
                {
                    _gridAgent.RequestSetDestination(transform.position, Player.transform.position);
                    _gridAgent.StopMoving();

				    _gridAgent.Disable();
				    GuardUtil.state = GuardUtil.State.Standing;
					GuardUtil.GuardOnCaughtPlayer();
				    yield break;
				}

			    if (speakTimer >= RandomTime)
			    {
                    AttemptPlayRandomStateSound(false);

			        speakTimer = 0f;
			        RandomTime = Random.Range(5f, 15f);
			        alreadySpoken = false;
			    }

				if (timer >= ChaseTime)
				{
                    print("Chase time up");
                    _gridAgent.StopMoving();

                    GuardUtil.state = GuardUtil.State.Investigate;
					break;
				}

				yield return null;
			}

            print("Finsihed Chasing");
			_chasing = false;
		}

	    private IEnumerator Stand()
	    {
	        _standing = true;

	        while (GuardUtil.state == GuardUtil.State.Standing)
	        {
	            _gridAgent.StopMoving();
                yield return null;
	        }

	        _standing = false;
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
                if (GuardUtil.CanSeePlayer(_fov) || GuardUtil.CanHearPlayer(_hearing))
	                GuardUtil.state = GuardUtil.State.Chase;

	            timer += Time.deltaTime;
	            yield return null;
	        }
	    }

	    private IEnumerator TalkToGuard(float waitTime)
	    {
	        var timer = 0f;

	        while (timer <= waitTime)
	        {
	            GuardUtil.SpotPlayer(_fov, ref _playerVisibleTimer, TimeToSpotPlayer);

                // Todo Talk To Guard

	            timer += Time.deltaTime;
	            yield return null;
	        }
	    }

        public void OnDrawGizmos()
        {
            if(Waypoints != null)
                GuardUtil.DrawWaypointGizmos(Waypoints);
            if(Triggers != null)
                GuardUtil.DrawTriggerGizmos(Triggers);
        }
    }
}