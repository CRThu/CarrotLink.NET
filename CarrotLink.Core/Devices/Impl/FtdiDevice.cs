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

namespace CarrotLink.Core.Devices.Impl
{
    
    public class FtdiDevice : DeviceBase<FtdiConfiguration>
    {
        /// <summary>
        /// 驱动层实现
        /// </summary>
        private FTDI ftdi;
        public new FtdiConfiguration Config => _config;

        private readonly object _lock_w = new object();
        private readonly object _lock_r = new object();

        public FtdiDevice(FtdiConfiguration config) : base(config)
        {
            ftdi = new FTDI();
        }

        public override async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected) return;

            Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.OpenBySerialNumber(Config.SerialNumber));

            // Set Timeout
            Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.SetTimeouts((uint)Config.Timeout, (uint)Config.Timeout));

            // Set BitMode
            // SYNC FIFO MODE NEED BOTH WRITE EEPROM AND SETBITMODE
            byte mask, mode;
            mask = 0xff;
            //   BitMode:
            //     For FT232H devices, valid values are FT_BIT_MODE_RESET, FT_BIT_MODE_ASYNC_BITBANG, FT_BIT_MODE_MPSSE, FT_BIT_MODE_SYNC_BITBANG, FT_BIT_MODE_CBUS_BITBANG, FT_BIT_MODE_MCU_HOST, FT_BIT_MODE_FAST_SERIAL, FT_BIT_MODE_SYNC_FIFO.
            //     For FT2232H devices, valid values are FT_BIT_MODE_RESET, FT_BIT_MODE_ASYNC_BITBANG, FT_BIT_MODE_MPSSE, FT_BIT_MODE_SYNC_BITBANG, FT_BIT_MODE_MCU_HOST, FT_BIT_MODE_FAST_SERIAL, FT_BIT_MODE_SYNC_FIFO.
            //     For FT4232H devices, valid values are FT_BIT_MODE_RESET, FT_BIT_MODE_ASYNC_BITBANG, FT_BIT_MODE_MPSSE, FT_BIT_MODE_SYNC_BITBANG.
            //     For FT232R devices, valid values are FT_BIT_MODE_RESET, FT_BIT_MODE_ASYNC_BITBANG, FT_BIT_MODE_SYNC_BITBANG, FT_BIT_MODE_CBUS_BITBANG.
            //     For FT245R devices, valid values are FT_BIT_MODE_RESET, FT_BIT_MODE_ASYNC_BITBANG, FT_BIT_MODE_SYNC_BITBANG.
            //     For FT2232 devices, valid values are FT_BIT_MODE_RESET, FT_BIT_MODE_ASYNC_BITBANG, FT_BIT_MODE_MPSSE, FT_BIT_MODE_SYNC_BITBANG, FT_BIT_MODE_MCU_HOST, FT_BIT_MODE_FAST_SERIAL.
            //     For FT232B and FT245B devices, valid values are FT_BIT_MODE_RESET, FT_BIT_MODE_ASYNC_BITBANG.
            mode = FT_BIT_MODES.FT_BIT_MODE_SYNC_FIFO;
            Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.SetBitMode(mask, mode));


            Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.Purge(FT_PURGE.FT_PURGE_RX & FT_PURGE.FT_PURGE_TX));

            ftdi.SetTimeouts(1, 1);

            IsConnected = true;
            _totalReadBytes =0;
            _totalWriteBytes =0;

            await Task.CompletedTask;
        }

        public override async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (ftdi != null && ftdi.IsOpen)
            {
                Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.Close());
                IsConnected = false;
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
            if (ftdi == null)
                throw new InvalidOperationException("Device not connected");

            if (!IsConnected) throw new InvalidOperationException("Not connected");

            // 同步实现

            uint bytesRead = 0;
            int bytesExpected = 0;
            byte[] rx = new byte[Math.Min(buffer.Length, Config.BufferSize)];

            bytesExpected = rx.Length;

            lock (_lock_r)
            {
                // Read
                Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.Read(rx, (uint)bytesExpected, ref bytesRead));
                _totalReadBytes += bytesRead;
            }
            rx.AsMemory(0, (int)bytesRead).CopyTo(buffer);

            await Task.CompletedTask;

            return (int)bytesRead;
        }

        public override async Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            if (ftdi == null)
                throw new InvalidOperationException("Device not connected");

            if (!IsConnected) throw new InvalidOperationException("Not connected");

            byte[] bufferWithZeroOffset = data.ToArray();
            uint numBytesWritten = 0;

            lock (_lock_w)
            {
                Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.Write(bufferWithZeroOffset, bufferWithZeroOffset.Length, ref numBytesWritten));

                // Waiting for transfer done
                // TODO 同步流写入存在阻塞，待优化
                while (bufferWithZeroOffset.Length != numBytesWritten)
                {
                    Debug.WriteLine($"Waiting for write device done ({numBytesWritten}/{bufferWithZeroOffset.Length})");
                }

                _totalWriteBytes += bufferWithZeroOffset.Length;
            }
            await Task.CompletedTask;
        }
    }
}
