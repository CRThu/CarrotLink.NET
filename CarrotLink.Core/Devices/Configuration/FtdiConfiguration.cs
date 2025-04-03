using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CarrotLink.Core.Devices.Configuration
{
    public class FtdiConfiguration : DeviceConfigurationBase
    {
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
