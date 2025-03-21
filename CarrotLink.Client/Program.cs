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
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            // 示例：完整设备操作流程
            var config = new SerialConfiguration
            {
                DeviceId = "Serial-COM250",
                PortName = "COM250",
                BaudRate = 115200,
            };
            var device = new SerialDevice(config);
            device.ConnectAsync().Wait();

            var parser = new RawAsciiProtocol();
            var storage = new MemoryStorage();

            // 创建带线程安全的存储管道
            var pipeline = new DevicePipelineService(parser, new ConcurrentStorageDecorator(storage));
            _ = pipeline.StartProcessingAsync();

            // 定时读取数据
            var scheduler = new DeviceServiceScheduler(device);
            scheduler.StartAutoPolling(100, async data =>
            {
                await pipeline.WriteToPipelineAsync(data);
                Console.WriteLine($"Received {data.Length} bytes");
            });


            // 等待用户主动退出
            Console.WriteLine("Press any key to end transfer");
            Console.ReadKey();

            // 导出最终数据
            storage.ExportAsJsonAsync("data.json").Wait();
        }
    }
}
