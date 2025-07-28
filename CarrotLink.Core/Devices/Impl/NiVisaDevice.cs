using System;
using System.Threading;
using System.Threading.Tasks;
using NationalInstruments.VisaNS;
using CarrotLink.Core.Devices.Configuration;

namespace CarrotLink.Core.Devices.Impl
{
    public class NiVisaDevice : DeviceBase<NiVisaConfiguration>
    {
        private MessageBasedSession? _visaSession;
        private readonly object _lock = new object();

        public NiVisaDevice(NiVisaConfiguration config) : base(config)
        {
        }

        /// <summary>
        /// 使用配置中的VISA资源字符串连接到设备。
        /// </summary>
        public override void Connect()
        {
            if (IsConnected)
                return;

            try
            {
                // 使用资源管理器打开VISA会话
                _visaSession = (MessageBasedSession)ResourceManager.GetLocalManager().Open(_config.ResourceString);

                // 根据配置设置I/O操作的超时时间
                if (_config.Timeout > 0)
                {
                    // VISA的超时单位是毫秒
                    _visaSession.Timeout = _config.Timeout;
                }

                _totalReadBytes = 0;
                _totalWriteBytes = 0;
                IsConnected = true;
            }
            catch (Exception ex)
            {
                // 如果连接失败，确保资源被释放
                _visaSession?.Dispose();
                _visaSession = null;
                IsConnected = false;
                throw new InvalidOperationException($"Failed to connect to NI-VISA resource '{_config.ResourceString}'. Please check if the resource string is correct and the device is available.", ex);
            }
        }

        /// <summary>
        /// 断开与设备的连接并释放资源。
        /// </summary>
        public override void Disconnect()
        {
            if (_visaSession != null)
            {
                // 必须设置NI-MAX/Tools/NI-VISA/Options,Disable R&S和Keysight驱动后设置Preferred NIVISA才能正常使用
                //_visaSession.Dispose();
                _visaSession = null;
            }
            IsConnected = false;
        }

        /// <summary>
        /// 从设备异步读取数据。
        /// </summary>
        /// <remarks>
        /// NI-VISA 仪器通常是命令/响应模式。此Read方法会尝试读取数据，如果仪器没有返回数据则会超时。
        /// 我们捕获预期的超时异常并返回0字节，以告知上层轮询服务当前没有数据，而不是抛出异常。
        /// </remarks>
        public override async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_visaSession == null || !IsConnected)
                throw new InvalidOperationException("Device not connected");

            int bytesRead = 0;
            byte[] localBuffer;

            lock (_lock)
            {
                try
                {
                    // 尝试从设备读取数据，最大读取长度由配置指定
                    localBuffer = _visaSession.ReadByteArray(_config.ReadBufferSize);
                    bytesRead = localBuffer.Length;

                    if (bytesRead > 0)
                    {
                        // 确保读取的数据不会超出外部传入的缓冲区大小
                        if (bytesRead > buffer.Length)
                        {
                            bytesRead = buffer.Length;
                            // 此处可以添加警告日志，提示缓冲区可能过小
                        }

                        localBuffer.AsMemory(0, bytesRead).CopyTo(buffer);
                        _totalReadBytes += bytesRead;
                    }
                }
                catch (VisaException ex) when (ex.ErrorCode == VisaStatusCode.ErrorTimeout)
                {
                    // 这是预期的行为，当仪器在超时时间内没有响应时（例如，在发送查询命令之前）
                    // 我们返回0，表示没有数据可读
                    bytesRead = 0;
                }
                // 其他类型的VisaException将被抛出，由上层代码处理
            }

            await Task.CompletedTask;
            return bytesRead;
        }

        /// <summary>
        /// 向设备异步写入数据。
        /// </summary>
        public override async Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            if (_visaSession == null || !IsConnected)
                throw new InvalidOperationException("Device not connected");

            byte[] dataToWrite = data.ToArray();

            lock (_lock)
            {
                _visaSession.Write(dataToWrite);
                _totalWriteBytes += dataToWrite.Length;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 释放设备资源。
        /// </summary>
        public override void Dispose()
        {
            Disconnect();
            base.Dispose();
        }
    }
}