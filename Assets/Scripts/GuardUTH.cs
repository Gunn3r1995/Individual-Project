using Assets.Scripts;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

namespace Assets.Scripts
{
    public class GuardUTH : MonoBehaviour
    {
        public static event System.Action OnGuardApprehendingPlayer;

        public bool autoTargetPlayer = true;
        public GameObject player;
        public enum State { PATROL, ALERT, SEARCH, CHASE }
        public State state;
        private bool alive;

        // Patrolling
        public GameObject[] waypoints;
        public bool randomWaypoints;
        public float patrolSpeed = 0.5f;
        private int waypointIndex = 0;


        //public float waitTime = 0.3f;
        //public float turnSpeed = 90f;


        // Alert    
        public float searchWait = 10.0f;
        private float timer = 0f;
        private Vector3 alertSpot;

        //public float reactionTime = 1.0f;
        //public GameObject alert;

        // Search

        //public float wanderTimer = 10.0f;
        //public float wanderRadius = 10.0f;

        // Chase
        public float chaseSpeed = 1.0f;

        // Sight
        public float heightMultiplier = 1.36f;
        public float sightDistance = 10f;
        public LayerMask viewMask;

        public float timeToSpotPlayer = 0.5f;
        float playerVisibleTimer;
        public Light spotlight;


        Color originalSpotlightColour;
        float viewAngle;

        // Other
        NavMeshAgent agent;
        ThirdPersonCharacter character;

        bool patrolling;
        bool alerted;
        bool searching;
        bool chasing;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            character = GetComponent<ThirdPersonCharacter>();

            if (autoTargetPlayer)
            {
                //Implement different way to find player
                player = GameObject.FindGameObjectWithTag("Player");
            }

            if (randomWaypoints)
            {
                waypointIndex = Random.Range(0, waypoints.Length);
            }

            agent.updatePosition = true;
            agent.updateRotation = false;

            state = GuardUTH.State.PATROL;
            alive = true;

            viewAngle = spotlight.spotAngle;
            originalSpotlightColour = spotlight.color;
        }

        void Update()
        {
            if (alive)
            {
                SpotPlayer();
                FSM();
            }
        }

        void FSM()
        {
            switch (state)
            {
                case State.PATROL:
                    if (!patrolling)
                    {
                        if (randomWaypoints)
                        {
                            StartCoroutine(RandomPatrol());
                        }
                        else
                        {
                            StartCoroutine(Patrol());
                        }
                    }
                    break;
                case State.ALERT:
                    if (!alerted)
                    {
                        StartCoroutine(Alert());
                    }
                    break;
                case State.SEARCH:
                    print("Searching");
                    break;
                case State.CHASE:
                    print("Chase State");
                    if (!chasing)
                    {
                        StartCoroutine(Chase());
                    }
                    break;
            }
        }

        IEnumerator Patrol()
        {
            print("Patrolling");
            patrolling = true;
            agent.speed = patrolSpeed;

            while (state == State.PATROL)
            {
                float distance = Vector3.Distance(this.transform.position, waypoints[waypointIndex].transform.position);
                if (distance >= 0.5)
                {
                    agent.SetDestination(waypoints[waypointIndex].transform.position);
                    character.Move(agent.desiredVelocity, false, false);
                }
                else if (distance <= 0.5)
                {
                    waypointIndex += 1;
                    if (waypointIndex >= waypoints.Length)
                    {
                        waypointIndex = 0;
                    }
                }
                else
                {
                    character.Move(Vector3.zero, false, false);
                }
                yield return null;
            }
            patrolling = false;
        }

        IEnumerator RandomPatrol()
        {
            patrolling = true;
            agent.speed = patrolSpeed;

            while (state == State.PATROL)
            {
                float distance = Vector3.Distance(this.transform.position, waypoints[waypointIndex].transform.position);

                if (distance >= 0.5)
                {
                    agent.SetDestination(waypoints[waypointIndex].transform.position);
                    character.Move(agent.desiredVelocity, false, false);
                }
                else if (distance <= 0.5)
                {
                    waypointIndex = Random.Range(0, waypoints.Length);
                }
                else
                {
                    character.Move(Vector3.zero, false, false);
                }
                yield return null;
            }
            patrolling = false;
        }

        IEnumerator Alert()
        {
            print("Alerted");
            alerted = true;

            if (CanSeePlayer())
                alertSpot = player.transform.position;

            yield return Wait(3.0f);

            while (state == State.ALERT)
            {
                print("alert");
                agent.SetDestination(alertSpot);
                character.Move(agent.desiredVelocity, false, false);

                if (CanSeePlayer())
                {
                    state = GuardUTH.State.CHASE;
                }
                else if (Vector3.Distance(this.transform.position, alertSpot) <= 1)
                {
                    state = GuardUTH.State.SEARCH;
                }
                yield return null;
            }
            alerted = false;
        }

        IEnumerator Wait(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
        }

        IEnumerator Search()
        {
            print("Searching");
            searching = true;

            agent.isStopped = true;
            agent.enabled = false;
            agent.enabled = true;

            while (state == State.SEARCH)
            {

                yield return null;
            }
            searching = false;
        }

        IEnumerator Chase()
        {
            print("Chasing");
            chasing = true;

            agent.speed = chaseSpeed;
            while (state == State.CHASE)
            {
                agent.SetDestination(player.transform.position);
                character.Move(agent.desiredVelocity, false, false);
                yield return null;
            }
            chasing = false;
        }

