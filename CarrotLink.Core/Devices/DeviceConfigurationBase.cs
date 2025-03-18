using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Devices
{
    public abstract class DeviceConfigurationBase
    {
        public required string DeviceId { get; set; } = "<devid>";

        public int timeout { get; set; } = 5000;

        public virtual void Validate()
        {

        }
    }
}
