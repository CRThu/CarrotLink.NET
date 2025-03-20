using CarrotLink.Core.Protocols.Impl;
using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Services.Storage;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services
{
    public sealed class DevicePipelineService : IDisposable
    {
        private readonly Pipe _pipe = new Pipe();
        private readonly IProtocolParser _parser;
        private readonly IDataStorage _storage;
        private readonly MemoryPool<byte> _memoryPool = MemoryPool<byte>.Shared;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        public DevicePipelineService(IProtocolParser parser, IDataStorage storage)
        {
            _parser = parser;
            _storage = storage;
        }
        public async Task StartProcessingAsync()
        {
            PipeReader? reader = _pipe.Reader;
            while (!_cts.IsCancellationRequested)
            {
                ReadResult readResult = await reader.ReadAsync(_cts.Token);
                foreach (var segment in readResult.Buffer)
                {
                    //_parser.TryParse(segment, out RawAsciiProtocolPacket parsedData);
                    //_storage.StoreInMemory(Encoding.UTF8.GetBytes(parsedData.ToString()));
                }
                reader.AdvanceTo(readResult.Buffer.End);
            }
        }
        public async Task WriteToPipelineAsync(byte[] data)
        {
            using IMemoryOwner<byte>? owner = _memoryPool.Rent(data.Length);
            data.CopyTo(owner.Memory.Span);
            await _pipe.Writer.WriteAsync(owner.Memory, _cts.Token);
        }
        public void Dispose() => _cts.Cancel();
    }
}
