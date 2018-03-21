using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.AStar;
using JetBrains.Annotations;

public class Hearing : MonoBehaviour {
    public float HearingRadius;
	public LayerMask PlayerMask;
	public float LookDelay = 1.0f;

    private GameObject _player;
    private Animator _playerAnimator;
	private GridAgent _gridAgent;


	[HideInInspector]
	public List<Transform> HeardTargets = new List<Transform>();

	// Use this for initialization
    private void Awake () {
        _player = GameObject.FindGameObjectWithTag("Player");
        _playerAnimator = _player.GetComponent<Animator>();
		_gridAgent = GetComponent<GridAgent>();
	}

	[UsedImplicitly]
	private void Start()
	{
		StartCoroutine(FindTargetsWithDelay(LookDelay));
	}

	private IEnumerator FindTargetsWithDelay(float delay)
	{
		while (true)
		{
			yield return new WaitForSeconds(delay);
            FindHearingTargets();
		}
	}

	private void FindHearingTargets()
	{
        HeardTargets.Clear();

        // Find targets within sphere radius
        LocateHeardTargetsWithinSphere(Physics.OverlapSphere(transform.position, HearingRadius, PlayerMask));
	}

    private IEnumerable<Transform> LocateHeardTargetsWithinSphere(Collider[] targetsWithinSphere) {
        List<Transform> list = new List<Transform>();

        foreach(var target in targetsWithinSphere) {
            if (!_playerAnimator.GetBool("Crouch"))
            {
                float forwardSpeed = _playerAnimator.GetFloat("Forward");
                if (forwardSpeed >= 0.3f)
                {
                    var direction = (_player.transform.position - transform.position).normalized;
                    var distance = Vector3.Distance(transform.position, _player.transform.position);
                    if (Physics.Raycast(transform.position, direction, distance, PlayerMask))
                    {
                        HeardTargets.Add(_player.transform);
                    }
                    else
                    {
                        CalculatePath(transform.position, target.transform.position);
                    }
                } 
            }
        }
        return list;
    }

	public void CalculatePath(Vector3 currentPosition, Vector3 targetPosition)
	{
        PathRequestManager.RequestPath(new PathRequest(currentPosition, targetPosition, OnCalculatePathFound));
	}

	public void OnCalculatePathFound(Vector3[] path, bool pathSuccessful)
	{
        List<Vector3> waypoints = new List<Vector3>(path);
		// Adding current and target position to waypoints list
		waypoints.Insert(0, transform.position);

		var pathLength = 0.0f;
        for (int i = 0; i < waypoints.Count - 1; i++)
            pathLength += Vector3.Distance(waypoints[i], waypoints[i + 1]);
        
        if (pathLength <= 0.0f)
            return;

        float forwardSpeed = _playerAnimator.GetFloat("Forward");
        if (forwardSpeed <= 0.75f && forwardSpeed >= 0.3f)
        {
            float walkingRadius = HearingRadius * 0.5f;
            // Player Walking
            if (pathLength <= walkingRadius)
            {
                HeardTargets.Add(_player.transform);
            }
        }
        else if (forwardSpeed >= 0.76f)
        {
            // Player Running
            if (pathLength <= HearingRadius)
            {
                HeardTargets.Add(_player.transform);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = HeardTargets.Count > 0 ? Color.red : Color.blue;
        Gizmos.DrawWireSphere(transform.position, HearingRadius);
    }
}
