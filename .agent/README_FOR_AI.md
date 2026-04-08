# CarrotLink.NET AI 开发指南

## 1. 项目愿景与核心定位
CarrotLink.NET 是一个基于 .NET 8 的高性能、插件化设备通信框架。其核心目标是解决上位机与嵌入式设备（串口、FTDI、VISA）之间异构协议的标准化解析、异步数据流调度以及高吞吐量数据存储问题。

## 2. 核心架构分层 (Layered Architecture)
项目严格遵循“硬件抽象 - 协议逻辑 - 会话调度”三层解耦架构：

### 2.1 硬件抽象层 (Device Layer)
*   **核心接口**: `IDevice` (`CarrotLink.Core.Devices.Interfaces`)
*   **职责**: 屏蔽物理介质差异，仅提供原始字节流的 `ReadAsync` 和 `WriteAsync`。
*   **关键实现**:
    *   `SerialDevice`: 标准串口通信。
    *   `FtdiDevice`: 基于 D2XX 驱动的高速 FTDI 芯片（如 FT2232H）通信。
    *   `NiVisaDevice`: 兼容 NI-VISA 标准的仪器控制（GPIB, USB-TMC, LAN）。
    *   `LoopbackDevice`: 内部回环测试设备。

### 2.2 协议逻辑层 (Protocol Layer)
*   **核心接口**: `IProtocol` (`CarrotLink.Core.Protocols.Impl`)
*   **职责**: 负责字节流 (`ReadOnlySequence<byte>`) 与结构化数据包 (`IPacket`) 的相互转换。
*   **关键实现**:
    *   `CarrotAsciiProtocol`: 处理 `CARROT_RPC.md` 定义的 ASCII 格式（[DATA], [REG] 等）。
    *   `CarrotBinaryProtocol`: 高速二进制包格式（DATA_266 等）。
    *   `ScpiProtocol`: 标准可编程仪器控制指令解析。

### 2.3 会话调度层 (Session Layer)
*   **核心类**: `DeviceSession` (`CarrotLink.Core.Session`)
*   **职责**: 整个框架的引擎。使用 `System.IO.Pipelines` 管理数据流，协调 Device 的读取与 Protocol 的解析。
*   **运行模式**:
    *   **AutoPolling (默认)**: 开启后台定时轮询任务从硬件读数并自动解析。
    *   **Manual**: 开发者手动触发读取逻辑，适用于请求-响应式指令。

## 3. 核心目录映射表 (Directory Map)
| 目录 | 说明 |
| :--- | :--- |
| `CarrotLink.Core/Devices` | 硬件驱动实现与配置定义 |
| `CarrotLink.Core/Protocols` | 协议解析算法与报文模型 (`IPacket`) |
| `CarrotLink.Core/Session` | `DeviceSession` 引擎与构造器 |
| `CarrotLink.Core/Storage` | 高并发数据存储后端 (基于 `Channel<T>`) |
| `CarrotLink.Core/Utility` | 字节操作、高性能 Span 转换等工具类 |
| `CarrotLink.Native` | 供外部（如 Python/C++）调用的 Native AOT 导出接口 |
| `CarrotLink.UnitTest` | 协议解析与核心逻辑的单元测试 |

## 4. 核心执行逻辑 (Core Execution Logic)

### 4.1 数据流管道 (Data Pipeline)
`DeviceSession` 是框架的心脏，采用 `System.IO.Pipelines` 实现高性能的生产者-消费者模型：
1.  **生产者 (`ReadInternalAsync`)**: 
    *   从 `IDevice` 异步读取字节流。
    *   使用 `ArrayPool<byte>` 租借缓冲区（默认 4MB），减少 GC 压力。
    *   将读取的数据写入 `PipeWriter`。
2.  **消费者 (`ProcessAsync` / `AutoProcessingAsync`)**:
    *   从 `PipeReader` 读取字节。
    *   调用 `IProtocol.TryDecode` 进行协议切包。
    *   解析成功的 `IPacket` 会分发给所有绑定的 `IPacketLogger` 并触发 `OnPacketReceived` 事件。

### 4.2 调度模式 (Scheduling)
*   **并发安全**: 使用 `Interlocked` (如 `_isReading`, `_isWriting`) 确保同一 Session 在读/写操作上的原子性，防止重入。
*   **定时轮询**: `AutoPollingAsync` 使用 `PeriodicTimer` (默认 15ms 间隔) 触发物理读取，适合高频实时数据采集。
*   **异常处理**: 任何链路层异常会通过 `OnError` 事件上报给 `IRuntimeLogger`。

