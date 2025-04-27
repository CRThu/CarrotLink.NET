using System;
using System.Threading.Tasks;
using CarrotLink.Core.Devices.Configuration;

namespace CarrotLink.Core.Devices.Impl
{
    public class LoopbackDevice : DeviceBase<LoopbackConfiguration>
    {
        private byte[] _buffer;
        private int _readPosition;
        private int _writePosition;
        private readonly object _lock = new object();

        private int FreeBytesLength
        {
            get
            {
                if (_writePosition >= _readPosition)
                    return _buffer.Length - _writePosition + _readPosition - 1;
                else
                    return _readPosition - _writePosition - 1;
            }
        }

        public LoopbackDevice(LoopbackConfiguration config) : base(config)
        {
            _buffer = new byte[config.BufferSize];
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
            if (!IsConnected)
                throw new InvalidOperationException("Device is not connected");

            int bytesRead;
            lock (_lock)
            {
                int bytesAvailable = _writePosition - _readPosition;
                if (bytesAvailable < 0)
                    bytesAvailable += _buffer.Length;
                bytesRead = Math.Min(bytesAvailable, buffer.Length);
                if (bytesRead > 0)
                {
                    var firstSegmentLength = Math.Min(bytesRead, _buffer.Length - _readPosition);
                    var firstSegment = new ReadOnlyMemory<byte>(_buffer, _readPosition, firstSegmentLength);
                    firstSegment.CopyTo(buffer.Slice(0, firstSegmentLength));

                    if (firstSegmentLength < bytesRead)
                    {
                        var secondSegmentLength = bytesRead - firstSegmentLength;
                        var secondSegment = new ReadOnlyMemory<byte>(_buffer, 0, secondSegmentLength);
                        secondSegment.CopyTo(buffer.Slice(firstSegmentLength, secondSegmentLength));
                    }

                    _readPosition = (_readPosition + bytesRead) % _buffer.Length;
                }

                TotalReceivedBytes += bytesRead;
            }
            return await Task.FromResult(bytesRead).ConfigureAwait(false);
        }

        public override async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Device is not connected");

            lock (_lock)
            {
                int bytesWritten = 0;
                int bytesRemaining = Math.Min(buffer.Length, FreeBytesLength);

                var firstSegmentLength = Math.Min(bytesRemaining, _buffer.Length - _writePosition);
                if (firstSegmentLength > 0)
                {
                    var firstSegment = new Memory<byte>(_buffer, _writePosition, firstSegmentLength);
                    buffer.Slice(0, firstSegmentLength).CopyTo(firstSegment);
                    _writePosition = (_writePosition + firstSegmentLength) % _buffer.Length;
                    bytesWritten += firstSegmentLength;
                }

                if (bytesWritten < bytesRemaining)
                {
                    var secondSegmentLength = bytesRemaining - bytesWritten;
                    var secondSegment = new Memory<byte>(_buffer, 0, secondSegmentLength);
                    buffer.Slice(bytesWritten, secondSegmentLength).CopyTo(secondSegment);
                    _writePosition = (_writePosition + secondSegmentLength) % _buffer.Length;
                    bytesWritten += secondSegmentLength;

                }
                TotalSentBytes += bytesWritten;
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
