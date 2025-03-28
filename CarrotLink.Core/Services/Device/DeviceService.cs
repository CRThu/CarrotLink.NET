using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Devices.Interfaces;
using CarrotLink.Core.Protocols.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services.Device
{
    /// <summary>
    /// 支持手动/定时/事件三种触发模式的接受服务调度以及发送方法
    /// </summary>
    public class DeviceService
    {
        private readonly IDevice _device;
        private readonly IProtocol _activeProtocol;
        private Timer? _pollingTimer;
        private bool _isProcessing = false;

        public DeviceService(IDevice device, IProtocol protocol)
        {
            _device = device;
            _activeProtocol = protocol;
        }

        /// <summary>
        /// 发送方法
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public async Task SendAsync(IPacket packet)
        {
            byte[] data = packet.Pack(_activeProtocol);
            await _device.WriteAsync(data);
        }

        // 手动触发模式
        public async Task<byte[]> ManualReadAsync()
        {
            var buffer = new byte[_device.Config.BufferSize];
            int bytesRead = await _device.ReadAsync(buffer.AsMemory());
            return buffer.Take(bytesRead).ToArray();
        }

        // 定时轮询模式
        public void StartAutoPolling(int intervalMs, Action<byte[]> callback)
        {
            if (_pollingTimer != null)
                throw new InvalidOperationException("Polling is already active");

            _pollingTimer = new Timer(async _ => {
                if (_isProcessing)
                    return;
                _isProcessing = true;

                var data = await ManualReadAsync();
                callback(data);

                _isProcessing = false;
            }, null, 0, intervalMs);
        }

        // 事件触发模式（需设备支持硬件中断）
        public void RegisterEventTrigger(Action<byte[]> callback)
        {
            if (_device is not IEventTriggerDevice eventDevice)
                throw new NotSupportedException("Device does not support event triggering");

            eventDevice.DataReceived += (sender, data) => {
                callback(data);
            };
        }
    }
}