## 5. 高性能存储后端 (Storage Backend)
针对高速采集场景（如 ADC 原始波形），`CarrotLink.Core.Storage` 提供了非阻塞存储：
*   **机制**: 基于 `System.Threading.Channels` 实现。
*   **ListStorageBackend<T>**: 
    *   写入操作 (`Write`) 将数据推入 Channel。
    *   后台任务 (`ProcessItemsAsync`) 批量从 Channel 提取数据并写入内部 `List<T>`。
    *   **背压控制**: 支持 `BoundedCapacity`（有界容量），防止内存溢出。
    *   **性能优化**: 内部使用 4096 长度的缓冲区进行批量合并写入，极大降低了 `List` 扩容和锁竞争开销。

## 6. 协议解析深度说明 (Protocol Deep Dive)

### 6.1 CarrotAsciiProtocol (CARROT RPC)
*   **特征检测**: 通过第一个字符判断类型。`[` 开头进入 RPC 解析，非 `[` 默认作为 `CommandPacket`。
*   **状态维护**: 具备“读请求”上下文感知。当发送 `REG.R` 后，若收到不带地址的 `[REG]: 0xVAL` 回复，能自动关联至上一次请求的地址。
*   **高效解析**: 广泛使用 `ReadOnlySpan<char>` 和 `SpanEx` 工具类，避免字符串切片导致的内存分配。

### 6.2 CarrotBinaryProtocol
*   **帧结构**: 固定帧头 `0x3C (<)`，帧尾 `0x3E (>)`。
*   **多通道支持**: 支持位域控制（Interleaved），通过 `DataPacketConfig` 结构体（LayoutKind.Explicit）直接映射控制字节，实现极速解析。

## 7. 扩展开发指南 (Extension Guide)

### 7.1 新增设备支持 (New Device)
1.  **定义配置**: 在 `CarrotLink.Core.Devices.Configuration` 下继承 `DeviceConfigurationBase`。
2.  **实现驱动**: 继承 `DeviceBase<TConfig>` 并实现 `ReadAsync` / `WriteAsync`。
    *   *准则*: 必须处理超时异常，返回 0 字节而非抛出中断 Session 的异常。
3.  **注册工厂**: 在 `DeviceFactory.cs` 的 `Create` 方法中添加对应枚举和逻辑。

### 7.2 新增协议解析 (New Protocol)
1.  **实现接口**: 实现 `IProtocol` 接口。
2.  **数据流解析**: `TryDecode` 必须遵循非破坏性读取。
    *   若数据不足，返回 `false` 且不移动 `buffer` 游标。
    *   若解析成功，必须通过 `buffer = buffer.Slice(reader.Position)` 更新游标。
3.  **注册工厂**: 在 `ProtocolFactory.cs` 中添加枚举项。

### 7.3 新增存储/日志 (New Logger/Storage)
1.  **实现接口**: 实现 `IPacketLogger` 或 `IRuntimeLogger`。
2.  **异步注意**: `HandlePacket` 应尽量轻量，耗时操作必须内部异步化（参考 `ListStorageBackend`），避免阻塞 `DeviceSession` 的处理线程。

## 8. 开发约束与命名规范 (Developer Constraints)

### 8.1 异步与性能
*   **强制异步**: 所有的 I/O 操作必须使用 `Async` 结尾的异步方法。
*   **无分配解析**: 优先使用 `ReadOnlySpan<byte>` 或 `ReadOnlySpan<char>` 进行解析。严禁在 `TryDecode` 循环内进行大规模的 `string` 拼接或 `byte[]` 拷贝。
*   **对象池**: 大规模字节缓冲区必须通过 `ArrayPool<byte>.Shared` 租借。

### 8.2 错误处理
*   **业务异常**: 使用 `DriverErrorException` 封装底层驱动错误。
*   **静默处理**: 在 `DeviceSession` 的轮询中，网络/串口超时应被捕获并记录日志，不应导致整个会话崩溃。

### 8.3 环境闭环 (Python Interop)
*   **uv 规范**: 项目包含 `py/` 目录。涉及 Python 脚本运行或依赖管理时：
    *   必须使用 `uv run python ...` 或 `uv run pytest ...`。
    *   新增依赖必须使用 `uv add <package>`。
    *   禁止直接调用 `python` 或 `pip`。

## 9. 核心工具箱 (Utility Toolbox)
AI 执行者在编写代码前应优先复用以下工具类：
*   **`SpanEx`**: 提供高效的十六进制和浮点数 Span 解析。
*   **`BytesEx`**: 字节数组与十六进制字符串的转换。
*   **`EscapeStringEx`**: 处理带转义字符的 ASCII 字符串（如 `\r\n\x01`）。
*   **`TimeoutDecorator`**: 同步转异步的超时包装器。

## 10. Native AOT 交互规范 (Native Interop)

### 10.1 导出接口 (`CarrotLink.Native`)
项目支持通过 Native AOT 技术导出 C 风格接口，允许 Python (ctypes) 或 C++ 直接调用。
*   **导出标记**: 必须使用 `[UnmanagedCallersOnly(EntryPoint = "...")]`。
*   **内存管理**: 
    *   托管对象（如 `IDevice`）必须通过 `GCHandle.Alloc(obj)` 转换为 `IntPtr` 返回给外部。
    *   外部调用者持有该 `IntPtr` 作为句柄。
