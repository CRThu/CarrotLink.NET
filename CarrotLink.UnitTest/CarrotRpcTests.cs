using CarrotLink.Core.Protocols.Configuration;
using CarrotLink.Core.Protocols.Impl;
using CarrotLink.Core.Protocols.Models;
using System.Buffers;
using System.Text;

namespace CarrotLink.UnitTest
{
    [TestClass]
    public class CarrotRpcTests
    {
        private CarrotAsciiProtocol _protocol;

        [TestInitialize]
        public void Init()
        {
            _protocol = new CarrotAsciiProtocol(new CarrotAsciiProtocolConfiguration()
            {
                RegfilesCommands = new CarrotAsciiProtocolRegfileCommands[] {
                    new CarrotAsciiProtocolRegfileCommands {Name = "file0", ReadRegCommand="RF0R" },
                    new CarrotAsciiProtocolRegfileCommands {Name = "file1", ReadRegCommand="RF1R" }
                }
            });
        }

        [TestMethod]
        public void Test_CommandPacket_Parsing()
        {
            // System Ready. -> 应识别为 CommandPacket
            var buffer = CreateBuffer("System Ready.\r\n");
            Assert.IsTrue(_protocol.TryDecode(ref buffer, out var packet));
            Assert.IsInstanceOfType(packet, typeof(CommandPacket));
            Assert.AreEqual("System Ready.", packet.ToString());
        }

        [TestMethod]
        public void Test_CommandPacket_Log_Parsing()
        {
            // [INFO]: System Ready. -> 应识别为 CommandPacket
            var buffer = CreateBuffer("[INFO]: System Ready.\r\n");
            Assert.IsTrue(_protocol.TryDecode(ref buffer, out var packet));
            Assert.IsInstanceOfType(packet, typeof(CommandPacket));
            Assert.AreEqual("[INFO]: System Ready.", packet.ToString());
        }

        [TestMethod]
        public void Test_DataPacket_SingleValue()
        {
            // [DATA]: 25.4 -> Channel: CH0, Val: 25.4
            var buffer = CreateBuffer("[DATA]: 25.4\r\n");
            _protocol.TryDecode(ref buffer, out var packet);
            var data = (DataPacket)packet;
            Assert.AreEqual("CH0", data.Keys[0]);
            Assert.AreEqual(25.4, data.Get<double>("CH0")[0]);
        }

        [TestMethod]
        public void Test_DataPacket_SingleValueWithPath()
        {
            // [DATA.TEMP]: 25.4 -> Channel: TEMP, Val: 25.4
            var buffer = CreateBuffer("[DATA.TEMP]: 25.4\r\n");
            _protocol.TryDecode(ref buffer, out var packet);
            var data = (DataPacket)packet;
            Assert.AreEqual("TEMP", data.Keys[0]);
            Assert.AreEqual(25.4, data.Get<double>("TEMP")[0]);
        }

        [TestMethod]
        public void Test_DataPacket_SingleValueWithChannel()
        {
            // [DATA]: temp=25.4 -> Channel: temp, Val: 25.4
            var buffer = CreateBuffer("[DATA]: temp=25.4\r\n");
            _protocol.TryDecode(ref buffer, out var packet);
            var data = (DataPacket)packet;
            Assert.AreEqual("temp", data.Keys[0]);
            Assert.AreEqual(25.4, data.Get<double>("temp")[0]);
        }

        [TestMethod]
        public void Test_DataPacket_SingleValueWithPathAndChannel()
        {
            // [DATA.TEMP]: tmp117=25.4 -> Channel: TEMP.tmp117, Val: 25.4
            var buffer = CreateBuffer("[DATA.TEMP]: tmp117=25.4\r\n");
            _protocol.TryDecode(ref buffer, out var packet);
            var data = (DataPacket)packet;
            Assert.AreEqual("TEMP.tmp117", data.Keys[0]);
            Assert.AreEqual(25.4, data.Get<double>("TEMP.tmp117")[0]);
        }

        [TestMethod]
        public void Test_DataPacket_MultiValue()
        {
            // [DATA]: 25.4, 70.0 -> Channel: CH0, Val: 25.4, Channel: CH1, Val: 70.0
            var buffer = CreateBuffer("[DATA]: 25.4, 70.0\r\n");
            _protocol.TryDecode(ref buffer, out var packet);
            var data = (DataPacket)packet;
            Assert.AreEqual("CH0", data.Keys[0]);
            Assert.AreEqual(25.4, data.Get<double>("CH0")[0]);
            Assert.AreEqual("CH1", data.Keys[1]);
            Assert.AreEqual(70.0, data.Get<double>("CH1")[0]);
        }

        [TestMethod]
        public void Test_DataPacket_MultiValueWithChannel()
        {
            // [DATA]: p=1.2,r=0.5 -> Channels: p, r
            var buffer = CreateBuffer("[DATA]: p=1.2,r=0.5\r\n");
            _protocol.TryDecode(ref buffer, out var packet);
            var data = (DataPacket)packet;
            Assert.AreEqual("p", data.Keys[0]);
            Assert.AreEqual("r", data.Keys[1]);
            Assert.AreEqual(1.2, data.Get<double>("p")[0]);
            Assert.AreEqual(0.5, data.Get<double>("r")[0]);
        }

        [TestMethod]
        public void Test_DataPacket_MultiValueWithPath()
        {
            // [DATA.IMU]: 1.2,0.5 -> Channels: IMU.CH0, IMU.CH1
            var buffer = CreateBuffer("[DATA.IMU]: 1.2,0.5\r\n");
            _protocol.TryDecode(ref buffer, out var packet);
            var data = (DataPacket)packet;
            Assert.AreEqual("IMU.CH0", data.Keys[0]);
            Assert.AreEqual("IMU.CH1", data.Keys[1]);
            Assert.AreEqual(1.2, data.Get<double>("IMU.CH0")[0]);
            Assert.AreEqual(0.5, data.Get<double>("IMU.CH1")[0]);
        }

