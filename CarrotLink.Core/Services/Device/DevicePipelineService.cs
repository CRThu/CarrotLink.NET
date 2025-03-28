using CarrotLink.Core.Protocols.Impl;
using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Services.Storage;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services.Device
{
    public sealed class DevicePipelineService : IDisposable
    {
        private readonly Pipe _pipe = new Pipe();
        private readonly IProtocol _protocol;
        private readonly IDataStorage _storage;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly object _writeLock = new object();

        public DevicePipelineService(IProtocol parser, IDataStorage storage)
        {
            _protocol = parser ?? throw new ArgumentNullException(nameof(parser));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public async Task StartProcessingAsync()
        {
            PipeReader? reader = _pipe.Reader;
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    ReadResult readResult = await reader.ReadAsync(_cts.Token);
                    var buffer = readResult.Buffer;
                    while (true)
                    {
                        bool parsed = _protocol.TryParse(ref buffer, out IPacket? packet);
                        if (!parsed || packet == null)
                            break;

                        await _storage.SaveAsync(packet);
                    }
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("DevicePipelineService.StartProcessingAsync() cancelled");
            }
        }

        public async Task WriteToPipelineAsync(byte[] data)
        {
            lock (_writeLock)
            {
                _pipe.Writer.WriteAsync(data, _cts.Token).AsTask().Wait();
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _pipe.Writer.Complete();
            _pipe.Reader.Complete();
        }
    }
}
