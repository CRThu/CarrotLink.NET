using System.Collections.Concurrent;

namespace CarrotLink.Core.Storage
{
    public class MemoryStorageBackend<T> : IStorageBackend<T>
    {
        private readonly ConcurrentQueue<T> _quene = new();
        private readonly SemaphoreSlim _dataAvailableSignal = new SemaphoreSlim(0);
        public long Count => _quene.Count;


        public void Write(T item)
        {
            _quene.Enqueue(item);
            _dataAvailableSignal.Release();
        }

        public bool TryRead(out T? item)
        {
            return _quene.TryDequeue(out item);
        }

        public async Task<T> ReadAsync(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                if (_quene.TryDequeue(out var item))
                {
                    return await Task.FromResult(item);
                }

                try
                {
                    await _dataAvailableSignal.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    if (_quene.TryDequeue(out item))
                    {
                        return await Task.FromResult(item);
                    }
                    throw;
                }
            }
        }

        public void Dispose()
        {
            _dataAvailableSignal?.Release();
            _quene.Clear();
        }
    }
}