using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Devices.Interfaces
{
    public interface IEventTriggerDevice
    {
        event EventHandler<byte[]> DataReceived;
    }
}
