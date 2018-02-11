using System;
using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;

namespace Assets.Scripts.AStar
{
    public class GridAgent : MonoBehaviour
    {
        private Vector3[] _path;
        private int _targetIndex;
        private float _speed;
        private ThirdPersonCharacter _character;


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

        public void SetDestination(Vector3 currentPosition, Vector3 targetPosition) {
            PathRequestManager.RequestPath(currentPosition, targetPosition, this.OnPathFound);
        }

        public void OnPathFound(Vector3[] newPath, bool pathSuccessful)
        {
            if (!pathSuccessful) return;

            _path = newPath;
            StopCoroutine("FollowPath");
            StartCoroutine("FollowPath");
        }

		public void StopMoving()
		{
            SetSpeed(0);
            _character.Move(Vector3.zero, false, false);
		}

        //public IEnumerator StopMoving(float waitTime)
        //{
        //    var timer = 0f;
        //    while (timer <= waitTime)
        //    {
        //        SetSpeed(0);
        //        _character.Move(Vector3.zero, false, false);
        //        timer += Time.deltaTime;
        //        yield return null;
        //    }
        //}

        public void StopMoving(bool crouch, bool jump){
            SetSpeed(0);
            _character.Move(Vector3.zero, crouch, jump);
        }

        private IEnumerator FollowPath()
        {
            if (_path.Length >= 1)
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

                    transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, _speed * Time.deltaTime);
                    _character.Move((currentWaypoint - transform.position).normalized * _speed, false, false);
                    yield return null;
                }
            }
        }

        public void OnDrawGizmos()
        {
            if (_path == null) return;

            for (var i = _targetIndex; i < _path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(_path[i], Vector3.one);

                Gizmos.DrawLine(i == _targetIndex ? transform.position : _path[i - 1], _path[i]);
            }
        }
    }
}
