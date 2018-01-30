using Assets.Scripts;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

public class GuardUTH : MonoBehaviour {
    public static event System.Action OnGuardApprehendingPlayer;


    NavMeshAgent agent;
    ThirdPersonCharacter character;

    public bool autoTargetPlayer = true;
    public GameObject player;
    public enum State { PATROL, ALERT, SEARCH, CHASE}
    public State state;
    private bool alive;

    // Variables For Patrolling
    public GameObject[] waypoints;
    public float patrolSpeed = 0.5f;
    private int waypointIndex = 0;
    public float timeToSpotPlayer = 0.5f;

    //public float waitTime = 0.3f;
    //public float turnSpeed = 90f;


    // Variables For Alert
    //public float reactionTime = 1.0f;
    //public GameObject alert;

    // Variables For Search
    //public float wanderTimer = 10.0f;
    //public float wanderRadius = 10.0f;

    // Variables For Chase
    public float chaseSpeed = 1.0f;

    
    // OLD

    public Light spotlight;

    public float viewDistance = 10f;
    public LayerMask viewMask;
    
    float viewAngle;
    float playerVisibleTimer;

    Color originalSpotlightColour;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        character = GetComponent<ThirdPersonCharacter>();

        if (autoTargetPlayer)
        {
            //Implement different way to find player
            player = GameObject.FindGameObjectWithTag("Player");
        }

        agent.updatePosition = true;
        agent.updateRotation = false;

        state = GuardUTH.State.PATROL;
        alive = true;

        viewAngle = spotlight.spotAngle;
        originalSpotlightColour = spotlight.color;

        StartCoroutine("FSM");
    }

    void Update()
    {
        SpotPlayer();
    }

    IEnumerator FSM()
    {
        while (alive)
        {
            switch (state)
            {
                case State.PATROL:
                    Patrol();
                    break;
                case State.CHASE:
                    Chase();
                    break;
            }
            yield return null;
        }
    }

    void Patrol()
    {
        agent.speed = patrolSpeed;
        float distance = Vector3.Distance(this.transform.position, waypoints[waypointIndex].transform.position);
        if (distance >= 0.5)
        {
            agent.SetDestination(waypoints[waypointIndex].transform.position);
            character.Move(agent.desiredVelocity, false, false);
        } else if (distance <= 0.5)
        {
            waypointIndex += 1;
            if(waypointIndex >= waypoints.Length)
            {
                waypointIndex = 0;
            }
        }
        else
        {
            character.Move(Vector3.zero, false, false);
        }
    }

    void Chase()
    {
        agent.speed = chaseSpeed;

        agent.SetDestination(player.transform.position);
        character.Move(agent.desiredVelocity, false, false);
    }

    private void OnTriggerEnter(Collider collider)
    {
        // TODO change to spot player
        if(collider.tag == "Player")
        {
            state = GuardUTH.State.CHASE;
        }
    }

    void GetAllComponents()
    {
        //animator = GetComponent<Animator>();
        //player = GameObject.FindGameObjectWithTag("Player").transform;
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
        Gizmos.DrawRay(this.transform.position, this.transform.forward * viewDistance);
    }

    void SpotPlayer()
    {
        if (CanSeePlayer())
        {
            playerVisibleTimer += Time.deltaTime;
        }
        else
        {
            playerVisibleTimer -= Time.deltaTime;
        }

        playerVisibleTimer = Mathf.Clamp(playerVisibleTimer, 0, timeToSpotPlayer);
        spotlight.color = Color.Lerp(originalSpotlightColour, Color.red, playerVisibleTimer / timeToSpotPlayer);

        if (playerVisibleTimer >= timeToSpotPlayer)
        {
            //TODO Change state
            state = State.CHASE;
        }
    }

    bool CanSeePlayer()
    {
        if (Vector3.Distance(this.transform.position, player.transform.position) < viewDistance)
        {
            Vector3 dirToPlayer = (player.transform.position - this.transform.position).normalized;
            float angleBetweenGuardAndPlayer = Vector3.Angle(this.transform.forward, dirToPlayer);
            if (angleBetweenGuardAndPlayer < viewAngle / 2f)
            {
                if (!Physics.Linecast(this.transform.position, player.transform.position, viewMask))
                {
                    return true;
                }
            }
        }
        return false;
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

    IEnumerator TurnToFace(Vector3 lookTarget)
    {
        Vector3 dirToLookTarget = (lookTarget - transform.position).normalized;
        float targetAngle = 90 - Mathf.Atan2(dirToLookTarget.z, dirToLookTarget.x) * Mathf.Rad2Deg;

        while (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetAngle)) > 0.05f)
        {
            float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, turnSpeed * Time.deltaTime);
            transform.eulerAngles = Vector3.up * angle;
            yield return null;
        }
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

    void SpotPlayer()
    {
        if (CanSeePlayer())
            playerVisibleTimer += Time.deltaTime;
        else
            playerVisibleTimer -= Time.deltaTime;
   
        playerVisibleTimer = Mathf.Clamp(playerVisibleTimer, 0, timeToSpotPlayer);
        spotlight.color = Color.Lerp(originalSpotlightColour, Color.red, playerVisibleTimer / timeToSpotPlayer);

        if (playerVisibleTimer >= timeToSpotPlayer)
        {
            state = States.Alerted;
        }
    } 
    
    bool CanSeePlayer()
    {
        if (Vector3.Distance(transform.position, player.position) < viewDistance)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angleBetweenGuardAndPlayer = Vector3.Angle(transform.forward, dirToPlayer);
            if (angleBetweenGuardAndPlayer < viewAngle / 2f)
            {
                if (!Physics.Linecast(transform.position, player.position, viewMask))
                {
                    return true;
                }
            }
        }
        return false;
    }

    bool VisionObstructedToPosition(Vector3 position)
    {
        if(Physics.Linecast(transform.position, position, viewMask))
        {
            return true;
        }
        return false;
    }

    void GuardPatrolling()
    {
        AlertSetActive(false);
        if (!followPathCoroutineRunning)
        {
            StartCoroutine(FollowPath(GenerateWaypointPositions()));
        }
    }

    void GuardAlerted()
    {
        if(!moveToPositionCoroutineRunning)
        {
            StartCoroutine(MoveToPlayersLastKnownPosition(player.position));
        }
    }

    void GuardSearchingForPlayer()
    {
        if (!searchingForPlayerCoroutineRunning)
        {
            StartCoroutine(SearchForPlayer());
        }
    }

    void GuardApprehendingPlayer()
    {
        if (!apprehendingPlayerCouroutineRunning)
        {
            StartCoroutine(ApprehendingPlayer());
        }
    }
    */
}