using CarrotLink.Core.Devices;
using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Devices.Interfaces;
using CarrotLink.Core.Discovery;
using CarrotLink.Core.Discovery.Models;
using CarrotLink.Core.Session;
using CarrotLink.Core.Protocols.Models;
using CarrotLink.NFC.Models;
using CarrotLink.NFC.Protocols;
using System.Collections.Concurrent;

namespace CarrotLink.NFC.Demo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Title = "CarrotLink NFC 寻卡 Demo";
        Console.WriteLine("=== CarrotLink NFC 寻卡验证程序 ===");

        // 1. 设备发现
        var factory = new DeviceSearcherFactory();
        var discoveryService = new DeviceDiscoveryService(factory);
        
        Console.WriteLine("\n[1/3] 正在扫描可用硬件设备...");
        var devices = discoveryService.DiscoverAll().ToList();

        if (!devices.Any())
        {
            Console.WriteLine("未发现可用设备。");
            return;
        }

        for (int i = 0; i < devices.Count; i++)
        {
            var dev = devices[i];
            Console.WriteLine($"  [{i}] {dev.Driver} | {dev.Name}");
        }

        Console.Write("\n请选择设备编号: ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice >= devices.Count) return;

        var selectedDevice = devices[choice];

        // 2. 协议与会话初始化
        var registry = new NfcCommandRegistry();
        registry.LoadFromDirectory("nfc/");
        var protocol = new Pn532HsuProtocol(registry);

        IDevice? device = null;
        DeviceSession? session = null;

        try
        {
            DeviceConfigurationBase config = selectedDevice.Driver switch
            {
                DriverType.Serial => new SerialConfiguration { DeviceId = selectedDevice.Name, PortName = selectedDevice.Name, BaudRate = 115200 },
                _ => throw new NotSupportedException()
            };

            device = DeviceFactory.Create(selectedDevice.Interface, config);
            device.Connect();

            session = new DeviceSessionBuilder()
                .WithDevice(device)
                .WithProtocol(protocol)
                .WithPolling(autoPolling: true, interval: 20)
                .Build();

            Console.WriteLine("\n[2/3] 链路已建立，正在初始化 PN532...");

            // 初始化 SAM (Normal mode)
            // 0x14 0x01 0x14 0x01 -> SAMConfiguration, Mode=1 (Normal), Timeout=0x14, IRQ=0x01
            await session.SendNfcAsync("PN532.SAMConfiguration", new byte[] { 0x01, 0x14, 0x01 });
            Console.WriteLine("SAM 初始化完成。");

            // 3. 循环寻卡
            Console.WriteLine("\n[3/3] 进入寻卡循环，请将 NFC 卡片靠近读卡器...");
            Console.WriteLine("(按 Ctrl+C 退出程序)\n");

            while (true)
            {
                // 发送 InListPassiveTarget 指令 (1个目标, 106kbps TypeA)
                // 0x4A 0x01 0x00
                var response = await session.SendNfcWithResponseAsync("PN532.InListPassiveTarget", new byte[] { 0x01, 0x00 }, timeoutMs: 1000);

                if (response != null && response.IsSuccess && response.Payload != null && response.Payload.Length > 0)
                {
                    byte nbTargets = response.Payload[0];
                    if (nbTargets > 0)
                    {
                        // 解析 UID (通常在 Payload 的特定位置，取决于 InListPassiveTarget 的返回格式)
                        // 根据 PN532 文档：D5 4B [Nb] [Tg] [SENS_RES] [SEL_RES] [NFCIDLength] [NFCID...]
                        // 简化处理：跳过前 5 个字节获取 UID
                        int idLen = response.Payload[5];
                        byte[] uid = new byte[idLen];
                        Array.Copy(response.Payload, 6, uid, 0, idLen);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 发现卡片! UID: {BitConverter.ToString(uid).Replace("-", "")}");
                        Console.ResetColor();

                        // 发现后稍作停顿，避免刷屏
                        await Task.Delay(1000);
                    }
                }

                await Task.Delay(200); // 轮询间隔
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生错误: {ex.Message}");
        }
        finally
        {
            session?.Dispose();
            device?.Disconnect();
        }
    }
}
