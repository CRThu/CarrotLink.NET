using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services.Storage
{
    public class MemoryStorage : IDataStorage
    {
        private readonly ConcurrentQueue<byte[]> _storageQueue = new();

        public Task SaveAsync(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _storageQueue.Enqueue(data);
            return Task.CompletedTask;
        }

        public Task ExportAsJsonAsync(string path)
        {
            var jsonData = JsonSerializer.Serialize(_storageQueue);
            return File.WriteAllTextAsync(path, jsonData);
        }

        // 辅助方法：获取内存中存储的数据
        public IEnumerable<byte[]> GetStoredData()
        {
            return _storageQueue.ToArray();
        }
    }
}
