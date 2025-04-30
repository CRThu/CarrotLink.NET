using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Devices.Interfaces;
using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Services.Storage;
using NationalInstruments.VisaNS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CarrotLink.Core.Services.Device
{
    /// <summary>
    /// 支持手动/定时/事件三种触发模式的接受服务调度以及发送方法
    /// </summary>
    public class DeviceService : IDisposable
    {
        private readonly IDevice _device;
        private readonly IProtocol _protocol;
        private readonly IDataStorage _storage;

        // pipe for proc
        private readonly Pipe _pipe = new Pipe();

        // timer for read
        private PeriodicTimer? _pollingTimer;

        private int _isReading;

        public long TotalBytesSent { get; private set; } = 0;
        public long TotalBytesReceived { get; private set; } = 0;

        public DeviceService(IDevice device, IProtocol protocol, IDataStorage storage)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

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
                        bool parsed = _protocol.TryParse(ref buffer, out IPacket? packet);
                        if (!parsed || packet == null)
                            break;

                        // save to storage
                        await _storage.SaveAsync(packet);
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
        /// <returns></returns>
        public async Task WriteAsync(IPacket packet, CancellationToken cancellationToken = default)
        {
            byte[] data = packet.Pack(_protocol);
            await _device.WriteAsync(data);
            TotalBytesSent += data.Length;
        }

        private async Task<byte[]> SafeReadInternalAsync(CancellationToken cancellationToken = default)
        {
            if(cancellationToken.IsCancellationRequested)
                return Array.Empty<byte>();
            if (Interlocked.CompareExchange(ref _isReading, 1, 0) != 0)
                throw new InvalidOperationException("Reading Operation is running");
            try
            {
                return await ReadInternalAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Volatile.Write(ref _isReading, 0);
            }
        }

        private async Task<byte[]> ReadInternalAsync(CancellationToken cancellationToken = default)
        {
            PipeWriter? writer = _pipe.Writer;
            try
            {
                var buffer = new byte[_device.Config.BufferSize];
                int bytesRead = await _device.ReadAsync(buffer.AsMemory(), cancellationToken);
                var data = buffer.Take(bytesRead).ToArray();
                if (bytesRead > 0)
                {
                    var result = await writer.WriteAsync(data, cancellationToken);

                    TotalBytesReceived += bytesRead;
                    Console.WriteLine($"Received {data.Length} bytes, Total: {TotalBytesReceived} bytes");

                    if (bytesRead == buffer.Length)
                    {
                        Console.WriteLine("Warning: Buffer is full, data might be lost.");
                    }
                }
                return data;
            }
            catch (OperationCanceledException ex)
            {
                //writer.Complete(ex);
                Console.WriteLine("DeviceService.ReadImplAsync() cancelled");
                return Array.Empty<byte>();
            }
        }

        // 手动触发模式
        public async Task<byte[]> ManualReadAsync(CancellationToken cancellationToken = default)
        {
            return await SafeReadInternalAsync(cancellationToken);
        }

        // 定时轮询模式
        public Task StartAutoPolling(int intervalMs, CancellationToken cancellationToken = default)
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
