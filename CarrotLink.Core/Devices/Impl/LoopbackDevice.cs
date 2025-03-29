using System;
using System.Threading.Tasks;
using CarrotLink.Core.Devices.Configuration;

namespace CarrotLink.Core.Devices.Impl
{
    public class LoopbackDevice : DeviceBase<LoopbackConfiguration>
    {
        private byte[] _buffer;
        private int _bufferSize;
        private int _readPosition;
        private int _writePosition;
        private readonly object _lock = new object();

        public LoopbackDevice(LoopbackConfiguration config) : base(config)
        {
            _bufferSize = 16 * 1024 * 1024;
            _buffer = new byte[_bufferSize];
        }

        public override Task ConnectAsync()
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        public override Task DisconnectAsync()
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public override Task<int> ReadAsync(Memory<byte> buffer)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Device is not connected");

            int bytesRead = 0;
            lock (_lock)
            {
                while (bytesRead < buffer.Length)
                {
                    int localReadPosition = _readPosition;
                    int localWritePosition = _writePosition;

                    if (localReadPosition == localWritePosition)
                        break;

                    buffer.Span[bytesRead++] = _buffer[localReadPosition];
                    _readPosition = (localReadPosition + 1) % _bufferSize;
                }
            }

            TotalReceivedBytes += bytesRead;
            return Task.FromResult(bytesRead);
        }

        public override Task WriteAsync(ReadOnlyMemory<byte> data)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Device is not connected");

            lock (_lock)
            {
                foreach (var b in data.Span)
                {
                    int localWritePosition = _writePosition;
                    _buffer[localWritePosition] = b;
                    _writePosition = (localWritePosition + 1) % _bufferSize;
                }
            }

            TotalSentBytes += data.Length;
            return Task.CompletedTask;
        }
    }
}
