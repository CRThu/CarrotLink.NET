using CarrotLink.Core.Devices.Impl;
using CarrotLink.Core.Services.Storage;
using CarrotLink.Core.Services;
using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Protocols;
using CarrotLink.Core.Protocols.Impl;
using CarrotLink.Core.Services.Device;

namespace CarrotLink.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            // 示例：完整设备操作流程
            var config = new SerialConfiguration {
                DeviceId = "Serial-COM250",
                PortName = "COM250",
                BaudRate = 115200,
            };
            var device = new SerialDevice(config);
            await device.ConnectAsync();

            var protocol = new RawAsciiProtocol();
            var storage = new MemoryStorage();

            // 创建带线程安全的存储管道
            var pipeline = new DevicePipelineService(protocol, new ConcurrentStorageDecorator(storage));
            _ = pipeline.StartProcessingAsync();

            // 定时读取数据
            var service = new DeviceService(device, protocol);
            int totalBytesReceived = 0;
            object lockObject = new object();

            service.StartAutoPolling(100, async data => {
                await pipeline.WriteToPipelineAsync(data);
                lock (lockObject)
                {
                    totalBytesReceived += data.Length;
                }
                Console.WriteLine($"Received {data.Length} bytes, Total: {totalBytesReceived} bytes");
            });

            // 循环读取用户输入并发送
            Console.WriteLine("Press ESC to end transfer");
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }
                await service.SendAscii(key.KeyChar.ToString());
            }

            // 导出最终数据
            storage.ExportAsJsonAsync("data.json").Wait();
        }
    }
}