        void SpotPlayer()
        {
            if (CanSeePlayer())
                playerVisibleTimer += Time.deltaTime;
            else
                playerVisibleTimer -= Time.deltaTime;

            playerVisibleTimer = Mathf.Clamp(playerVisibleTimer, 0, timeToSpotPlayer);
            spotlight.color = Color.Lerp(originalSpotlightColour, Color.red, playerVisibleTimer / timeToSpotPlayer);

            if (playerVisibleTimer >= timeToSpotPlayer && state == State.PATROL)
            {
                state = GuardUTH.State.ALERT;
            }
        }

        bool CanSeePlayer()
        {
            if (Vector3.Distance(transform.position, player.transform.position) < sightDistance)
            {
                Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
                float angleBetweenGuardAndPlayer = Vector3.Angle(transform.forward, dirToPlayer);
                if (angleBetweenGuardAndPlayer < viewAngle / 2f)
                {
                    if (!Physics.Linecast(transform.position, player.transform.position, viewMask))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        void OnDrawGizmos()
        {
            Vector3 startPosition = waypoints[0].transform.position;
            Vector3 previousPosition = startPosition;

            foreach (GameObject waypoint in waypoints)
            {
                Gizmos.DrawSphere(waypoint.transform.position, 0.3f);
                Gizmos.DrawLine(previousPosition, waypoint.transform.position);
                previousPosition = waypoint.transform.position;
            }

            Gizmos.DrawLine(previousPosition, startPosition);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + Vector3.up * heightMultiplier, transform.forward * sightDistance);


        }

        /*

        IEnumerator FollowPath(Vector3[] waypoints)
        {
            print("FollowPath: starting patrolling coroutine");
            followPathCoroutineRunning = true;

            Vector3 targetWaypoint = waypoints[targetWaypointIndex];
            transform.LookAt(targetWaypoint);

            while (state == States.Patrolling)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetWaypoint, walkSpeed * Time.deltaTime);
                animator.SetFloat("speedPercent", 0.5f, speedSmoothTime, Time.deltaTime);

                if (transform.position == targetWaypoint)
                {
                    //Update Waypoint
                    targetWaypointIndex = (targetWaypointIndex + 1) % waypoints.Length;
                    targetWaypoint = waypoints[targetWaypointIndex];

                    yield return StartCoroutine(Wait(waitTime));
                    yield return StartCoroutine(TurnToFace(targetWaypoint));
                    animator.SetFloat("speedPercent", 0.5f);
                }
                yield return null;
            }
            followPathCoroutineRunning = false;
        }

        IEnumerator MoveToPlayersLastKnownPosition(Vector3 targetPosition)
        {
            print("MoveToPostition: starting move to position coroutine");
            moveToPositionCoroutineRunning = true;

            //Change Colour of Alert
            AlertSetActive(true);

            yield return StartCoroutine(Wait(reactionTime));
            yield return StartCoroutine(TurnToFace(targetPosition));

            while (state == States.Alerted)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, runSpeed * Time.deltaTime);
                animator.SetFloat("speedPercent", 1.0f, speedSmoothTime, Time.deltaTime);

                if (CanSeePlayer())
                {
                    state = States.ApprehendingPlayer;
                } else if (transform.position == targetPosition)
                {
                    animator.SetFloat("speedPercent", 0f);
                    state = States.SearchingForPlayer;
                }
                yield return null;
            }

            moveToPositionCoroutineRunning = false;
        }

        IEnumerator SearchForPlayer()
        {
            searchingForPlayerCoroutineRunning = true;

            //Random direction
            //Random distance
            //Go to waypoint and look around.
            float timer = 0f;

            Vector3 targetPosition = RandomNavSphere(transform.position, wanderRadius);
            targetPosition.y = 0;

            while (state == States.SearchingForPlayer)
            {
                timer += Time.deltaTime;
                if (timer <= wanderTimer)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, walkSpeed * Time.deltaTime);

                    if(transform.position == targetPosition)
                    {
                        targetPosition = RandomNavSphere(transform.position, wanderRadius);
                        targetPosition.y = 0;
                    }
                } else
                {
                    print("Times up!!, Im bored");
                    state = States.Patrolling;
                }
                yield return null;
            }
            searchingForPlayerCoroutineRunning = false;
        }

        public Vector3 RandomNavSphere(Vector3 origin, float dist)
        {
            Vector3 randDirection = UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(0, dist);
            randDirection += origin;

            return randDirection;
        }

        IEnumerator ApprehendingPlayer()
        {
            print("ApprehendingPlayer: starting apprehending player coroutine");
            apprehendingPlayerCouroutineRunning = true;

            while (state == States.ApprehendingPlayer)
            {
                if (CanSeePlayer())
                {
                    transform.LookAt(player.position);
                    transform.position = Vector3.MoveTowards(transform.position, player.position, runSpeed * Time.deltaTime);
                    animator.SetFloat("speedPercent", 1.0f, speedSmoothTime, Time.deltaTime);
                } else
                {
                    state = States.SearchingForPlayer;
                }
                yield return null;
            }
            apprehendingPlayerCouroutineRunning = false;
        }

        IEnumerator Wait(float waitTime)
        {
            animator.SetFloat("speedPercent", 0);
            yield return new WaitForSeconds(waitTime);
        }

        bool AlertSetActive(bool activeState)
        {
            if(alert != null)
            {
                alert.SetActive(activeState);
                return true;
            }
            return false;
        }


        */
    }
}