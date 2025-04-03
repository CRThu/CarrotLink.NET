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
        private readonly object _lock_w = new object();
        private readonly object _lock_r = new object();

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

            _serialPort.ReadBufferSize = 16 * 1024 * 1024;
            _serialPort.WriteBufferSize = 16 * 1024 * 1024;

            if (Config.UseHardwareEvent)
            {
                _serialPort.ReceivedBytesThreshold = 1; // 重要：收到1字节即触发
                _serialPort.DataReceived += OnSerialDataReceived;
            }

            _serialPort.Open();
            IsConnected = true;

            TotalSentBytes = 0;
            TotalReceivedBytes = 0;

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

            int bytesRead;

            //// 异步实现
            //var timeoutToken = CreateTimeoutToken();
            //bytesRead = await _serialPort.BaseStream
            //   .ReadAsync(buffer, timeoutToken)
            //   .ConfigureAwait(false);
            //TotalReceivedBytes += bytesRead;

            //return bytesRead;

            // 同步实现
            byte[] localBuffer = new byte[buffer.Length];

            lock (_lock_r)
            {
                bytesRead = _serialPort.Read(localBuffer, 0, localBuffer.Length);
                TotalReceivedBytes += bytesRead;
            }

            localBuffer.AsMemory(0, bytesRead).CopyTo(buffer);
            await Task.CompletedTask;

            return bytesRead;
        }

        public override async Task WriteAsync(ReadOnlyMemory<byte> data)
        {
            if (_serialPort == null)
                throw new InvalidOperationException("Device not connected");

            if (!IsConnected) throw new InvalidOperationException("Not connected");


            //// 异步实现
            //var timeoutToken = CreateTimeoutToken();
            //await _serialPort.BaseStream
            //    .WriteAsync(data, timeoutToken)
            //    .ConfigureAwait(false);
            //TotalSentBytes += data.Length;


            // 同步实现
            byte[] localBuffer = data.ToArray();

            lock (_lock_w)
            {
                _serialPort.Write(localBuffer, 0, localBuffer.Length);
                TotalSentBytes += data.Length;
            }

            await Task.CompletedTask;

        }

        private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!Config.UseHardwareEvent) return;
            if (_serialPort == null || !_serialPort.IsOpen) return;

            lock (_lock_r)
            {
                var bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead == 0) return;

                var buffer = new byte[bytesToRead];
                _serialPort.Read(buffer, 0, bytesToRead);
                DataReceived?.Invoke(this, buffer);
            }
        }
    }
}
