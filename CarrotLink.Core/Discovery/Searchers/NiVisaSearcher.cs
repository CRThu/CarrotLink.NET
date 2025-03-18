using CarrotLink.Core.Discovery.Interfaces;
using CarrotLink.Core.Discovery.Models;
using NationalInstruments.VisaNS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Discovery.Searchers
{
    public class NiVisaSearcher : IDeviceSearcher
    {
        public DeviceType SupportedType => DeviceType.Gpib;
        public NiVisaSearcher()
        {
        }

        public IEnumerable<DeviceInfo> Search()
        {
            try
            {
                string expression = "?*";
                //string expression = "GPIB?*INSTR";
                string[] res = ResourceManager.GetLocalManager().FindResources(expression);
                return res.Select(d => new DeviceInfo(/*"VISA", d, "NI-VISA DEVICE"*/)).ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return Array.Empty<DeviceInfo>();
            }
        }
    }
}
