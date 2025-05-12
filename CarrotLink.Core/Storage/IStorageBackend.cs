namespace CarrotLink.Core.Storage
{
    public interface IStorageBackend<T>
    {
        public long Count { get; }

        void Enquene(T item);

        bool TryDequeue(out T? data);
    }
}