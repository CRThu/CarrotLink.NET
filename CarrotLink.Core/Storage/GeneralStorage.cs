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
        private readonly IStorageBackend<T> _backend;
        private Func<IPacket, bool>? _filter;
        private Func<IPacket, T> _converter;

        private int _cursor = 0;

        public int Count => _backend.Count;
        public T this[int index] => _backend[index];

        public GeneralStorage(Func<IPacket, T> converter,
            Func<IPacket, bool>? filter = default,
            IStorageBackend<T>? backend = default)
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
            return _backend.TryRead(_cursor++, out data);
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
        public CommandStorage(IStorageBackend<string>? backend = default) : base(
            converter: p => p.ToString(),
            filter: p => p.PacketType == PacketType.Command,
            backend: backend)
        {

        }
    }
    public class DataStorage : GeneralStorage<string>
    {
        public DataStorage(Func<IPacket, string> converter, Func<IPacket, bool>? filter = null, IStorageBackend<string>? backend = null) : base(converter, filter, backend)
        {
            throw new NotImplementedException();
        }
    }
}
