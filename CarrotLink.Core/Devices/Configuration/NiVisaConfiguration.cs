using NationalInstruments.VisaNS;
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

        public virtual void ApplySettings(MessageBasedSession session)
        {
            if (Timeout > 0)
            {
                Timeout = session.Timeout;
            }

            //session.TerminationCharacterEnabled = true;
            //session.TerminationCharacter = (byte)'\n';
            //if (session is SerialSession serialSession)
            //{
            //}
            //else if (session is GpibSession gpibSession)
            //{
            //}
        }
    }
}
