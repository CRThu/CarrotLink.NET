using CarrotLink.Core.Discovery.Interfaces;
using CarrotLink.Core.Discovery.Models;
using NationalInstruments.VisaNS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Discovery.Searchers
{
    public class NiVisaSearcher : IDeviceSearcher
    {
        public DeviceType SupportedType => DeviceType.Gpib;
        public NiVisaSearcher()
        {
            Assembly.LoadFrom("NationalInstruments.Common.dll");
            Assembly.LoadFrom("NationalInstruments.VisaNS.dll");
        }

        public IEnumerable<DeviceInfo> Search()
        {
            try
            {
                string expression = "?*";
                //string expression = "GPIB?*INSTR";
                string[] res = ResourceManager.GetLocalManager().FindResources(expression);
                return res.Select(resourceName => new DeviceInfo() {
                    Interface = "VISA",
                    Name = resourceName,
                    Description = "NI-VISA DEVICE"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Array.Empty<DeviceInfo>();
            }
        }
    }
}
