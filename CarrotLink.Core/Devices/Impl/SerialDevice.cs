using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Devices.Interfaces;
using NationalInstruments.DataInfrastructure;

namespace CarrotLink.Core.Devices.Impl
{
    public class SerialDevice : DeviceBase<SerialConfiguration>, IEventTriggerDevice
    {
        /// <summary>
        /// 驱动层实现
        /// </summary>
        private SerialPort? _serialPort;

        public SerialDevice(SerialConfiguration config) : base(config) { }

        public event EventHandler<byte[]>? DataReceived;

        public override async Task ConnectAsync()
        {
            if (IsConnected) return;

            _serialPort = new SerialPort(
                portName: Config.PortName,
                baudRate: Config.BaudRate,
                parity: (Parity)Config.Parity,
                dataBits: Config.DataBits,
                stopBits: (StopBits)Config.StopBits);

            _serialPort.ReadBufferSize = 1048576;
            _serialPort.WriteBufferSize = 1048576;

            if (Config.UseHardwareEvent)
            {
                _serialPort.ReceivedBytesThreshold = 1; // 重要：收到1字节即触发
                _serialPort.DataReceived += OnSerialDataReceived;
            }

            _serialPort.Open();
            IsConnected = true;
            await Task.CompletedTask;
        }

        public override async Task DisconnectAsync()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                if (Config.UseHardwareEvent)
                {
                    _serialPort.DataReceived -= OnSerialDataReceived;
                }


                _serialPort.Close();
                IsConnected = false;
            }
            await Task.CompletedTask;
        }

        public override async Task<int> ReadAsync(Memory<byte> buffer)
        {
            if (_serialPort == null)
                throw new InvalidOperationException("Device not connected");

            if (!IsConnected) throw new InvalidOperationException("Not connected");

            var timeoutToken = CreateTimeoutToken();
            return await _serialPort.BaseStream
                .ReadAsync(buffer, timeoutToken)
                .ConfigureAwait(false);
        }

        public override async Task WriteAsync(ReadOnlyMemory<byte> data)
        {
            if (_serialPort == null)
                throw new InvalidOperationException("Device not connected");

            if (!IsConnected) throw new InvalidOperationException("Not connected");

            var timeoutToken = CreateTimeoutToken();
            await _serialPort.BaseStream
                .WriteAsync(data, timeoutToken)
                .ConfigureAwait(false);
        }
        private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!Config.UseHardwareEvent) return;
            if (_serialPort == null || !_serialPort.IsOpen) return;

            var bytesToRead = _serialPort.BytesToRead;
            if (bytesToRead == 0) return;

            var buffer = new byte[bytesToRead];
            _serialPort.Read(buffer, 0, bytesToRead);
            DataReceived?.Invoke(this, buffer);
        }

    }
}
