using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FTD2XX_NET;
using static FTD2XX_NET.FTDI;
using System.Diagnostics;
using System.Threading;
using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Devices.Library;
using CarrotLink.Core.Utility;

namespace CarrotLink.Core.Devices.Impl
{

    public class FtdiDevice : DeviceBase<FtdiConfiguration>
    {
        /// <summary>
        /// 驱动层实现
        /// </summary>
        private FTDI ftdi;

        /// <summary>
        /// ft2232h驱动限制最大读取为64K
        /// </summary>
        private static int _d2xxMaxReadSize = 65536;

        /// <summary>
        /// 内部接收缓冲区
        /// </summary>
        private static byte[] rxBuffer = new byte[_d2xxMaxReadSize];

        public new FtdiConfiguration Config => _config;
        public new bool IsConnected => ftdi != null && ftdi.IsOpen;

        private readonly object _lock_w = new object();
        private readonly object _lock_r = new object();

        public FtdiDevice(FtdiConfiguration config) : base(config)
        {
            ftdi = new FTDI();
        }

        public override async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected)
                return;

            Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.OpenBySerialNumber(Config.SerialNumber));

            // Set BitMode
            // SYNC FIFO MODE NEED BOTH WRITE EEPROM AND SETBITMODE
            byte mask, mode;
            mask = 0xff;
            mode = Config.Mode switch
            {
                FtdiCommMode.SyncFifo => FT_BIT_MODES.FT_BIT_MODE_SYNC_FIFO,
                _ => FT_BIT_MODES.FT_BIT_MODE_RESET,
            };
            Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.SetBitMode(mask, mode));

            // Flush
            Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.Purge(FT_PURGE.FT_PURGE_RX & FT_PURGE.FT_PURGE_TX));

            // Set Timeout
            //Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.SetTimeouts((uint)Config.Timeout, (uint)Config.Timeout));
            Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.SetTimeouts(1, 1));

            _totalReadBytes = 0;
            _totalWriteBytes = 0;

            await Task.CompletedTask;
        }

        public override async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (ftdi != null && ftdi.IsOpen)
            {
                Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.Close());
            }
            await Task.CompletedTask;
        }

        //private int GetBytesToRead()
        //{
        //    uint rxQuene = 0;
        //    Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.GetRxBytesAvailable(ref rxQuene));
        //    return (int)rxQuene;
        //}

        public override async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Device not connected");

            // 同步实现
            int bufferSizeLimit = Math.Min(buffer.Length, Config.BufferSize);

            int bytesRead = 0;
            //byte[] rx = new byte[_d2xxMaxReadSize];

            lock (_lock_r)
            {
                // Read
                //Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.Read(buffer.ToUnsafeArray(), (uint)bytesExpected, ref bytesRead));

                uint bytesToRead = 0;
                uint currentBytesRead = 0;
                while (true)
                {
                    Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.GetRxBytesAvailable(ref bytesToRead));
                    uint bytesExpected = (uint)Math.Min(_d2xxMaxReadSize, bytesToRead);

                    //Console.WriteLine($"(FTDI.GetRxBytesAvailable){ftStatus},{bytesToRead}");

                    // 数据超限则返回
                    if (bytesRead + bytesExpected > bufferSizeLimit)
                        break;

                    Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.Read(rxBuffer, bytesExpected, ref currentBytesRead));
                    //Console.WriteLine($"(FTDI.Read){ftStatus},{bytesExpected},{currentBytesRead}");

                    rxBuffer.AsMemory(0, (int)currentBytesRead).CopyTo(buffer.Slice(bytesRead));

                    bytesRead += (int)currentBytesRead;
                    if (currentBytesRead < _d2xxMaxReadSize)
                        break;
                }
                _totalReadBytes += bytesRead;
                //Console.WriteLine($"(FTDI.DEV)Read {bytesRead} bytes, Total: {TotalReceivedBytes} bytes");
            }
            return await Task.FromResult(bytesRead);
        }

        public override async Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Device not connected");

            uint numBytesWritten = 0;

            lock (_lock_w)
            {
                Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.Write(data.ToUnsafeArray(), data.Length, ref numBytesWritten));

                // Waiting for transfer done
                // TODO 同步流写入存在阻塞，待优化
                if (data.Length != numBytesWritten)
                {
                    throw new DriverErrorException($"FT_Write: numBytesWritten({numBytesWritten}) != data.Length({data.Length}).");
                }

                _totalWriteBytes += data.Length;
            }
            await Task.CompletedTask;
        }
    }
}
