using CarrotLink.Core.Devices.Impl;
using CarrotLink.Core.Services.Storage;
using CarrotLink.Core.Services;
using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Protocols;
using CarrotLink.Core.Protocols.Impl;

namespace CarrotLink.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            // 示例：完整设备操作流程
            var config = new SerialConfiguration
            {
                DeviceId = "Serial-COM3",
                PortName = "COM3",
                BaudRate = 115200,
            };
            var device = new SerialDevice(config);
            var storage = new MemoryStorage();
            var parser = new RawAsciiProtocol();
            await device.ConnectAsync();
            // 创建带线程安全的存储管道
            var pipeline = new DevicePipelineService(parser, new ConcurrentStorageDecorator(storage));
            _ = pipeline.StartProcessingAsync();
            // 定时读取数据
            var scheduler = new DeviceServiceScheduler(device);
            scheduler.StartAutoPolling(1000, data =>
            {
                pipeline.WriteToPipelineAsync(data).Wait();
                Console.WriteLine($"Received {data.Length} bytes");
            });
            // 导出最终数据
            await storage.ExportAsJsonAsync("data.json");
        }
    }
}
