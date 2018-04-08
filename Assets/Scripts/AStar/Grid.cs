using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts.AStar
{
    /// <inheritdoc />
    /// <summary>
    /// Grid creates a world of nodes to be used with A* Pathfinding Algorithm
    /// </summary>
    public class Grid : MonoBehaviour
    {
        public bool DisplayGridGizmos;
        public LayerMask UnwalkableMask;
        public Vector2 GridSize;
        public float NodeRadius = 0.5f;

        public int MaxSize
        {
            get { return _gridSizeX * _gridSizeY; }
        }

        private Node[,] _grid;
        private float _nodeDiameter;
        private int _gridSizeX, _gridSizeY;

        [UsedImplicitly]
        private void Awake()
        {
            // World size parameters
            _nodeDiameter = NodeRadius * 2;
            _gridSizeX = Mathf.RoundToInt(GridSize.x / _nodeDiameter);
            _gridSizeY = Mathf.RoundToInt(GridSize.y / _nodeDiameter);

            CreateGrid();
        }

        /// <summary>
        /// Creates a grid full of nodes
        /// </summary>
        private void CreateGrid()
        {
            // New Array of nodes
            _grid = new Node[_gridSizeX, _gridSizeY];
            
            // Calculate the bottom left of the grid
            var bottomLeft = transform.position 
                - Vector3.right * GridSize.x / 2 
                - Vector3.forward * GridSize.y / 2;

            // loop each X,Y grid value
            for (var x = 0; x < _gridSizeX; x++) {
                for (var y = 0; y < _gridSizeY; y++){
                    // Calculate world position
                    var worldPosition = bottomLeft 
                        + Vector3.right * (x * _nodeDiameter + NodeRadius) 
                        + Vector3.forward * (y * _nodeDiameter + NodeRadius);

                    // Check for physical obstacles
                    var walkable = !Physics.CheckSphere(worldPosition, NodeRadius, UnwalkableMask);
                    // Check for empty ground
                    if (walkable) walkable = CheckForGround(worldPosition, NodeRadius);
                
                    // Create Calculated node for grid position x,y
                    _grid[x, y] = new Node(walkable, worldPosition, x, y);
                }
            }
        }

        /// <summary>
        /// Check for hit on ground at 'worldPosition'
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="radius"></param>
        /// <returns>true if hits ground</returns>
        public static bool CheckForGround(Vector3 worldPosition, float radius)
        {
            worldPosition.y += 1.0f;
            return Physics.Raycast(worldPosition, -Vector3.up);
        }

        /// <summary>
        /// Returns all the neighbours of the current node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public List<Node> GetNeighbours(Node node)
        {
            var neighbours = new List<Node>();
            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    // If current node continue
                    if (x == 0 && y == 0) continue;

                    // Calculate current neighbour position
                    var neighbourX = node.GridX + x;
                    var neighbourY = node.GridY + y;

                    // Check neighbour x,y is within grid size and 
                    if (neighbourX >= 0 && neighbourX < _gridSizeX && neighbourY >= 0 && neighbourY < _gridSizeY)
                        neighbours.Add(_grid[neighbourX, neighbourY]);
                }
            }
            return neighbours;
        }

        /// <summary>
        /// Calculates the grid position from a 'worldPosition'
        /// </summary>
        /// <param name="worldPosistion"></param>
        /// <returns>Node</returns>
        public Node GetNodeFromWorldPoint(Vector3 worldPosistion){
            // Reverse the world size calcualtions
            var x = Mathf.RoundToInt((_gridSizeX - 1) * Mathf.Clamp01((worldPosistion.x + GridSize.x / 2) / GridSize.x));
            var y = Mathf.RoundToInt((_gridSizeY - 1) * Mathf.Clamp01((worldPosistion.z + GridSize.y / 2) / GridSize.y));

            return _grid[x, y];
        }

        [UsedImplicitly]
        private void OnDrawGizmos()
        {
            // Draw wireframe of the grid size for easy editing
            Gizmos.DrawWireCube(transform.position, new Vector3(GridSize.x,1, GridSize.y));

            // Draws all the nodes in the grid
            HandleDrawGridNodes();
        }

        /// <summary>
        /// Handles the drawing of the nodes within the grid
        /// </summary>
        private void HandleDrawGridNodes()
        {
            // If no grid or DoNotDisplayGridGizmos then return
            if (_grid == null || !DisplayGridGizmos) return;

            foreach (var node in _grid)
            {
                // If walkable white, else red
                Gizmos.color = (node.Walkable) ? Color.white : Color.red;
                // Draw slightly smaller cube to differentiate between nodes
                Gizmos.DrawCube(node.WorldPosition, Vector3.one * (_nodeDiameter - 0.1f));
            }
        }
    }
}
