import ctypes
from ctypes import *

# 加载DLL
native = ctypes.CDLL(
    r"D:\Projects\CarrotLink.NET\CarrotLink.Native\bin\Release\net8.0\win-x64\publish\CarrotLink.Native.dll")

# 定义函数原型
native.init_serial_device.argtypes = [
    POINTER(c_byte), c_int,  # deviceId (指针 + 长度)
    POINTER(c_byte), c_int,  # portName (指针 + 长度)
    c_int,  # baudRate
    POINTER(c_void_p)  # deviceHandle (输出参数)
]
native.init_serial_device.restype = c_int

# 准备参数：将字符串转换为字节缓冲区
device_id = create_string_buffer(b"Serial-COM250")
port_name = create_string_buffer(b"COM250")
handle = c_void_p()

# 调用函数：使用 cast 转换为 POINTER(c_byte)
result = native.init_serial_device(
    cast(device_id, POINTER(c_byte)), len(device_id.value) - 1,  # 排除末尾的\0
    cast(port_name, POINTER(c_byte)), len(port_name.value) - 1,
    115200,
    byref(handle)  # 传递指针的指针
)

if result == 0:
    print(f"Device initialized! Handle: {handle.value}")
else:
    print("Initialization failed.")
