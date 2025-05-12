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
}
