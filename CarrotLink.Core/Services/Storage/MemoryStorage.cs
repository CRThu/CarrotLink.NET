using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services.Storage
{
    public class MemoryStorage : IDataStorage
    {
        private readonly ConcurrentQueue<byte[]> _buffer = new();

        public void StoreInMemory(byte[] data) => _buffer.Enqueue(data);

        public Task ExportAsJsonAsync(string filePath, object data)
        => throw new NotImplementedException();

        public Task ExportAsCsvAsync(string filePath, IEnumerable<object> records)
        => throw new NotImplementedException();
    }
}
