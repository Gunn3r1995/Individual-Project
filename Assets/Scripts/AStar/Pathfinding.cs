using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts.AStar
{
    public class Pathfinding : MonoBehaviour
    {
        private Grid _grid;

        [UsedImplicitly]
        private void Awake()
        {
            _grid = GetComponent<Grid>();
        }

        public void FindPath(PathRequest request, Action<PathResult> callback)
        {
            var waypoints = new Vector3[0];
            var pathSuccess = false;

            var startNode = _grid.GetNodeFromWorldPoint(request.PathStart);
            var targetNode = _grid.GetNodeFromWorldPoint(request.PathEnd);

            if (startNode == targetNode) pathSuccess = true;
            else if (startNode.Walkable && targetNode.Walkable)
            {
                var openSet = new Heap<Node>(_grid.MaxSize);
                var closedSet = new HashSet<Node>();

                openSet.Add(startNode);

                while (openSet.Count > 0)
                {
                    var currentNode = openSet.RemoveFirst();
                    closedSet.Add(currentNode);

                    if (currentNode == targetNode)
                    {
                        pathSuccess = true;
                        break;
                    }

                    foreach (var neighbour in _grid.GetNeighbours(currentNode))
                    {
                        if (!neighbour.Walkable || closedSet.Contains(neighbour)) continue;

                        var newMovementCostToNeighbour = currentNode.GCost + GetDistance(currentNode, neighbour);
                        if (newMovementCostToNeighbour >= neighbour.GCost && openSet.Contains(neighbour)) continue;

                        neighbour.GCost = newMovementCostToNeighbour;
                        neighbour.HCost = GetDistance(neighbour, targetNode);
                        neighbour.Parent = currentNode;

                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                        else
                            openSet.UpdateItem(neighbour);
                    }
                }
            }

            if (pathSuccess)
            {
                waypoints = RetracePath(startNode, targetNode);
                pathSuccess = waypoints.Length > 0;
            }
            
            callback(new PathResult(waypoints, pathSuccess, request.Callback));
        }

        /// <summary>
        /// Retrace the path from finish-start to start-finish
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        private static Vector3[] RetracePath(Node startNode, Node targetNode)
        {
            var path = new List<Node>();
            var currentNode = targetNode;

            // Add all paths in correct Start to Finish order
            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
            }

            // Create WorldPosition waypoints
            var waypoints = ConvertGridPositionsToWorldPositions(path);
            // Reverse them to the correct order
            Array.Reverse(waypoints);
            return waypoints;
        }

        /// <summary>
        /// Converts a list of nodes to an array of world positions
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static Vector3[] ConvertGridPositionsToWorldPositions(IEnumerable<Node> path)
        {
            return path.Select(node => node.WorldPosition).ToArray();
        }

        private static int GetDistance(Node nodeA, Node nodeB)
        {
            var distanceX = Mathf.Abs(nodeA.GridX - nodeB.GridX);
            var distanceY = Mathf.Abs(nodeA.GridY - nodeB.GridY);

            if (distanceX > distanceY)
            {
                return 14 * distanceY + 10 * (distanceX - distanceY);
            }
            return 14 * distanceX + 10 * (distanceY - distanceX);
        }
    }
}
