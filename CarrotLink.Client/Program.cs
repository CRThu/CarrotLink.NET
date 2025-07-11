using CarrotLink.Core.Devices.Impl;
using CarrotLink.Core.Services;
using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Protocols;
using CarrotLink.Core.Protocols.Impl;
using CarrotLink.Core.Utility;
using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Discovery;
using CarrotLink.Core.Devices.Interfaces;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System;
using CarrotLink.Core.Logging;
using CarrotLink.Logging.NLogLogger;
using CarrotLink.Core.Storage;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.IO;
using CarrotLink.Core.Discovery.Models;

namespace CarrotLink.Client
{
    public class CommContext
    {
        public DeviceSession Session;
        public IDevice Device;
        public IProtocol Protocol;
        public Dictionary<string, IPacketLogger> Loggers;
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[CarrotLink.Client]");
            Console.WriteLine("StorageBackend test...");
            StorageBackendTest.StorageBackendSyncTest();
            StorageBackendTest.StorageBackendAsyncTest();

            Console.Write("press to run device");
            Console.ReadKey();

            Console.WriteLine("Hello, World!");

            Console.WriteLine("Discovering device...");
            DeviceInfo[] devices = DiscoverAllDevices().ToArray();
            Console.WriteLine("Discovering done.");


            var context = new CommContext();
            CancellationTokenSource cts = new CancellationTokenSource();
            Console.WriteLine("Initialize device...");

            // 示例：完整设备操作流程
            //var config = new SerialConfiguration
            //{
            //    DeviceId = "Serial-COM17",
            //    PortName = "COM17",
            //    BaudRate = 115200,
            //};
            //context.Device = new SerialDevice(config);

            //context.Device = new LoopbackDevice(new LoopbackConfiguration() { DeviceId = "Loopback" });

            var config = new FtdiConfiguration
            {
                DeviceId = "ftdi-1",
                SerialNumber = "FTA8EKKFA",
                Mode = FtdiCommMode.AsyncFifo,
                Model = FtdiModel.Ft2232h,
            };
            context.Device = new FtdiDevice(config);

            context.Device.Connect();
            Console.WriteLine("Initialize done.");

            Console.WriteLine("Initialize service...");
            context.Protocol = new CarrotAsciiProtocol(null);
            context.Loggers = new Dictionary<string, IPacketLogger>()
            {
                //{"console",new ConsoleLogger() },
                {"nlog", new NLogWrapper(true,"nlog.log") },
                {"storage", new CommandStorage() }
            };

            context.Session = DeviceSession.Create()
                .WithDevice(context.Device)
                .WithProtocol(context.Protocol)
                .WithLoggers(context.Loggers.Values)
                .Build();
            Task procTask = context.Session.StartProcessingAsync(cts.Token);
            Task pollTask = context.Session.StartAutoPollingAsync(15, cts.Token);
            Console.WriteLine("Initialize done...");

