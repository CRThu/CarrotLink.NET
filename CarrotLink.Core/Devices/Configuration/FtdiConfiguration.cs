using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CarrotLink.Core.Devices.Configuration
{
    public enum FtdiModel
    {
        Ft2232h
    }

    public enum FtdiCommMode
    {
        AsyncSerial,
        AsyncFifo,
        SyncFifo,
    }

    public class FtdiConfiguration : DeviceConfigurationBase
    {
        public FtdiModel Model { get; set; } = FtdiModel.Ft2232h;

        public FtdiCommMode Mode { get; set; } = FtdiCommMode.SyncFifo;

        /// <summary>
        /// 序列号（必需）
        /// </summary>
        [JsonPropertyName("serialNumber")]
        public required string SerialNumber { get; init; }

        public override void Validate()
        {
            base.Validate();
        }
    }
}
