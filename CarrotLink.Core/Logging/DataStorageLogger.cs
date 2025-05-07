using CarrotLink.Core.Protocols.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CarrotLink.Core.Logging
{
    public class DataStorageLogger : ILogger, IPacketLogger
    {
        private static readonly string commandStorageKey = "_command";
        private static readonly string dataStorageKey = "_data";

        private readonly ConcurrentDictionary<string, ConcurrentQueue<IPacket>> _storage;

        public DataStorageLogger()
        {
            _storage = new ConcurrentDictionary<string, ConcurrentQueue<IPacket>>();
        }

        public void HandlePacket(IPacket packet)
        {
            var quene = _storage.GetOrAdd(
                packet.Type == PacketType.Command ? commandStorageKey : dataStorageKey,
                _ => new ConcurrentQueue<IPacket>());
            quene.Enqueue(packet);
        }

        public bool TryGetCommandPacket(out IPacket? packet)
        {
            if (_storage.TryGetValue(commandStorageKey, out var quene))
            {
                return quene.TryDequeue(out packet);
            }
            packet = default;
            return false;
        }

        public bool TryGetDataPacket(out IPacket? packet)
        {
            if (_storage.TryGetValue(dataStorageKey, out var quene))
            {
                return quene.TryDequeue(out packet);
            }
            packet = default;
            return false;
        }

        //public async Task ExportAsJsonAsync(string path)
        //{
        //    var p = _storage.Select(p => p.ToString()).ToArray();
        //    var jsonData = JsonSerializer.Serialize(p);
        //    await File.WriteAllTextAsync(path, jsonData);
        //}
    }
}
