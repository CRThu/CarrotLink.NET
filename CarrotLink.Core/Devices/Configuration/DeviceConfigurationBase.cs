using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Devices.Configuration
{
    public abstract class DeviceConfigurationBase
    {
        public required string DeviceId { get; set; } = "<devid>";

        public int Timeout { get; set; } = 5000;

        public virtual void Validate()
        {

        }
    }
}
