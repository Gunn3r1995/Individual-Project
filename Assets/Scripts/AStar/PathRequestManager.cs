using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using JetBrains.Annotations;

namespace Assets.Scripts.AStar
{
    public class PathRequestManager : MonoBehaviour
    {
        private Queue<PathResult> _results = new Queue<PathResult>();

        private static PathRequestManager _instance;
        private Pathfinding _pathfinding;


        private void Awake()
        {
            _instance = this;
            _pathfinding = GetComponent<Pathfinding>();
        }

        [UsedImplicitly]
        private void Update()
        {
            lock (_results)
            {
                if (_results != null && _results.Count <= 0) return;
                var itemsInQueue = _results.Count;
                for (var i = 0; i < itemsInQueue; i++)
                {
                    var result = _results.Dequeue();
                    result.Callback(result.Path, result.Success);
                }
            }
        }

        public static void RequestPath(PathRequest request)
        {
            ThreadStart threadStart = delegate
            {
                _instance._pathfinding.FindPath(request, _instance.FinishedProcessingPath);
            };
            threadStart.Invoke();
        }

        public void FinishedProcessingPath(PathResult result)
        {
            lock (_results)
            {
                _results.Enqueue(result);
            }
        }

		public static void RequestCalculatePath(PathRequest request)
		{
			ThreadStart threadStart = delegate
			{
                _instance._pathfinding.FindPath(request, _instance.FinishedProcessingCalculatePath);
			};
			threadStart.Invoke();
		}

		public void FinishedProcessingCalculatePath(PathResult result)
		{
			lock (_results)
			{
				_results.Enqueue(result);
			}
		}
    }
}

public struct PathResult
{
    public Vector3[] Path;
    public bool Success;
    public Action<Vector3[], bool> Callback;

    public PathResult(Vector3[] path, bool success, Action<Vector3[], bool> callback)
    {
        Path = path;
        Success = success;
        Callback = callback;
    }

}

public struct PathRequest
{
    public Vector3 PathStart;
    public Vector3 PathEnd;
    public Action<Vector3[], bool> Callback;

    public PathRequest(Vector3 start, Vector3 end, Action<Vector3[], bool> callback)
    {
        PathStart = start;
        PathEnd = end;
        Callback = callback;
    }
}