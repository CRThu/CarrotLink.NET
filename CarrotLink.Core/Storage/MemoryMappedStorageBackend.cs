using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Storage
{
    public class MemoryMappedStorageBackend<T> : IStorageBackend<T>
    {
        public long Count => throw new NotImplementedException();

        public void Enquene(T item)
        {
            throw new NotImplementedException();
        }

        public bool TryDequeue(out T? data)
        {
            throw new NotImplementedException();
        }
    }
}