        [TestMethod]
        public void Test_DataPacket_MultiValueWithPathAndChannel()
        {
            // [DATA.IMU]: p=1.2,r=0.5 -> Channels: IMU.p, IMU.r
            var buffer = CreateBuffer("[DATA.IMU]: p=1.2,r=0.5\r\n");
            _protocol.TryDecode(ref buffer, out var packet);
            var data = (DataPacket)packet;
            Assert.AreEqual("IMU.p", data.Keys[0]);
            Assert.AreEqual("IMU.r", data.Keys[1]);
            Assert.AreEqual(1.2, data.Get<double>("IMU.p")[0]);
            Assert.AreEqual(0.5, data.Get<double>("IMU.r")[0]);
        }

        [TestMethod]
        public void Test_RegisterPacket_ReadResult_Legacy_Parsing()
        {
            // tx: REGFILE1 REG READ 0x1234
            // rx: [REG]: 0x00FF
            var req = new RegisterPacket(RegisterOperation.ReadRequest, 1, 0x1234);
            _protocol.Encode(req);

            var buffer = CreateBuffer("[REG]: 0x00FF\r\n");
            _protocol.TryDecode(ref buffer, out var packet);
            var reg = (RegisterPacket)packet;
            Assert.AreEqual(RegisterOperation.ReadResult, reg.Operation);
            Assert.AreEqual(0x1u, reg.Regfile);
            Assert.AreEqual(0x1234u, reg.Address);
            Assert.AreEqual(0x00FFu, reg.Value);
        }

        [TestMethod]
        public void Test_RegisterPacket_ReadResult_Parsing()
        {
            // [REG.0x4001]: 0x00FF
            var buffer = CreateBuffer("[REG.0x4001]: 0x00FF\r\n");
            _protocol.TryDecode(ref buffer, out var packet);
            var reg = (RegisterPacket)packet;
            Assert.AreEqual(RegisterOperation.ReadResult, reg.Operation);
            Assert.AreEqual(0x4001u, reg.Address);
            Assert.AreEqual(0x00FFu, reg.Value);
        }

        [TestMethod]
        public void Test_RegisterPacket_ReadResult_Regfile_Parsing()
        {
            // [REG.file1.0x4001]: 0x00FF
            var buffer = CreateBuffer("[REG.file1.0x4001]: 0x00FF\r\n");
            _protocol.TryDecode(ref buffer, out var packet);
            var reg = (RegisterPacket)packet;
            Assert.AreEqual(RegisterOperation.ReadResult, reg.Operation);
            Assert.AreEqual(0x1u, reg.Regfile);
            Assert.AreEqual(0x4001u, reg.Address);
            Assert.AreEqual(0x00FFu, reg.Value);
        }

        [TestMethod]
        public void Test_RegisterPacket_BitsReadResult_Legacy_Parsing()
        {
            // tx: REGFILE1 REG READ 0x1234 b3_1
            // rx: [REG]: 0x00FF
            var req = new RegisterPacket(RegisterOperation.BitsReadRequest, 1, 0x1234, 1, 3);
            _protocol.Encode(req);

            var buffer = CreateBuffer("[REG]: 0x00FF\r\n");
            _protocol.TryDecode(ref buffer, out var packet);
            var reg = (RegisterPacket)packet;
            Assert.AreEqual(RegisterOperation.BitsReadResult, reg.Operation);
            Assert.AreEqual(0x1u, reg.Regfile);
            Assert.AreEqual(0x1234u, reg.Address);
            Assert.AreEqual(0x1u, reg.StartBit);
            Assert.AreEqual(0x3u, reg.EndBit);
            Assert.AreEqual(0x00FFu, reg.Value);
        }

        [TestMethod]
        public void Test_RegisterPacket_BitsReadResult_Parsing()
        {
            // [REG.0x10.b7_4]: 0xA
            var buffer = CreateBuffer("[REG.0x10.b7_4]: 0xA\r\n");
            _protocol.TryDecode(ref buffer, out var packet);
            var reg = (RegisterPacket)packet;
            Assert.AreEqual(RegisterOperation.BitsReadResult, reg.Operation);
            Assert.AreEqual(0x0u, reg.Regfile);
            Assert.AreEqual(0x10u, reg.Address);
            Assert.AreEqual(7u, reg.EndBit);
            Assert.AreEqual(4u, reg.StartBit);
            Assert.AreEqual(0xAu, reg.Value);
        }

        [TestMethod]
        public void Test_RegisterPacket_BitsReadResult_Regfile_Parsing()
        {
            // [REG.file1.0x10.b7_4]: 0xA
            var buffer = CreateBuffer("[REG.file1.0x10.b7_4]: 0xA\r\n");
            _protocol.TryDecode(ref buffer, out var packet);
            var reg = (RegisterPacket)packet;
            Assert.AreEqual(RegisterOperation.BitsReadResult, reg.Operation);
            Assert.AreEqual(0x1u, reg.Regfile);
            Assert.AreEqual(0x10u, reg.Address);
            Assert.AreEqual(7u, reg.EndBit);
            Assert.AreEqual(4u, reg.StartBit);
            Assert.AreEqual(0xAu, reg.Value);
        }

        private ReadOnlySequence<byte> CreateBuffer(string s) => new(Encoding.ASCII.GetBytes(s));
    }
}