            try
            {
                while (true)
                {
                    Console.WriteLine("请选择操作:");
                    Console.WriteLine("1. LoopbackTest");
                    Console.WriteLine("2. EnterCommands");
                    Console.WriteLine("3. DebugTest");
                    Console.WriteLine("q. Exit");
                    var k = Console.ReadKey();
                    if (k.KeyChar == 'q')
                        break;
                    Console.WriteLine();
                    switch (k.KeyChar)
                    {
                        case '1':
                            LoopbackTest(context);
                            break;
                        case '2':
                            EnterCommands(context);
                            break;
                        case '3':
                            DebugTest();
                            break;
                        default:
                            Console.WriteLine("无效选择");
                            break;
                    }
                }
            }
            finally
            {
                cts.Cancel();
                Task.WhenAll(procTask, pollTask).Wait();
                context.Device.Disconnect();
                cts.Dispose();
                context.Session.Dispose();
                context.Device.Dispose();
                foreach (var logger in context.Loggers.Values)
                    logger.Dispose();
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static IEnumerable<DeviceInfo> DiscoverAllDevices()
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
                    Console.WriteLine($"Interface: {device.Type}, Name: {device.Name}, Description: {device.Description}");
                }
            }
            else
            {
                Console.WriteLine("No devices were found.");
            }
            return allDevices;
        }

        private static void LoopbackTest(CommContext context)
        {
            // 发送大数据量测试
            Console.WriteLine("开始数据测试...");
            int packetNum = 1000000;
            for (int i = 0; i < packetNum; i++)
            {
                context.Session.SendAscii($"{i:D18}");
                if (i % 10000 == 0)
                    Thread.Sleep(10);
            }

            Console.WriteLine("数据测试发送完成");

            Console.WriteLine("Press any key to see transfer info:");
            Console.ReadKey(intercept: true);
            Console.WriteLine($"TotalBytesReceived: {context.Session.TotalReadBytes}");
            Console.WriteLine($"Device TX: {context.Device.TotalWriteBytes}, RX: {context.Device.TotalReadBytes}");

            // 比较数据是否正确
            var commandStorage = (context.Loggers["storage"] as CommandStorage);

            if (packetNum != commandStorage.Count)
                Console.WriteLine($"Sent bytes != received bytes.");
            else
            {
                int maxErrorCount = 0;
                for (int i = 0; i < packetNum; i++)
                {
                    string send = $"{i:D18}";
                    commandStorage.TryRead(out string? recv);
                    if (send != recv)
                    {
                        maxErrorCount++;
                        Console.WriteLine($"ERROR DATA: Sent: {send}, Received: {recv}");
                    }
                    if (maxErrorCount >= 20)
                    {
                        Console.WriteLine("Only display 20 items.");
                        break;
                    }
                }
            }
        }

        private static void EnterCommands(CommContext context)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            object _lock = new();
            var commandStorage = (context.Loggers["storage"] as CommandStorage);

            var readTask = Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    bool hasPkt = commandStorage.TryRead(out var pkt);
                    if (hasPkt)
                    {
                        lock (_lock)
                        {
                            Console.WriteLine($"Received: {pkt}");
                        }
                    }
                    Task.Delay(100);
                }
            }, cts.Token);

            // 循环读取用户输入并发送
            Console.WriteLine("enter commands or 'exit' to end transfer");
            while (true)
            {
                var line = Console.ReadLine();
                if (line == "exit")
                {
                    break;
                }

                context.Session.SendAscii(line!.ToString());
                lock (_lock)
                {
                    Console.WriteLine($"Sent: {(line == "" ? "<empty>" : line)}");
                }
            }

            cts.Cancel();

            // 导出最终数据
            //await context.Storage.ExportAsJsonAsync("data.json");
        }

        private static void DebugTest()
        {
            //try
            //{
            //    SerialPort serialPort = new SerialPort("COM1");

            //    serialPort.Open();

            //    var buffer = new byte[4096];
            //    // 1: timeout: throw
            //    //serialPort.ReadTimeout = 1000;
            //    //var b = serialPort.Read(buffer, 0, buffer.Length);

            //    // 2: timeout: throw
            //    //serialPort.ReadTimeout = 1000;
            //    //var b = serialPort.BaseStream.Read(buffer, 0, buffer.Length);

            //    // 3: timeout:
            //    int b = 0;
            //    CancellationTokenSource cts = new(1000);
            //    //serialPort.ReadTimeout = 0; // 无效
            //    var readTask = serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
            //    await Task.Delay(2000);
            //    serialPort.DiscardInBuffer();

            //    var complTask = await Task.WhenAny(readTask, Task.Delay(5000));
            //    if (complTask == readTask)
            //    {
            //        b = readTask.Result;
            //    }
            //    else
            //    {
            //        Console.WriteLine("5000ms timeout");
            //    }

            //    Console.WriteLine($"recv {b} bytes");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}
        }
    }
}