*   **参数传递**: 字符串传递需使用 `byte*` + `length` 模式，并利用 `Marshal.PtrToStringUTF8` 进行转换。

### 10.2 Python 侧集成 (`py/src`)
*   **加载逻辑**: 见 `aot_invoke.py`。使用 `ctypes.CDLL` 加载发布后的原生 DLL。
*   **环境依赖**: Python 侧必须使用 `uv` 管理。执行路径需确保 DLL 及其依赖项（如 FTD2XX.dll, NiVisa 运行库）在系统搜索路径中。

## 11. 单元测试规范 (Testing Standard)

### 11.1 测试架构
*   **框架**: 使用 MSTest。
*   **并行化**: 在 `MSTestSettings.cs` 中全局开启 `Parallelize(Scope = ExecutionScope.MethodLevel)` 以加速测试。
*   **核心用例**: 
    *   `CarrotRpcTests.cs` 覆盖了所有 `CARROT_RPC.md` 定义的 ASCII 报文解析场景。
    *   测试应包含：边界值测试、不完整报文测试、多包连续解析测试。

### 11.2 环境闭环测试指令
*   **执行测试**: `dotnet test` 或 `uv run dotnet test`。
*   **断言准则**: 协议解析测试必须验证 `IPacket` 的具体属性（如 `Address`, `Value`, `Channels`）而非仅验证解析是否成功。

## 12. 文档同步与演进 (Documentation Evolution)

### 12.1 强制同步机制
每次 AI 执行者进行以下修改时，必须**增量式更新**相关文档：
1.  **新增 RPC 命令/格式**: 同步更新 `CARROT_RPC.md`。
2.  **修改核心架构或存储模式**: 同步更新本 `README_FOR_AI.md`。
3.  **新增硬件依赖**: 更新 `README.md` 中的“多设备支持”章节。

### 12.2 禁止行为 (Forbidden Actions)
*   **禁止硬编码**: 严禁将设备地址、波特率等配置硬编码在 `Device` 实现类中，必须通过 `Configuration` 类注入。
*   **禁止破坏性解析**: 严禁在 `TryDecode` 中使用 `buffer.ToArray()`，这会破坏 `ReadOnlySequence` 的零拷贝优势。
*   **禁止绕过 Session**: 严禁在应用层直接操作底层 `IDevice` 的 Read/Write，必须通过 `DeviceSession` 进行调度以维持状态一致性。

## 13. 扩展项目 (Extension Projects)

### 13.1 CarrotLink.NFC
*   **定位**: 提供硬件无关的 NFC 通信抽象模型，通过“助记符 (Mnemonic) + 语义化动作 (Action)”简化 NFC 交互过程。
*   **模型层 (`CarrotLink.NFC.Models`)**:
    *   **`NfcAction`**: 语义化动作枚举。
        *   控制原语: `FieldOn`, `FieldOff`, `Halt`。
        *   请求指令: `REQA`, `WUPA`, `ListPassiveTarget`, `Transceive`。
        *   响应结果: `GetAtqa`, `GetSak`, `GetUid`, `Response`。
    *   **`NfcPacket`**: 实现 `ICommandPacket` 的 `record` 模型。
        *   `Command` 计算属性: 聚合展示助记符、十六进制 Payload 和 Action 状态，支持空保护。
*   **协议层 (`CarrotLink.NFC.Protocols`)**:
    *   **`Pn532HsuProtocol`**: 针对 PN532 HSU (High-Speed UART) 的协议实现。
        *   **帧结构**: `00 00 FF LEN LCS [TFI DATA] DCS 00`。
        *   **校验算法**:
            *   `LCS`: `(0x100 - (LEN & 0xFF)) & 0xFF`。
            *   `DCS`: `(0x100 - (Sum(TFI+DATA) & 0xFF)) & 0xFF`。
        *   **ACK 机制**: 自动识别并过滤静默 ACK 帧 (`00 00 FF 00 FF 00`)。
        *   **指令映射**:
            *   `ListPassiveTarget` 映射至 PN532 指令 `0x4A 01 00`。
            *   `REQA`/`WUPA`/`Halt` 映射至 `InCommunicateThru` (`0x42`) 模式。
        *   **响应解析**: 解析 `TFI=0xD5` 后的 OpCode。对于 `0x4B` (ListPassiveTarget) 响应，解析 `NbTarget` 并提取卡片特征（ATQA+SAK+UID）至 `Payload`。
*   **目录结构**:
    *   `Models/`: 存放动作枚举与报文模型。
    *   `Protocols/`: 存放具体硬件的协议映射实现。
    *   `Utility/`: 存放 NFC 专用的扩展辅助逻辑。