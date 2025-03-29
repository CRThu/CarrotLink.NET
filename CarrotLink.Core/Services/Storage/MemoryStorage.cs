using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Utility;
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
        private readonly ConcurrentQueue<IPacket> _storageQueue = new();

        public Task SaveAsync(IPacket? packet)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            _storageQueue.Enqueue(packet);
            return Task.CompletedTask;
        }

        public Task ExportAsJsonAsync(string path)
        {
            var p = _storageQueue.Select(p => p.ToString()).ToArray();
            var jsonData = JsonSerializer.Serialize(p);
            return File.WriteAllTextAsync(path, jsonData);
        }

        // 辅助方法：获取内存中存储的数据
        public IEnumerable<IPacket> GetStoredData()
        {
            return _storageQueue.ToArray();
        }
    }
}
