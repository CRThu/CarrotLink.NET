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
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // timer for read
        private PeriodicTimer? _pollingTimer;

        public long TotalBytesSent { get; private set; } = 0;
        public long TotalBytesReceived { get; private set; } = 0;

        public DeviceService(IDevice device, IProtocol protocol, IDataStorage storage)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
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
                Debug.WriteLine("DeviceService.StartProcessingAsync() cancelled");
            }
        }

        /// <summary>
        /// 发送方法
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task WriteAsync(IPacket packet)
        {
            byte[] data = packet.Pack(_protocol);
            await _device.WriteAsync(data);
            TotalBytesSent += data.Length;
        }

        private async Task<byte[]> ReadImplAsync()
        {
            var buffer = new byte[_device.Config.BufferSize];
            int bytesRead = await _device.ReadAsync(buffer.AsMemory());
            var data = buffer.Take(bytesRead).ToArray();
            if (bytesRead > 0)
            {
                var result = await _pipe.Writer.WriteAsync(data, _cts.Token);

                TotalBytesReceived += bytesRead;
                Console.WriteLine($"Received {data.Length} bytes, Total: {TotalBytesReceived} bytes");

                if (bytesRead == buffer.Length)
                {
                    Console.WriteLine("Warning: Buffer is full, data might be lost.");
                }
            }
            return data;
        }

        // 手动触发模式
        public async Task<byte[]> ManualReadAsync()
        {
            return await ReadImplAsync();
        }

        // 定时轮询模式
        public void StartAutoPolling(int intervalMs, Action<byte[]> callback)
        {
            if (_pollingTimer != null)
                throw new InvalidOperationException("Polling is already active");

            _pollingTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMs));
            _ = Task.Run(async () => {
                while (await _pollingTimer.WaitForNextTickAsync())
                {
                    var data = await ReadImplAsync();
                    if (data.Length > 0)
                    {
                        callback(data);
                    }
                }
            });
        }


        // 事件触发模式（需设备支持硬件中断）
        private bool _isProcessing = false;
        private readonly object _lockObj = new object();

        public void RegisterEventTrigger(Action<byte[]> callback)
        {
            if (_device is not IEventTriggerDevice eventDevice)
                throw new NotSupportedException("Device does not support event triggering");

            eventDevice.DataReceived += (sender, data) => {
                if (data.Length > 0)
                {
                    lock (_lockObj)
                    {
                        if (_isProcessing)
                            return;

                        _isProcessing = true;
                    }

                    try
                    {
                        callback(data);
                        TotalBytesReceived += data.Length;
                        Console.WriteLine($"Received {data.Length} bytes, Total: {TotalBytesReceived} bytes");
                    }
                    finally
                    {
                        lock (_lockObj)
                        {
                            _isProcessing = false;
                        }
                    }
                }
            };
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
