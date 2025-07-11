using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Devices.Interfaces;
using CarrotLink.Core.Logging;
using CarrotLink.Core.Protocols.Models;
using NationalInstruments.VisaNS;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services
{
    /// <summary>
    /// 设备会话，支持手动/定时/事件三种触发模式的接受服务调度以及发送方法
    /// </summary>
    public class DeviceSession : IDisposable
    {
        // component
        private IDevice _device;
        private IProtocol _protocol;
        private List<IPacketLogger> _loggers;

        // task
        private Task? _processingTask;
        private Task? _pollingTask;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private int _isRunning;

        // for logger event
        public delegate void PacketHandler(IPacket packet);
        public event PacketHandler OnPacketReceived;

        // for processing
        private static ArrayPool<byte> _dataProcPool = ArrayPool<byte>.Create(128 * 1024 * 1024, 5);
        private readonly Pipe _pipe = new Pipe();

        // for polling
        private PeriodicTimer? _pollingTimer;


        // lock flag
        private int _isReading = 0;
        private int _isWriting = 0;

        private long _totalWriteBytes = 0;
        private long _totalReadBytes = 0;

        public long TotalWriteBytes => _totalWriteBytes;
        public long TotalReadBytes => _totalReadBytes;

        public DeviceSession(IDevice device, IProtocol protocol, IEnumerable<IPacketLogger> loggers)
        {
            _device = device;
            _protocol = protocol;
            _loggers = new List<IPacketLogger>(loggers);

            _loggers.ForEach(l => OnPacketReceived += l.HandlePacket);
        }

        public static DeviceServiceBuilder Create() => new DeviceServiceBuilder();

        public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            PipeReader? reader = _pipe.Reader;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // read when data received
                    ReadResult readResult = await reader.ReadAsync(cancellationToken);

                    var buffer = readResult.Buffer;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // parse until buffer has no complete packets
                        bool parsed = _protocol.TryDecode(ref buffer, out IPacket? packet);
                        if (!parsed || packet == null)
                            break;

                        // save to storage
                        OnPacketReceived?.Invoke(packet);
                    }

                    // set examined position
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
            catch (OperationCanceledException ex)
            {
                //reader.Complete(ex);
                Console.WriteLine("DeviceService.StartProcessingAsync() cancelled");
            }
        }

        /// <summary>
        /// 发送方法
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task WriteAsync(IPacket packet, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            if (Interlocked.CompareExchange(ref _isWriting, 1, 0) != 0)
                throw new InvalidOperationException("Write Operation is running");
            try
            {
                var pktBytes = _protocol.Encode(packet);
                await _device.WriteAsync(pktBytes, cancellationToken);
                Interlocked.Add(ref _totalWriteBytes, pktBytes.Length);
            }
            finally
            {
                Volatile.Write(ref _isWriting, 0);
            }
        }

        private async Task<int> SafeReadInternalAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return 0;
            if (Interlocked.CompareExchange(ref _isReading, 1, 0) != 0)
                throw new InvalidOperationException("Read Operation is running");
            try
            {
                return await ReadInternalAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Volatile.Write(ref _isReading, 0);
            }
        }

        private async Task<int> ReadInternalAsync(CancellationToken cancellationToken = default)
        {
            PipeWriter? writer = _pipe.Writer;
            byte[] buffer = _dataProcPool.Rent(_device.Config.BufferSize);
            try
            {
                int bytesRead = await _device.ReadAsync(buffer, cancellationToken);
                var bufmem = buffer.AsMemory(0, bytesRead);
                if (bytesRead > 0)
                {
                    var result = await writer.WriteAsync(bufmem, cancellationToken);

                    Interlocked.Add(ref _totalReadBytes, bytesRead);
                    Console.WriteLine($"Received {bufmem.Length} bytes, Total: {TotalReadBytes} bytes");

                    if (bytesRead == buffer.Length)
                    {
                        Console.WriteLine("Warning: Buffer is full, data might be lost.");
                    }
                }
                return bytesRead;
            }
            catch (OperationCanceledException ex)
            {
                //writer.Complete(ex);
                Console.WriteLine("DeviceService.ReadImplAsync() cancelled");
                return 0;
            }
            finally
            {
                _dataProcPool.Return(buffer);
            }
        }

        // 手动触发模式
        public async Task<int> ManualReadAsync(CancellationToken cancellationToken = default)
        {
            return await SafeReadInternalAsync(cancellationToken);
        }

        // 定时轮询模式
        public Task StartAutoPollingAsync(int intervalMs, CancellationToken cancellationToken = default)
        {
            if (_pollingTimer != null)
                throw new InvalidOperationException("Polling is already active");

            _pollingTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMs));
            return Task.Run(async () =>
            {
                try
                {
                    while (await _pollingTimer.WaitForNextTickAsync(cancellationToken))
                    {
                        //Debug.WriteLine($"[PeriodicTimer]: {DateTime.Now} TIME TO READ");
                        var data = await SafeReadInternalAsync(cancellationToken);
                        //Debug.WriteLine($"[PeriodicTimer]: READ DONE");
                    }
                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine("DeviceService.StartAutoPolling() cancelled");
                }
                finally
                {
                    _pollingTimer.Dispose();
                    _pollingTimer = null;
                }
            }, cancellationToken);
        }


        // 事件触发模式（需设备支持硬件中断）
        //private bool _isProcessing = false;
        //private readonly object _lockObj = new object();

        //public void RegisterEventTrigger(Action<byte[]> callback)
        //{
        //    try
        //    {
        //        if (_device is not IEventTriggerDevice eventDevice)
        //            throw new NotSupportedException("Device does not support event triggering");

        //        eventDevice.DataReceived += (sender, data) =>
        //        {
        //            if (data.Length > 0)
        //            {
        //                lock (_lockObj)
        //                {
        //                    if (_isProcessing)
        //                        return;

        //                    _isProcessing = true;
        //                }

        //                try
        //                {
        //                    callback(data);
        //                    TotalBytesReceived += data.Length;
        //                    Console.WriteLine($"Received {data.Length} bytes, Total: {TotalBytesReceived} bytes");
        //                }
        //                finally
        //                {
        //                    lock (_lockObj)
        //                    {
        //                        _isProcessing = false;
        //                    }
        //                }
        //            }
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex);
        //    }
        //}

        public void Dispose()
        {
            try
            {
                _pipe.Writer.Complete();
                _pipe.Reader.Complete();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
