using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services.Storage
{
    public class ConcurrentStorageDecorator : IDataStorage
    {
        public ConcurrentStorageDecorator(IDataStorage storage)
        {
        }

        public Task ExportAsCsvAsync(string filePath, IEnumerable<object> records)
        {
            throw new NotImplementedException();
        }

        public Task ExportAsJsonAsync(string filePath, object data)
        {
            throw new NotImplementedException();
        }

        public void StoreInMemory(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
