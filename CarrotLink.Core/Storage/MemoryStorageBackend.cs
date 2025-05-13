using System.Collections.Concurrent;

namespace CarrotLink.Core.Storage
{
    public class MemoryStorageBackend<T> : IStorageBackend<T>
    {
        private readonly ConcurrentQueue<T> _quene = new();
        public long Count => _quene.Count;


        public void Enquene(T item)
        {
            _quene.Enqueue(item);
        }

        public bool TryDequeue(out T? item)
        {
            return _quene.TryDequeue(out item);
        }

        public void Dispose()
        {
            _quene.Clear();
        }
    }
}