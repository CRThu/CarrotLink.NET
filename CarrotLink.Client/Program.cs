using CarrotLink.Core.Devices.Impl;
using CarrotLink.Core.Services.Storage;
using CarrotLink.Core.Services;
using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Protocols;
using CarrotLink.Core.Protocols.Impl;
using CarrotLink.Core.Services.Device;
using CarrotLink.Core.Utility;

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
            // 定时读取数据
            var service = new DeviceService(device, protocol, new ConcurrentStorageDecorator(storage));
            _ = service.StartProcessingAsync();

            service.StartAutoPolling(1000, data => {
            });

            // 发送大数据量测试
            Console.WriteLine("开始数据测试...");
            int packetNum = 1000000;
            for (int i = 0; i < packetNum; i++)
            {
                await service.SendAscii($"{i:D18}");
            }

            Console.WriteLine("数据测试发送完成");

            Console.WriteLine("Press any key to see transfer info:");
            Console.ReadKey(intercept: true);
            Console.WriteLine($"TotalBytesReceived: {service.TotalBytesReceived}");
            Console.WriteLine($"Device TX: {device.TotalSentBytes}, RX: {device.TotalReceivedBytes}");


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
