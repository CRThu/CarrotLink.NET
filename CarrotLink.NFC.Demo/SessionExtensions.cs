using CarrotLink.Core.Session;
using CarrotLink.NFC.Models;
using CarrotLink.Core.Protocols.Models;
using System.Collections.Concurrent;

namespace CarrotLink.NFC.Demo;

/// <summary>
/// NFC 会话流式调用扩展。
/// </summary>
public static class SessionExtensions
{
    private static readonly ConcurrentDictionary<DeviceSession, TaskCompletionSource<NfcPacket>> _pendingRequests = new();

    /// <summary>
    /// 发送 NFC 指令（单向）。
    /// </summary>
    public static async Task SendNfcAsync(this DeviceSession session, string mnemonic, byte[]? payload = null)
    {
        var packet = new NfcPacket
        {
            Mnemonic = mnemonic,
            Payload = payload,
            Direction = NfcDirection.Request
        };
        await session.WriteAsync(packet);
    }

    /// <summary>
    /// 发送 NFC 指令并等待响应包（同步请求模式）。
    /// </summary>
    /// <param name="session">当前会话</param>
    /// <param name="mnemonic">助记符（如 PN532.GetFirmwareVersion）</param>
    /// <param name="payload">参数载荷</param>
    /// <param name="timeoutMs">超时时间(ms)</param>
    /// <returns>响应包，超时则返回 null</returns>
    public static async Task<NfcPacket?> SendNfcWithResponseAsync(this DeviceSession session, string mnemonic, byte[]? payload = null, int timeoutMs = 2000)
    {
        var tcs = new TaskCompletionSource<NfcPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        // 定义响应匹配处理器
        void OnReceived(IPacket packet, string sender)
        {
            if (packet is NfcPacket p && p.Direction == NfcDirection.Response)
            {
                // 在简单的寻卡场景中，最新的响应即为目标响应包
                // 生产环境建议通过 OpCode 或 SequenceID 进行更精确的匹配
                tcs.TrySetResult(p);
            }
        }

        // 临时监听
        session.OnPacketReceived += OnReceived;

        try
        {
            await session.SendNfcAsync(mnemonic, payload);
            
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            if (completedTask == tcs.Task)
            {
                return await tcs.Task;
            }
            return null; // 链路超时或包丢失
        }
        finally
        {
            // 务必移除监听避免内存泄漏和重复触发
            session.OnPacketReceived -= OnReceived;
        }
    }
}
