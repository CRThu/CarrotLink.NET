namespace CarrotLink.Core.Storage
{
    public interface IStorageNew<T> : IDisposable
    {
        public int Count { get; }

        public T this[int index] { get; }

        void Write(T item);

        public bool TryRead(int index, out T? data);

        public void Clear();

        public IReadOnlyList<T> ToArray();
    }

    public interface IStorageBackend<T> : IDisposable
    {
        public long Count { get; }

        void Write(T item);

        bool TryRead(out T? data);

        Task<T> ReadAsync(CancellationToken cancellationToken = default);
    }
}