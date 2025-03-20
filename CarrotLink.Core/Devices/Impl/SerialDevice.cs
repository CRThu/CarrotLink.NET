using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CarrotLink.Core.Devices.Configuration;
using NationalInstruments.DataInfrastructure;

namespace CarrotLink.Core.Devices.Impl
{
    public class SerialDevice : DeviceBase<SerialConfiguration>
    {
        /// <summary>
        /// 驱动层实现
        /// </summary>
        private SerialPort? _serialPort;

        public SerialDevice(SerialConfiguration config) : base(config) { }

        public override async Task ConnectAsync()
        {
            if (IsConnected) return;

            _serialPort = new SerialPort(
                portName: Config.PortName,
                baudRate: Config.BaudRate,
                parity: (Parity)Config.Parity,
                dataBits: Config.DataBits,
                stopBits: (StopBits)Config.StopBits);

            _serialPort.Open();
            IsConnected = true;
            await Task.CompletedTask;
        }

        public override async Task DisconnectAsync()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
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
    }
}
