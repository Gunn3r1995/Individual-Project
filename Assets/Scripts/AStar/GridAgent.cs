using System;
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
        private bool _pathFinished;

        private void Awake()
        {
            _character = GetComponent<ThirdPersonCharacter>();
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

        public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
        {
            if (!pathSuccessful)
            {
                _pathFinished = true;
                return;
            }

            _path = newPath;
            StopCoroutine(FollowPath());
            StartCoroutine(FollowPath());
        }

		public void StopMoving()
		{
		    _speed = 0;
            _character.Move(Vector3.zero, false, false);
		}

        public void StopMoving(bool crouch, bool jump){
            SetSpeed(0);
            _character.Move(Vector3.zero, crouch, jump);
        }

        public void ClearPath()
        {
            _pathFinished = true;
            StopCoroutine("FollowPath");
        }

        private IEnumerator FollowPath()
        {
            Vector3 currentWaypoint = _path[0];

            while (true)
            {
                if (Vector3.Distance(transform.position, currentWaypoint) <= 0.2f)
                {
                    _targetIndex++;
                    if (_targetIndex >= _path.Length)
                    {
                        print("Destination Reached");
                        _pathFinished = true;
                        _targetIndex = 0;
                        yield break;
                    }
                    currentWaypoint = _path[_targetIndex];
                }

                transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, _speed * Time.deltaTime);
                _character.Move((currentWaypoint - transform.position).normalized * _speed, false, false);
                yield return null;
            }
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
