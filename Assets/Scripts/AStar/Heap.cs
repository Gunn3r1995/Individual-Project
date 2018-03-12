using System;

namespace Assets.Scripts.AStar
{
    public class Heap<T> where T : IHeapItem<T>
    {
        private readonly T[] _items;
        public int Count { get; private set; }

        /// <summary>
        /// Heap Constructor
        /// </summary>
        /// <param name="capacity"></param>
        public Heap(int capacity)
        {
            _items = new T[capacity];
        }

        /// <summary>
        /// Add item to Heap
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            // Add item to top off Heap
            item.HeapIndex = Count;
            _items[Count] = item;

            // Sort the item to ne in correct place
            SortUp(item);

            // Increment total count
            Count++;
        }

        /// <summary>
        /// Removes the first item from Heap and then re-sorts Heap
        /// </summary>
        /// <returns></returns>
        public T RemoveFirst()
        {
            // Get the first item
            var firstItem = _items[0];

            // Decrement total count
            Count--;

            // Put item at end of Heap to the first item
            _items[0] = _items[Count];
            _items[0].HeapIndex = 0;

            // Sort item at begining back to its correct position
            SortDown(_items[0]);

            // Return the first node back
            return firstItem;
        }

        /// <summary>
        /// Updates 'item' in the Heap
        /// </summary>
        /// <param name="item"></param>
        public void UpdateItem(T item)
        {
            SortUp(item);
        }

        /// <summary>
        /// Checks if Heap contains 'item'
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            return Equals(_items[item.HeapIndex], item);
        }

        /// <summary>
        /// Sort current item value down to correct position
        /// </summary>
        /// <param name="item"></param>
        private void SortDown(T item)
        {
            while (true)
            {
                var childIndexLeft = FindLeftChildIndex(item.HeapIndex);
                var childIndexRight = FindRightChildIndex(item.HeapIndex);

                // Check if left child index is within Heap Count
                if (childIndexLeft < Count)
                {
                    // Current swap index is left child
                    var swapIndex = childIndexLeft;
                    // Check if right child index is within Heap Count
                    if (childIndexRight < Count)
                    {
                        // Check if right child value is less than left child value
                        if (_items[childIndexLeft].CompareTo(_items[childIndexRight]) < 0)
                            // Current swap index is right child
                            swapIndex = childIndexRight;
                    }

                    // Check if swap index value is less than current value; if so swap
                    if (item.CompareTo(_items[swapIndex]) < 0)
                        Swap(item, _items[swapIndex]);
                    else
                        return;
                }
                else
                    return;
            }
        }

        /// <summary>
        /// Sort current item value up to correct position
        /// </summary>
        /// <param name="item"></param>
        private void SortUp(T item)
        {
            var parentIndex = FindParentIndex(item.HeapIndex);

            while (true)
            {
                // If current item greater than parent item, swap values 
                if (item.CompareTo(_items[parentIndex]) > 0)
                    Swap(item, _items[parentIndex]);
                else
                    break;
                // if swapped change parent index
                parentIndex = FindParentIndex(item.HeapIndex);
            }
        }

        /// <summary>
        /// Finds the parent item index of 'heapIndex'
        /// </summary>
        /// <param name="heapIndex"></param>
        /// <returns></returns>
        private static int FindParentIndex(int heapIndex)
        {
            return (heapIndex - 1) / 2;
        }

        /// <summary>
        /// Find the left child index of 'heapIndex'
        /// </summary>
        /// <param name="heapIndex"></param>
        /// <returns></returns>
        private static int FindLeftChildIndex(int heapIndex)
        {
            return heapIndex * 2 + 1;
        }

        /// <summary>
        /// Find the right child index of 'heapIndex'
        /// </summary>
        /// <param name="heapIndex"></param>
        /// <returns></returns>
        private static int FindRightChildIndex(int heapIndex)
        {
            return heapIndex * 2 + 2;
        }

        /// <summary>
        /// Swaps the indexes of the supplied items
        /// </summary>
        /// <param name="itemA"></param>
        /// <param name="itemB"></param>
        private void Swap(T itemA, T itemB)
        {
            // Set itemA index to value of itemB and vis versa
            _items[itemA.HeapIndex] = itemB;
            _items[itemB.HeapIndex] = itemA;
            
            // Store current itemA index
            var itemAIndex = itemA.HeapIndex;
            // swap item indexes
            itemA.HeapIndex = itemB.HeapIndex;
            itemB.HeapIndex = itemAIndex;
        }
    }

    public interface IHeapItem<T> : IComparable<T>
    {
        int HeapIndex { get; set; }
    }
}