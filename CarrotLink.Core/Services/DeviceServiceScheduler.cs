using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Devices.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services
{/// <summary>
 /// 支持手动/定时/事件三种触发模式的服务调度
 /// </summary>
    public class DeviceServiceScheduler
    {
        private readonly DeviceBase<DeviceConfigurationBase> _device;
        private Timer? _pollingTimer;
        public DeviceServiceScheduler(DeviceBase<DeviceConfigurationBase> device) => _device = device;

        /*
        // 手动触发模式
        public async Task<byte[]> ManualReadAsync()
        => await _device.ReadAsync(_device.Config.timeout);

        // 定时轮询模式
        public void StartAutoPolling(int intervalMs, Action<byte[]> callback)
        {
            _pollingTimer = new Timer(async _ =>
            {
                var data = await _device.ReadAsync(_device.Config.timeout);
                callback(data);
            }, null, 0, intervalMs);
        }

        // 事件触发模式（需设备支持硬件中断）
        public void RegisterEventTrigger(Action<byte[]> callback)
        {
            // 示例：假设设备有DataReceived事件
            if (_device is SerialDevice serialDevice)
                serialDevice.DataReceived += (sender, data) => callback(data);
        }
        */
    }
}
