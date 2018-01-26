using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guard : MonoBehaviour {
    //public static event System.Action OnGuardPatrolling;
    //public static event System.Action OnGuardAlerted;
    public static event System.Action OnGuardHasSpottedPlayer;
    //public static event System.Action OnGuardSearchingForPlayer;
    //public static event System.Action OnGuardApprehendingPlayer;

    public enum States { patrolling, alerted, spottedPlayer, searchingForPlayer, ApprehendingPlayer}
    States state = States.patrolling;

    public float walkSpeed = 2;
    public float runSpeed = 6;
    public float waitTime = 0.3f;
    public float turnSpeed = 90f;
    public float timeToSpotPlayer = 0.5f;
    public float speedSmoothTime = 0.1f;

    public Light spotlight;
    public float viewDistance = 10f;
    public LayerMask viewMask;
    //public GameObject alert;

    float viewAngle;
    float playerVisibleTimer;

    Animator animator;
    public Transform pathHolder;
    Transform player;
    Color originalSpotlightColour;

    bool followPathCoroutineRunning;
    int targetWaypointIndex;

    void Start ()
    {
        GetAllComponents();

        //Spotlight view angle and colour
        viewAngle = spotlight.spotAngle;
        originalSpotlightColour = spotlight.color;

        GuardPatrolling();
    }

    void Update()
    {
        SpotPlayer();

        switch (state)
        {
            case States.patrolling:
                GuardPatrolling();
                break;
            case States.alerted:
                GuardAlerted();
                break;
            case States.spottedPlayer:
                GuardHasSpottedPlayer();
                break;
            case States.searchingForPlayer:
                break;
            case States.ApprehendingPlayer:
                break;
        };
    }

    #region util

    void GetAllComponents()
    {
        animator = GetComponent<Animator>();
        player = GameObject.FindObjectOfType<PlayerController>().transform;
        //player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    Vector3[] GenerateWaypointPositions()
    {
        Vector3[] waypoints = new Vector3[pathHolder.childCount];
        for (int i = 0; i < waypoints.Length; i++)
        {
            waypoints[i] = pathHolder.GetChild(i).position;
            waypoints[i] = new Vector3(waypoints[i].x, transform.position.y, waypoints[i].z);
        }
        return waypoints;
    }

    void OnDrawGizmos()
    {
        Vector3 startPosition = pathHolder.GetChild(0).position;
        Vector3 previousPosition = startPosition;

        foreach (Transform waypoint in pathHolder)
        {
            Gizmos.DrawSphere(waypoint.position, 0.3f);
            Gizmos.DrawLine(previousPosition, waypoint.position);
            previousPosition = waypoint.position;
        }
        Gizmos.DrawLine(previousPosition, startPosition);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * viewDistance);
    }

    #endregion

    #region Movement

    IEnumerator FollowPath(Vector3[] waypoints)
    {
        print("FollowPath: starting patrolling coroutine");
        followPathCoroutineRunning = true;

        Vector3 targetWaypoint = waypoints[targetWaypointIndex];
        transform.LookAt(targetWaypoint);

        while(state == States.patrolling)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWaypoint, walkSpeed * Time.deltaTime);
            animator.SetFloat("speedPercent", 0.5f, speedSmoothTime, Time.deltaTime);

            if (transform.position == targetWaypoint)
            {
                //Update Waypoint
                targetWaypointIndex = (targetWaypointIndex + 1) % waypoints.Length;
                targetWaypoint = waypoints[targetWaypointIndex];

                //Wait for X seconds
                animator.SetFloat("speedPercent", 0);
                yield return new WaitForSeconds(waitTime);

                //Move to new target
                animator.SetFloat("speedPercent", 0.5f);
                yield return StartCoroutine(TurnToFace(targetWaypoint));
            }
            yield return null;
        }
        followPathCoroutineRunning = false;
    }

    IEnumerator TurnToFace(Vector3 lookTarget)
    {
        animator.SetFloat("speedPercent", 0, speedSmoothTime, Time.deltaTime);
        Vector3 dirToLookTarget = (lookTarget - transform.position).normalized;
        float targetAngle = 90 - Mathf.Atan2(dirToLookTarget.z, dirToLookTarget.x) * Mathf.Rad2Deg;

        while (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetAngle)) > 0.05f)
        {
            float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, turnSpeed * Time.deltaTime);
            transform.eulerAngles = Vector3.up * angle;
            yield return null;
        }
    }

    #endregion

    #region Player Detection

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
            state = States.alerted;
            //if (OnGuardHasSpottedPlayer != null) { OnGuardHasSpottedPlayer(); }
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

    #endregion

    void GuardPatrolling()
    {
        if (!followPathCoroutineRunning)
        {
            StartCoroutine(FollowPath(GenerateWaypointPositions()));
        }
    }

    void GuardAlerted()
    {
        print("Guard Alerted");
        
        //Change Colour of Alert
        
        //alert.SetActive(true);
        if (CanSeePlayer())
        {
            Vector3 playerLastSceenPosition = player.position;
            transform.position = Vector3.MoveTowards(transform.position, playerLastSceenPosition, runSpeed * Time.deltaTime);
            animator.SetFloat("speedPercent", 1.0f, speedSmoothTime, Time.deltaTime);
        } else
        {
           // alert.SetActive(false);
            state = States.patrolling;
        }
    }

    void GuardHasSpottedPlayer()
    {
        print("Spotted Player");
    }
}
