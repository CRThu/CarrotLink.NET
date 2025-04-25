using CarrotLink.Core.Devices.Impl;
using CarrotLink.Core.Services.Storage;
using CarrotLink.Core.Services;
using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Protocols;
using CarrotLink.Core.Protocols.Impl;
using CarrotLink.Core.Services.Device;
using CarrotLink.Core.Utility;
using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Discovery;
using CarrotLink.Core.Devices.Interfaces;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace CarrotLink.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("[CarrotLink.Client]");
            Console.WriteLine("Hello, World!");

            Console.WriteLine("请选择操作:");
            Console.WriteLine("0. 设备扫描");
            Console.WriteLine("1. 测试数据传输");
            Console.WriteLine("2. 测试通信, 读取");
            Console.WriteLine("3. 测试函数");
            var choice = Console.ReadLine();

            switch (choice)
            {
                case "0":
                    await DiscoverAllDevicesAsync();
                    break;
                case "1":
                    await PerformDataTransferTestAsync();
                    break;
                case "2":
                    await HandleUserInputAsync();
                    break;
                case "3":
                    await TestMethod();
                    break;
                default:
                    Console.WriteLine("无效选择");
                    break;
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task DiscoverAllDevicesAsync()
        {
            Console.WriteLine("Searching for all devices...");

            var factory = new DeviceSearcherFactory();
            var service = new DeviceDiscoveryService(factory);

            var allDevices = service.DiscoverAll();

            if (allDevices.Any())
            {
                Console.WriteLine("The following devices were found:");
                foreach (var device in allDevices)
                {
                    Console.WriteLine($"Interface: {device.Interface}, Name: {device.Name}, Description: {device.Description}");
                }
            }
            else
            {
                Console.WriteLine("No devices were found.");
            }

            await Task.CompletedTask;
        }

        private static async Task<(IDevice, DeviceService, MemoryStorage)> InitializeDeviceAndServiceAsync()
        {
            // 示例：完整设备操作流程
            var config = new SerialConfiguration
            {
                DeviceId = "Serial-COM17",
                PortName = "COM17",
                BaudRate = 115200,
            };
            var device = new SerialDevice(config);

            //var device = new LoopbackDevice(new LoopbackConfiguration() { DeviceId = "Loopback" });
            await device.ConnectAsync();

            var protocol = new RawAsciiProtocol();
            var storage = new MemoryStorage();

            // 创建带线程安全的存储管道
            var service = new DeviceService(device, protocol, new ConcurrentStorageDecorator(storage));
            _ = service.StartProcessingAsync();

            service.StartAutoPolling(250);

            return (device, service, storage);
        }

        private static async Task PerformDataTransferTestAsync()
        {
            IDevice device;
            DeviceService service;
            MemoryStorage storage;
            (device, service, storage) = await InitializeDeviceAndServiceAsync();

            // 发送大数据量测试
            Console.WriteLine("开始数据测试...");
            int packetNum = 10000;
            for (int i = 0; i < packetNum; i++)
            {
                await service.SendAscii($"{i:D18}");
                if (i % 10000 == 0)
                    await Task.Delay(10);
            }

            Console.WriteLine("数据测试发送完成");

            Console.WriteLine("Press any key to see transfer info:");
            Console.ReadKey(intercept: true);
            Console.WriteLine($"TotalBytesReceived: {service.TotalBytesReceived}");
            Console.WriteLine($"Device TX: {device.TotalSentBytes}, RX: {device.TotalReceivedBytes}");

            // 比较数据是否正确
            var sentData = Enumerable.Range(0, packetNum).Select(i => $"{i:D18}").ToArray();
            var receivedData = storage.GetStoredData().ToArray();

            for (int i = 0; i < sentData.Length; i++)
            {
                string send = sentData[i];
                string recv = (receivedData[i] as AsciiPacket).Payload;
                if (send != recv)
                    Console.WriteLine($"Sent: {send}, Received: {recv}");
            }
            bool isDataCorrect = sentData.SequenceEqual(receivedData.Select(p => (p as AsciiPacket).Payload));
            Console.WriteLine($"数据是否正确: {isDataCorrect}");

            await device.DisconnectAsync();
        }

        private static async Task HandleUserInputAsync()
        {
            IDevice device;
            DeviceService service;
            MemoryStorage storage;
            (device, service, storage) = await InitializeDeviceAndServiceAsync();

            // 循环读取用户输入并发送
            Console.WriteLine("Press exit to end transfer");
            while (true)
            {
                var line = Console.ReadLine();
                if (line == "exit")
                {
                    break;
                }
                await service.SendAscii(line!.ToString());
                Console.WriteLine($"Sent: {line}");

                await Task.Delay(500);

                var pkt = storage.Read();
                if (pkt != null)
                    Console.WriteLine($"Received: {(pkt as AsciiPacket)}");
            }

            // 导出最终数据
            await storage.ExportAsJsonAsync("data.json");

            await device.DisconnectAsync();
        }

        private static async Task TestMethod()
        {
            try
            {
                SerialPort serialPort = new SerialPort("COM1");

                serialPort.Open();

                var buffer = new byte[4096];
                // 1: timeout: throw
                //serialPort.ReadTimeout = 1000;
                //var b = serialPort.Read(buffer, 0, buffer.Length);

                // 2: timeout: throw
                //serialPort.ReadTimeout = 1000;
                //var b = serialPort.BaseStream.Read(buffer, 0, buffer.Length);

                // 3: timeout:
                int b = 0;
                CancellationTokenSource cts = new(1000);
                //serialPort.ReadTimeout = 0; // 无效
                var readTask = serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                await Task.Delay(2000);
                serialPort.DiscardInBuffer();

                var complTask = await Task.WhenAny(readTask, Task.Delay(5000));
                if (complTask == readTask)
                {
                    b = readTask.Result;
                }
                else
                {
                    Console.WriteLine("5000ms timeout");
                }

                Console.WriteLine($"recv {b} bytes");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
