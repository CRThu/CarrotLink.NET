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
    public class GeneralStorage<T> : IStorage<T>, IDisposable
    {
        private readonly IStorageBackend<T> _backend;
        private Func<IPacket, bool>? _filter;
        private Func<IPacket, T> _converter;

        public long Count => _backend.Count;

        public GeneralStorage(Func<IPacket, T> converter,
            Func<IPacket, bool>? filter = default,
            IStorageBackend<T>? backend = default)
        {
            _backend = backend ?? new MemoryStorageBackend<T>();
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
            return _backend.TryRead(out data);
        }

        public async Task<T?> ReadAsync(CancellationToken cancellationToken = default)
        {
            return await _backend.ReadAsync(cancellationToken);
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
            filter: p => p.Type == PacketType.Command,
            backend: backend)
        {

        }
    }
}
