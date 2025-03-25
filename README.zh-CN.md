## CarrotLink.NET 通信框架

🌐 Language: [English](README.md) | [中文](README.zh-CN.md)

![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)

---

CarrotLink.NET 是一个基于.NET平台的设备通信框架，支持多种通信协议和设备类型，包括串口、FTDI、NI-VISA等。

## 功能特性

- **多设备支持**：支持串口、FTDI、NI-VISA等多种设备类型
- **协议解析**：内置多种通信协议解析器（如RawAsciiProtocol、CarrotDataProtocol）
- **服务调度**：提供手动、定时、事件三种触发模式
- **数据存储**：支持内存存储和数据导出为JSON格式
- **日志记录**：提供控制台日志和NLog日志支持

## 项目结构

```
CarrotLink.NET/
├── CarrotLink.Client/          # 客户端示例程序
├── CarrotLink.Core/            # 核心功能模块
│   ├── Devices/                # 设备相关实现
│   ├── Discovery/              # 设备发现功能
│   ├── Protocols/              # 通信协议实现
│   ├── Services/               # 服务实现
│   └── Utility/                # 工具类
├── CarrotLink.Native/          # 本地库
└── CarrotLink.Old/             # 旧版本代码
```

## 快速开始

1. **克隆仓库**
   ```bash
   git clone https://github.com/CRThu/CarrotLink.NET.git
   ```

2. **使用Visual Studio打开解决方案**
   - 打开`CarrotLink.NET.sln`文件
   - 设置`CarrotLink.Client`为启动项目

3. **运行示例**
   - 示例程序演示了如何使用串口设备进行通信和数据存储

## 构建

使用Visual Studio 2022或更高版本打开解决方案并构建项目。

## 许可证

Apache License 2.0

## 贡献

欢迎提交问题和拉取请求。

---

*本README由DeepSeek生成*