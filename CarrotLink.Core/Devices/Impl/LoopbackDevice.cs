using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using CarrotLink.Core.Devices.Configuration;

namespace CarrotLink.Core.Devices.Impl
{
    public class LoopbackDevice : DeviceBase<LoopbackConfiguration>
    {

        private readonly ConcurrentQueue<byte> _quene = new ConcurrentQueue<byte>();

        public LoopbackDevice(LoopbackConfiguration config) : base(config)
        {
        }

        public override async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = true;
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public override async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = false;
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public override async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var size = Math.Min(buffer.Length - 1, _quene.Count);
            if (size == 0)
                return 0;
            var _buf = ArrayPool<byte>.Shared.Rent(size);
            var index = 0;
            try
            {
                while (_quene.TryDequeue(out _buf[index]))
                {
                    index++;
                    if (index == size)
                        break;
                }
                _buf.AsMemory(0, index).CopyTo(buffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(_buf);
            }
            _totalReadBytes += index;
            return await Task.FromResult(index).ConfigureAwait(false);
        }

        public override async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Device is not connected");

            for (int i = 0; i < buffer.Length; i++)
                _quene.Enqueue(buffer.Span[i]);

            _totalWriteBytes += buffer.Length;
            await Task.CompletedTask.ConfigureAwait(false);

        }
    }
}
