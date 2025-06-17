using System.Threading.Channels;

namespace CarrotLink.Core.Storage
{
    /// <summary>
    /// 基于 Channel 的高并发存储后端实现
    /// </summary>
    public sealed class ListStorageBackend<T> : IStorageNew<T>
    {
        private readonly Channel<T> _channel;
        private readonly List<T> _storage = new();
        private volatile bool _isDisposed;
        private readonly Task _processingTask;
        private readonly CancellationTokenSource _linkedCts;

        public long Count => _storage.Count;

        public ListStorageBackend(int? boundedCapacity = null, CancellationToken cancellationToken = default)
        {
            // 支持有界/无界两种模式
            _channel = boundedCapacity.HasValue
                ? Channel.CreateBounded<T>(new BoundedChannelOptions(boundedCapacity.Value)
                {
                    FullMode = BoundedChannelFullMode.Wait, // 背压控制
                    SingleReader = false,
                    SingleWriter = false
                })
                : Channel.CreateUnbounded<T>(new UnboundedChannelOptions
                {
                    AllowSynchronousContinuations = true // 性能优化
                });

            _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _processingTask = Task.Run(() => ProcessItemsAsync(cancellationToken));
        }

        public void Write(T item)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ListStorageBackend<T>));

            if (!_channel.Writer.TryWrite(item))
            {
                // 只有在通道已关闭时才会失败
                throw new InvalidOperationException("Channel is completed");
            }
        }

        private async Task ProcessItemsAsync(CancellationToken ct)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ListStorageBackend<T>));

            try
            {
                await foreach (var item in _channel.Reader.ReadAllAsync(ct))
                {
                    _storage.Add(item);
                }
            }
            catch (ChannelClosedException) when (_isDisposed)
            {
                Console.WriteLine($"{nameof(ListStorageBackend<T>)}.{nameof(ProcessItemsAsync)}() channel closed when isDisposed");
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine($"{nameof(ListStorageBackend<T>)}.{nameof(ProcessItemsAsync)}() cancelled");
            }
        }

        public IReadOnlyList<T> GetAll()
        {
            return _storage.ToArray();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            try
            {
                _channel.Writer.Complete(); // 关闭通道
                _linkedCts.Cancel();
                _processingTask.Wait();
            }
            catch (ChannelClosedException)
            {
                Console.WriteLine($"{nameof(ListStorageBackend<T>)}.{nameof(Dispose)}() channel closed");
            }
            catch (AggregateException ae) when (ae.InnerException is TaskCanceledException)
            {
                Console.WriteLine($"{nameof(ListStorageBackend<T>)}.{nameof(Dispose)}() task cancelled");
            }
            _linkedCts.Dispose();
        }
    }
}