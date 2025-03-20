using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CarrotLink.Core.Devices.Configuration
{
    public class NiVisaConfiguration : DeviceConfigurationBase
    {
        public required string ResourceString { get; set; }
        public int ReadBufferSize { get; set; } = 4096;

        public override void Validate()
        {
            base.Validate();
        }
    }
}
