using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services.Storage
{
    public interface IDataStorage
    {
        void StoreInMemory(byte[] data);
        Task ExportAsJsonAsync(string filePath, object data);
        Task ExportAsCsvAsync(string filePath, IEnumerable<object> records);
    }
}
