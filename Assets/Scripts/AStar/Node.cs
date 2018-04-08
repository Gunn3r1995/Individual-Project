using UnityEngine;

namespace Assets.Scripts.AStar
{
    public class Node : IHeapItem<Node> {
        public bool Walkable;
        public Vector3 WorldPosition;
        public int GridX;
        public int GridY;

        public int GCost;
        public int HCost;
        public int FCost
        {
            get { return GCost + HCost; }
        }

        public int HeapIndex { get; set; }
        public Node Parent;

        /// <summary>
        /// Node Constructor
        /// </summary>
        /// <param name="walkable"></param>
        /// <param name="worldPosition"></paramt>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY)
        {
            Walkable = walkable;
            WorldPosition = worldPosition;
            GridX = gridX;
            GridY = gridY;
        }

        /// <summary>
        /// Compares supplied nodes FCost and HCost to current node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public int CompareTo(Node node)
        {
            var compare = FCost.CompareTo(node.FCost);
            if (compare == 0) compare = HCost.CompareTo(node.HCost);
            return -compare;
        }
    }
}
