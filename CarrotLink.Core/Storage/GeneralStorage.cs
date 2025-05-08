using CarrotLink.Core.Logging;
using CarrotLink.Core.Protocols.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CarrotLink.Core.Storage
{
    public interface IStorage<T> : IPacketLogger
    {
        public long Count { get; }
        public bool TryRead(out T? packet);
    }

    public class GeneralStorage<T> : IStorage<T>
    {
        private readonly ConcurrentQueue<T> _storage;
        private Func<IPacket, bool>? _filter;
        private Func<IPacket, T> _converter;

        public long Count => _storage.Count;

        public GeneralStorage(Func<IPacket, T> converter, Func<IPacket, bool>? filter = default)
        {
            _storage = new ConcurrentQueue<T>();
            _converter = converter;
            _filter = filter;
        }

        public void HandlePacket(IPacket packet)
        {
            if (_filter != null && _filter(packet))
            {
                _storage.Enqueue(_converter(packet));
            }
        }

        public bool TryRead(out T? data)
        {
            return _storage.TryDequeue(out data);
        }
    }

    public class CommandStorage : GeneralStorage<string>
    {
        public CommandStorage() : base(p => p.ToString(), p => p.Type == PacketType.Command)
        {

        }
    }
}
