using System.Collections;
using System.Collections.Generic;
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

        // Properties
        public bool HasPathFinished { get; private set; }
        public float Speed { get; set; }

        [UsedImplicitly]
        private void Awake()
        {
            _character = GetComponent<ThirdPersonCharacter>();
        }

        public void SetDestination(Vector3 currentPosition, Vector3 targetPosition)
        {
            print("Setting Destinaton");
            HasPathFinished = false;

            if (_lastRoutine != null)
                StopAllCoroutines();
            
            PathRequestManager.RequestPath(new PathRequest(currentPosition, targetPosition, OnPathFound));
        }

        public bool PathFound()
        {
            return _path != null && _path.Length > 0;
        }

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
            _lastRoutine = StartCoroutine("FollowPath");
        }

		public void StopMoving()
		{
		    Speed = 0;
            _character.Move(Vector3.zero, false, false);
            if (_lastRoutine != null)
                StopAllCoroutines();
		}

        [UsedImplicitly]
        private IEnumerator FollowPath()
        {
            var currentWaypoint = _path[0];

            while (true)
            {
                if (Vector3.Distance(transform.position, currentWaypoint) <= 0.2f)
                {
                    _targetIndex++;
                    if (_targetIndex >= _path.Length)
                    {
                        HasPathFinished = true;
                        _targetIndex = 0;
                        break;
                    }
                    currentWaypoint = _path[_targetIndex];
                }

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

            for (var i = _targetIndex; i < _path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(_path[i], Vector3.one);

                Gizmos.DrawLine(i == _targetIndex ? transform.position : _path[i - 1], _path[i]);
            }
        }
    }
}