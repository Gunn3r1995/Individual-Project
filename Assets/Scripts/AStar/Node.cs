using UnityEngine;

namespace Assets.Scripts.AStar
{
    public class Node : IHeapItem<Node> {
        public bool walkable;
        public Vector3 WorldPosition;
        public int gridX;
        public int gridY;

        public int GCost;
        public int HCost;
        public int FCost
        {
            get { return GCost + HCost; }
        }

        public Node Parent;
        private int heapIndex;

        public Node(bool _walkable, Vector3 _worldPosition, int _gridX, int _gridY)
        {
            walkable = _walkable;
            WorldPosition = _worldPosition;
            gridX = _gridX;
            gridY = _gridY;
        }

        public int HeapIndex
        {
            get { return heapIndex; }
            set { heapIndex = value; }
        }

        public int CompareTo(Node nodeToCompare)
        {
            int compare = FCost.CompareTo(nodeToCompare.FCost);
            if (compare == 0)
            {
                compare = HCost.CompareTo(nodeToCompare.HCost);
            }
            return -compare;
        }
    }
}
