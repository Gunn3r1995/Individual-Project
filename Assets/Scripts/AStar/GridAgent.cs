﻿using System;
using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;

namespace Assets.Scripts.AStar
{
    public class GridAgent : MonoBehaviour
    {
        private Vector3[] _path;
        private float _speed;
        private int _targetIndex = 0;
        private ThirdPersonCharacter _character;
        private Rigidbody _rigidbody;
        private bool _pathFinished;
        private bool _following;
        Coroutine lastRoutine = null;

        private void Awake()
        {
            _character = GetComponent<ThirdPersonCharacter>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        public float GetSpeed() {
            return _speed;
        }

        public void SetSpeed(float speed) {
            this._speed = speed;
        }

        public void SetDestination(Vector3 currentPosition, Vector3 targetPosition)
        {
            _pathFinished = false;
            PathRequestManager.RequestPath(new PathRequest(currentPosition, targetPosition, this.OnPathFound));
        }

        public bool PathFound() {
            if(_path != null && _path.Length > 0) {
                return true;
            }
            return false;
        }

        public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
        {
            print("Path Found");
            if (!pathSuccessful)
            {
                _pathFinished = true;
                return;
            }

            _path = newPath;
            if (lastRoutine != null)
                StopAllCoroutines();
 
            lastRoutine = StartCoroutine("FollowPath");
        }

		public void StopMoving()
		{
		    _speed = 0;
            _character.Move(Vector3.zero, false, false);
            if (lastRoutine != null)
                StopAllCoroutines();
		}

        private IEnumerator FollowPath()
        {
            //_following = true;
            Vector3 currentWaypoint = _path[0];

            while (true)
            {
                if (Vector3.Distance(transform.position, currentWaypoint) <= 0.2f)
                {
                    _targetIndex++;
                    if (_targetIndex >= _path.Length)
                    {
                        _pathFinished = true;
                        _targetIndex = 0;
                        break;
                    }
                    currentWaypoint = _path[_targetIndex];
                }

				transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, _speed * Time.deltaTime);
				_character.Move((currentWaypoint - transform.position).normalized * _speed, false, false);
  
                yield return null;
            }
            yield return null;
        }

        public void StraightToDestination(Vector3 target) {
            transform.position = Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime);
            _character.Move((target - transform.position).normalized * _speed, false, false);
        }

        public bool HasPathFinished()
        {
            return _pathFinished;
        }

        public void OnDrawGizmos()
        {
            if (_path == null) return;

            for (var i = _targetIndex; i < _path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(_path[i], Vector3.one);

                if (i == _targetIndex)
                {
                    Gizmos.DrawLine(transform.position, _path[i]);
                }
                else
                {
                    Gizmos.DrawLine(_path[i - 1], _path[i]);
                }
            }
        }
    }
}
