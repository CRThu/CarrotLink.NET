# /// script
# dependencies = [
#   "pyserial",
#   "loguru",
# ]
# ///

import serial
import time
from loguru import logger
import sys

# 配置 loguru 输出格式 (简洁且包含颜色)
logger.remove()
logger.add(sys.stderr, format="<green>{time:HH:mm:ss.SSS}</green> | <level>{level: <8}</level> | {message}", colorize=True)

class PN532_HSU:
    def __init__(self, port="COM3", baudrate=115200):
        # 初始化串口
        try:
            self.ser = serial.Serial(port, baudrate, timeout=1)
            print(f"成功连接串口: {port}")
        except Exception as e:
            print(f"无法打开串口: {e}")
            exit()

    def send_frame(self, data):
        """封装并发送 NXP 标准帧"""
        # 数据长度 = TFI (1字节) + DATA
        length = len(data) + 1 
        # LCS (长度校验和): LEN + LCS = 0x00
        lcs = (256 - length) & 0xFF
        # TFI (方向): 上位机到PN532固定为 0xD4
        tfi = 0xD4
        # DCS (数据校验和): TFI + DATA + DCS = 0x00
        dcs = (256 - (tfi + sum(data))) & 0xFF
        
        # 帧结构: 00 00 FF [LEN] [LCS] [TFI] [DATA] [DCS] 00
        frame = bytearray([0x00, 0x00, 0xFF, length, lcs, tfi]) + bytearray(data) + bytearray([dcs, 0x00])
        self.ser.write(frame)
        logger.opt(colors=True).info(f"<cyan>TX -> {frame.hex(' ').upper()}</cyan>")

    def read_frame(self):
        """读取并解析回复帧"""
        # 等待 ACK (00 00 FF 00 FF 00)
        ack = self.ser.read(6)
        if len(ack) > 0:
            logger.opt(colors=True).info(f"<magenta>RX <- {ack.hex(' ').upper()} (ACK)</magenta>")
        
        if ack != b'\x00\x00\xff\x00\xff\x00':
            return None
        
        # 读取数据帧头
        header = self.ser.read(3) # 00 00 FF
        if len(header) < 3: return None
        
        length = self.ser.read(1)[0]
        lcs = self.ser.read(1)[0]
        tfi = self.ser.read(1)[0] # 应为 0xD5
        data = self.ser.read(length - 1)
        dcs = self.ser.read(1)[0]
        post = self.ser.read(1)[0]
        
        full_frame = bytearray(header) + bytearray([length, lcs, tfi]) + bytearray(data) + bytearray([dcs, post])
        logger.opt(colors=True).info(f"<magenta>RX <- {full_frame.hex(' ').upper()}</magenta>")
        
        return data

    def wakeup(self):
        """发送唤醒序列"""
        wake_cmd = bytearray([0x55, 0x55, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x03, 0xFD, 0xD4, 0x14, 0x01, 0x17, 0x00])
        self.ser.write(wake_cmd)
        logger.opt(colors=True).info(f"<cyan>TX -> {wake_cmd.hex(' ').upper()} (WAKEUP)</cyan>")
        time.sleep(0.1)
        self.ser.flushInput()
        logger.success("唤醒 PN532 完成")

    def get_firmware(self):
        """获取固件版本"""
        self.send_frame([0x02])
        res = self.read_frame()
        if res:
            logger.success(f"PN532 固件版本: {res.hex(' ').upper()}")
            return True
        return False

    def sam_config(self):
        """配置 SAM 为普通模式 (Normal Mode)"""
        self.send_frame([0x14, 0x01, 0x00]) # SAMConfiguration, Normal Mode
        return self.read_frame()

    def poll_card(self):
        """寻卡 (ISO14443A) 修正版"""
        # 0x4A (InListPassiveTarget), 0x01 (只寻1张卡), 0x00 (106 kbps Type A)
        self.send_frame([0x4A, 0x01, 0x00])
        res = self.read_frame()
        
        # res[0] 是指令回复 0x4B, res[1] 是卡片数量
        if res and len(res) > 1 and res[1] > 0:
            # 根据文档严格解析：
            # res[5] 是 SAK (Select Acknowledge)
            # res[6] 是 UID 长度
            # res[7...] 才是 UID
            sak = res[5]
            uid_len = res[6]
            uid = res[7 : 7 + uid_len]
            
            # 判断卡片类型
            card_type = "未知类型"
            if sak == 0x00:
                card_type = "NTAG / Mifare Ultralight"
            elif sak == 0x08:
                card_type = "Mifare Classic 1K"
            elif sak == 0x18:
                card_type = "Mifare Classic 4K"
            elif sak == 0x20:
                card_type = "ISO14443-4 兼容卡 (如 CPU卡)"

            logger.success(f"发现卡片! 类型: <bold>{card_type}</bold>")
            logger.info(f"UID: {uid.hex(' ').upper()} | SAK: 0x{sak:02X}")
            print("-" * 30)
            return uid, sak
        return None, None

if __name__ == "__main__":
    nfc = PN532_HSU(port="COM20")
    nfc.wakeup()
    if nfc.get_firmware():
        nfc.sam_config()
        logger.info("开始寻卡...")
        while True:
            nfc.poll_card()
            time.sleep(0.5) # 降低 CPU 占用