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
    public class GeneralStorage<T> : IPacketLogger, IDisposable
    {
        private readonly IStorageNew<T> _backend;
        private Func<IPacket, bool>? _filter;
        private Func<IPacket, T> _converter;

        private int _cursor = 0;

        public int Count => _backend.Count;
        public T this[int index] => _backend[index];

        public GeneralStorage(Func<IPacket, T> converter,
            Func<IPacket, bool>? filter = default,
            IStorageNew<T>? backend = default)
        {
            _backend = backend ?? new ListStorageBackend<T>(null);
            _converter = converter;
            _filter = filter;
        }

        public void HandlePacket(IPacket packet)
        {
            if (_filter != null && _filter(packet))
            {
                var item = _converter(packet);
                _backend.Write(item);
            }
        }

        public bool TryRead(out T? data)
        {
            return _backend.TryRead(_cursor, out data);
        }

        public void Clear()
        {
            _backend.Clear();
        }

        public void Dispose()
        {
            _backend.Dispose();
        }
    }

    public class CommandStorage : GeneralStorage<string>
    {
        public CommandStorage(IStorageNew<string>? backend = default) : base(
            converter: p => p.ToString(),
            filter: p => p.Type == PacketType.Command,
            backend: backend)
        {

        }
    }
}
