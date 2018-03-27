using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;

namespace Assets.Scripts.AStar
{
    public class GridAgent : MonoBehaviour
    {
        private Vector3[] _path;
        private int _targetIndex;
        private ThirdPersonCharacter _character;
        private Coroutine _lastRoutine;
        private Rigidbody _rigidbody;
        private bool _smart;
        private Grid _grid;

        // Properties
        public bool HasPathFinished { get; private set; }
        public float Speed { get; set; }

        [UsedImplicitly]
        private void Awake()
        {
            _character = GetComponent<ThirdPersonCharacter>();
            _rigidbody = GetComponent<Rigidbody>();
            _grid = FindObjectOfType<Grid>();
        }

        /// <summary>
        /// Requests the destination from current position to target position using A* pathfinding
        /// </summary>
        /// <param name="currentPosition"></param>
        /// <param name="targetPosition"></param>
        /// <param name="smart"></param>
        public void RequestSetDestination(Vector3 currentPosition, Vector3 targetPosition, bool smart)
        {
            // Has not finished path
            HasPathFinished = false;

            // Stops all coroutines
            if (_lastRoutine != null)
                StopAllCoroutines();

            _smart = smart;
            // Requests A* Pathfinding algorithm then callbacks OnPathFound once finished
            PathRequestManager.RequestPath(new PathRequest(currentPosition, targetPosition, OnPathFound));
        }

        /// <summary>
        /// Checks if path has been found
        /// </summary>
        /// <returns></returns>
        public bool PathFound()
        {
            return _path != null && _path.Length > 0;
        }

        /// <summary>
        /// Once path found start following path
        /// </summary>
        /// <param name="newPath"></param>
        /// <param name="pathSuccessful"></param>
        public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
        {
            // Path found: check if successful
            if (!pathSuccessful)
            {
                HasPathFinished = true;
                return;
            }

            // If successful Dispose of old path and stop any coroutines
            _path = newPath;
            if (_lastRoutine != null)
                StopAllCoroutines();

            // Start coroutine to follow path
            _lastRoutine = StartCoroutine(FollowPath(_path));
        }

        /// <summary>
        /// Stop moving character
        /// </summary>
		public void StopMoving()
		{
		    Speed = 0;
            _character.Move(Vector3.zero, false, false);
            if (_lastRoutine != null)
                StopAllCoroutines();
		}

        public void Disable()
        {
            _rigidbody.isKinematic = true;
        }

        /// <summary>
        /// Follows the current path
        /// </summary>
        /// <returns></returns>
        [UsedImplicitly]
        private IEnumerator FollowPath(Vector3[] path)
        {
            if (!PathFound()) yield return null;

            // Set initial waypoint to first index of path
            _targetIndex = 0;
            var currentWaypoint = path[0];
            var seenGuard = false;
            var timer = 0f;

            while (!HasPathFinished)
            {
                // Check if reached current waypoint destination
                if (Vector3.Distance(this.transform.position, currentWaypoint) <= 0.1f)
                {
                    _targetIndex++;
                    // If reached last waypoint
                    if (_targetIndex >= path.Length)
                    {
                        HasPathFinished = true;
                        _targetIndex = 0;
                        break;
                    }
                    // Go to next waypoint
                    currentWaypoint = path[_targetIndex];
                }

                // Walk around other targets if smart
                if (_smart)
                {
                    if(seenGuard)
                        timer += Time.deltaTime;

                    RaycastHit hit;
                    if (Physics.Linecast(transform.position, transform.position + transform.forward * 4.5f, out hit)) { 
                        if((hit.transform.gameObject.tag == "Guard" || hit.transform.gameObject.tag == "Civilian") && !hit.collider.isTrigger)
                        {
                            if(!seenGuard) {
                                seenGuard = true;
                                for (var i = 0; i < 5; i++)
                                {
                                    var tempIndex = _targetIndex + i;
                                    if (tempIndex >= path.Length) continue;

                                    if (!_grid.GetNodeFromWorldPoint(path[tempIndex] + transform.right * 1f).Walkable)
                                    { 
                                        break;
                                    }

                                    switch (i)
                                    {
                                        case 0:
                                            path[tempIndex] += transform.right * 0.5f;
                                            break;
                                        case 1:
                                        case 2:
                                        case 3:
                                            path[tempIndex] += transform.right * 0.75f;
                                            break;
                                        case 4:
                                            path[tempIndex] += transform.right * 0.5f;
                                            break;
                                    }
                                }
                            }
                        }
                    }

                    if (timer >= 5f)
                    {
                        seenGuard = false;
                        timer = 0f;
                    }
                }

                // Move transform and character towards waypoint
				transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, Speed * Time.deltaTime);
				_character.Move((currentWaypoint - transform.position).normalized * Speed, false, false);
  
                yield return null;
            }
            yield return null;
        }

        /// <summary>
        /// Moves the object attacted straight to the target parameters position overtime
        /// </summary>
        /// <param name="target"></param>
        public void StraightToDestination(Vector3 target) {
            transform.position = Vector3.MoveTowards(transform.position, target, Speed * Time.deltaTime);
            _character.Move((target - transform.position).normalized * Speed, false, false);
        }

        public void OnDrawGizmos()
        {
            if (!PathFound()) return;

            Gizmos.color = Color.blue;
            for (var i = _targetIndex; i < _path.Length; i++)
            {
                // Draw a cube for each waypoint not yet reached
                Gizmos.DrawCube(_path[i], Vector3.one * 0.75f);
                // Draw a line from each waypoint to another
                Gizmos.DrawLine(i == _targetIndex ? transform.position : _path[i - 1], _path[i]);
            }
        }
    }
}