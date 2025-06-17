namespace CarrotLink.Core.Storage
{
    public interface IStorageNew<T> : IDisposable
    {
        public long Count { get; }

        void Write(T item);

        public IReadOnlyList<T> GetAll();
    }

    public interface IStorageBackend<T> : IDisposable
    {
        public long Count { get; }

        void Write(T item);

        bool TryRead(out T? data);

        Task<T> ReadAsync(CancellationToken cancellationToken = default);
    }
}