using CarrotLink.Core.Protocols.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services.Storage
{
    public interface IDataStorage
    {
        public IPacket? Read();
        public Task SaveAsync(IPacket? data);
        public Task ExportAsJsonAsync(string path);
    }
}
