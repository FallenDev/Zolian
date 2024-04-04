namespace Darkages.Types
{
    internal abstract class BinaryHeap<T> where T : IComparable<T>
    {
        private readonly List<T> _heap = [];

        public int Count => _heap.Count;

        public void Add(T item)
        {
            _heap.Add(item);
            HeapifyUp(_heap.Count - 1);
        }

        public bool Contains(T item) => _heap.Contains(item);

        public T TakeRoot()
        {
            if (_heap.Count == 0) return default;
            var root = _heap[0];
            _heap[0] = _heap[^1];
            _heap.RemoveAt(_heap.Count - 1);
            HeapifyDown(0);
            return root;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                var parentIndex = (index - 1) / 2;
                if (_heap[index].CompareTo(_heap[parentIndex]) < 0)
                {
                    Swap(index, parentIndex);
                    index = parentIndex;
                }
                else break;
            }
        }

        private void HeapifyDown(int index)
        {
            while (true)
            {
                var leftChild = 2 * index + 1;
                var rightChild = 2 * index + 2;
                var smallest = index;

                if (leftChild < _heap.Count && _heap[leftChild].CompareTo(_heap[smallest]) < 0) smallest = leftChild;
                if (rightChild < _heap.Count && _heap[rightChild].CompareTo(_heap[smallest]) < 0) smallest = rightChild;
                if (smallest != index)
                {
                    Swap(index, smallest);
                    index = smallest;
                    continue;
                }

                break;
            }
        }

        private void Swap(int index1, int index2) => (_heap[index1], _heap[index2]) = (_heap[index2], _heap[index1]);
    }
}
