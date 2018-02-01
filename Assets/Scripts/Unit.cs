using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.AStar;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;

namespace Assets.Scripts
{
    public class Unit : MonoBehaviour
    {
        public Transform Target;
        private Vector3[] _path;
        private int _targetIndex;
        private float _currentSpeed;

        public enum State { Patrol, Alert, Search, Chase }
        public State state;

        private ThirdPersonCharacter _character;
        private bool _patrolling;

        // Patrolling
        public GameObject[] Waypoints;
        public bool RandomWaypoints;
        public float PatrolSpeed = 1.0f;
        private int _waypointIndex = 0;

        private void Start()
        {
            _character = GetComponent<ThirdPersonCharacter>();
            _currentSpeed = PatrolSpeed;
            state = State.Patrol;

            //PathRequestManager.RequestPath(this.transform.position, Target.position, OnPathFound);
        }

        private void Update()
        {
            FSM();
        }

        private void FSM()
        {
            switch (state)
            {
                case State.Patrol:
                    if (!_patrolling)
                    {
                        //if (randomWaypoints)
                        //{
                        //    StartCoroutine(RandomPatrol());
                        //}
                        StartCoroutine(Patrol());
                    }
                    break;
                case State.Alert:
                    break;
                case State.Search:
                    print("Searching");
                    break;
                case State.Chase:
                    print("Chase State");
                    break;
            }
        }

        public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
        {
            if (!pathSuccessful) return;

            _path = newPath;
            StopCoroutine("FollowPath");
            StartCoroutine("FollowPath");
        }

        private IEnumerator Patrol()
        {
            print("Patrolling");
            _patrolling = true;

            while (state == State.Patrol)
            {
                _currentSpeed = PatrolSpeed;
                var distance = Vector3.Distance(transform.position, Waypoints[_waypointIndex].transform.position);
                if (distance >= 2.0f)
                {
                    PathRequestManager.RequestPath(transform.position, Waypoints[_waypointIndex].transform.position, OnPathFound);
                }
                if (distance <= 2.0f)
                {
                    _waypointIndex += 1;
                    if (_waypointIndex >= Waypoints.Length)
                    {
                        _waypointIndex = 0;
                    }
                    PathRequestManager.RequestPath(transform.position, Waypoints[_waypointIndex].transform.position, OnPathFound);
                }
                else
                {
                    _character.Move(Vector3.zero, false, false);
                }

                yield return null;
            }

            _patrolling = false;
        }

        private IEnumerator FollowPath()
        {
            var currentWaypoint = _path[0];

            while (true)
            {
                if (transform.position == currentWaypoint)
                {
                    _targetIndex++;
                    if (_targetIndex >= _path.Length)
                    {
                        yield break;
                    }
                    currentWaypoint = _path[_targetIndex];
                }

                transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, _currentSpeed * Time.deltaTime);
                _character.Move((currentWaypoint - transform.position).normalized * _currentSpeed, false, false);
                yield return null;
            }
        }

        public void OnDrawGizmos()
        {
            if (_path == null) return;

            var startPosition = Waypoints[0].transform.position;
            var previousPosition = startPosition;

            foreach (var waypoint in Waypoints)
            {
                Gizmos.DrawSphere(waypoint.transform.position, 0.3f);
                Gizmos.DrawLine(previousPosition, waypoint.transform.position);
                previousPosition = waypoint.transform.position;
            }

            Gizmos.DrawLine(previousPosition, startPosition);

            for (var i = _targetIndex; i < _path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(_path[i], Vector3.one);

                Gizmos.DrawLine(i == _targetIndex ? transform.position : _path[i - 1], _path[i]);
            }
        }
    }
}
