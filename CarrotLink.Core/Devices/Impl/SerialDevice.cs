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

namespace CarrotLink.Core.Devices.Impl
{
    public class SerialDevice : DeviceBase<SerialConfiguration>/*, IEventTriggerDevice*/
    {
        /// <summary>
        /// 驱动层实现
        /// </summary>
        private SerialPort? _serialPort;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly object _lock_w = new object();
        private readonly object _lock_r = new object();

        public SerialDevice(SerialConfiguration config) : base(config) { }

        //public event EventHandler<byte[]>? DataReceived;

        public override void Connect()
        {
            if (IsConnected)
                return;

            _serialPort = new SerialPort(
                portName: _config.PortName,
                baudRate: _config.BaudRate,
                parity: (Parity)_config.Parity,
                dataBits: _config.DataBits,
                stopBits: (StopBits)_config.StopBits);

            _serialPort.ReadBufferSize = 16 * 1024 * 1024;
            _serialPort.WriteBufferSize = 16 * 1024 * 1024;

            // ch343 may error when setting -1
            //_serialPort.ReadTimeout = int.MaxValue;
            //_serialPort.WriteTimeout = int.MaxValue;
            _serialPort.ReadTimeout = -1;
            _serialPort.WriteTimeout = -1;

            //if (Config.UseHardwareEvent)
            //{
            //    _serialPort.ReceivedBytesThreshold = 1; // 重要：收到1字节即触发
            //    _serialPort.DataReceived += OnSerialDataReceived;
            //}

            _totalWriteBytes = 0;
            _totalReadBytes = 0;

            _serialPort.Open();
            IsConnected = true;
        }

        public override void Disconnect()
        {
            _cts.Cancel();
            _cts.Dispose();

            if (_serialPort != null && _serialPort.IsOpen)
            {
                //if (Config.UseHardwareEvent)
                //{
                //    _serialPort.DataReceived -= OnSerialDataReceived;
                //}

                _serialPort.Close();
                IsConnected = false;
            }
        }

        public override async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_serialPort == null)
                throw new InvalidOperationException("Device not connected");

            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            int bytesRead;

            // 异步实现
            //try
            //{
            //    //var timeoutToken = CreateTimeoutToken();
            //    bytesRead = await _serialPort.BaseStream
            //   .ReadAsync(buffer, _cts.Token)
            //   .ConfigureAwait(false);
            //    TotalReceivedBytes += bytesRead;
            //    return bytesRead;
            //}
            //catch (OperationCanceledException ex)
            //{
            //    Console.WriteLine($"[INFO]: SerialDevice throwed OperationCanceledException");
            //    return 0;
            //    // 正常取消操作，无需处理
            //}
            //catch (IOException ex) when (ex.HResult == 995) // ERROR_OPERATION_ABORTED
            //{
            //    Console.WriteLine($"[INFO]: SerialDevice throwed IOException, ex.HResult == 995");
            //    Console.WriteLine(ex);
            //    return 0;
            //    // 处理因取消导致的 IOException
            //}
            //finally
            //{
            //}

            // 同步实现
            byte[] localBuffer = new byte[buffer.Length];

            lock (_lock_r)
            {
                try
                {
                    bytesRead = _serialPort.Read(localBuffer, 0, localBuffer.Length);
                }
                catch (TimeoutException ex)
                {
                    //Console.WriteLine(ex);
                    bytesRead = 0;
                }
                _totalReadBytes += bytesRead;
            }

            localBuffer.AsMemory(0, bytesRead).CopyTo(buffer);
            await Task.CompletedTask;

            return bytesRead;
        }

        public override async Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            if (_serialPort == null)
                throw new InvalidOperationException("Device not connected");

            if (!IsConnected)
                throw new InvalidOperationException("Not connected");


            // 异步实现
            //try
            //{
            //var timeoutToken = CreateTimeoutToken();
            //await _serialPort.BaseStream
            //    .WriteAsync(data, _cts.Token)
            //    .ConfigureAwait(false);
            //TotalSentBytes += data.Length;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex);
            //}


            // 同步实现
            byte[] localBuffer = data.ToArray();

            lock (_lock_w)
            {
                _serialPort.Write(localBuffer, 0, localBuffer.Length);
                _totalWriteBytes += data.Length;
            }

            await Task.CompletedTask;

        }

        //private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    try
        //    {
        //        if (!Config.UseHardwareEvent) return;
        //        if (_serialPort == null || !_serialPort.IsOpen) return;

        //        lock (_lock_r)
        //        {
        //            var bytesToRead = _serialPort.BytesToRead;
        //            if (bytesToRead == 0) return;

        //            var buffer = new byte[bytesToRead];
        //            _serialPort.Read(buffer, 0, bytesToRead);
        //            DataReceived?.Invoke(this, buffer);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex);
        //    }
        //}
    }
}
