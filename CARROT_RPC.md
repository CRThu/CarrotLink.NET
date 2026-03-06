# CARROT RPC 协议通信手册 (v1.2.3)

本手册定义了下位机与上位机之间的远程过程调用（RPC）报文格式，包含 **ASCII 可视化 RPC** 与 **Binary 高速流 RPC**。

## 1. ASCII RPC 接口规范
所有 ASCII 报文必须以 `\r\n` (0x0D, 0x0A) 结尾。

### 1.1 系统消息接口 (`MSG`)
用于推送带状态前缀的日志信息。
*   **语法**: `[<LEVEL>]: <MESSAGE>`
*   **约定**: `<LEVEL>` 通常由调用者在字符串中内置（如 `INFO`, `WARN`, `ERROR`）。
*   **示例**: `[INFO]: System Boot Complete`

### 1.2 结构化数据接口 (`DATA`)
用于上报实时数值。
*   **语法**: `[DATA{.<PATH>}]: {<KEY>=}<VALUE>{,<KEY>=<VALUE>... }`
*   **数据约定**: `<VALUE>` 默认解析为 **double** (浮点数)。
*   **可选性**: 
    *   `.<PATH>` 是可选的，用于区分数据源（如 `DATA.IMU`）。
    *   `<KEY>=` 是可选的，用于多参数或命名参数场景。
*   **示例**:
    *   `[DATA]: 12.5` (最简格式)
    *   `[DATA.TEMP]: 25.4` (带路径)
    *   `[DATA.IMU]: pitch=1.2,roll=0.5` (完整格式)


### 1.3 寄存器访问接口 (`REG`)
用于返回寄存器当前值。支持可选的命名空间以区分不同的寄存器组（RegFile）。
*   **语法**: `[REG{.<FILE>}.0x<ADDR>]: 0x<HEX_VAL>`
*   **参数**: `.<FILE>` 为可选路径，用于区分不同的寄存器文件或外设基址。
*   **数据约定**: `<ADDR>` 和 `<HEX_VAL>` 均采用 **16 进制** 格式。
*   **示例**: 
    *   `[REG.0x4001]: 0x00FF` (默认组)
    *   `[REG.file0.0xAA]: 0x0012` (指定寄存器组)

### 1.4 位域访问接口 (`BITS`)
用于返回寄存器中特定位范围的值。
*   **语法**: `[REG{.<FILE>}.0x<ADDR>.b<END>_<START>]: 0x<HEX_VAL>`
*   **参数**: `<END>` 和 `<START>` 为位序号（10进制，如 7 和 0）。
*   **数据约定**: `<HEX_VAL>` 采用 **16 进制** 格式。
*   **示例**: 
    *   `[REG.0x10.b7_4]: 0xA`
    *   `[REG.file1.0x10.b7_4]: 0xB`

---

## 2. Binary RPC 接口规范 (高速流)
用于传输原始波形等高带宽数据，固定长度包格式。

### 2.1 报文结构 (DATA_266)
| 偏移 | 字段 | 类型 | 说明 |
| :--- | :--- | :--- | :--- |
| 0 | `START` | uint8 | 固定为 `0x3C` (`<`) |
| 1 | `ID` | uint8 | `0x42` (DATA_266) |
| 2 | `FLAGS` | uint16 | 数据元信息 (端序、位宽等) |
| 4 | `STREAM_ID` | uint8 | 逻辑通道 ID |
| 5 | `LEN` | uint16 | Payload 有效载荷长度 |
| 7 | `PAYLOAD` | uint8[256] | 原始二进制数据 |
| 263 | `CRC` | uint16 | 校验码 |
| 265 | `END` | uint8 | 固定为 `0x3E` (`>`) |

---

## 3. 开发者调用示例 (C SDK)

```c
// 1. 推送带级别的日志
write_log("INFO", "Battery Level %d%%", 85);

// 2. 推送 Double 类型数据
write_data("ENV", "temp=%.2f,humi=%.2f", 25.4, 60.1);

// 3. 返回 16 进制寄存器值 (支持可选的 file 参数)
reply_reg(NULL, 0x40, 0x1F);      // [REG.0x40]: 0x1F
reply_reg("file0", 0xAA, 0x12);   // [REG.file0.0xAA]: 0x12

// 4. 返回 16 进制位域值
reply_bits("file1", 0x40, 0, 3, 0xA); // [REG.file1.0x40.b3_0]: 0xA
```
