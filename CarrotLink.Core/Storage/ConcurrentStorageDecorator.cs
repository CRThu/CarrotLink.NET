using CarrotLink.Core.Protocols.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Storage
{
    public class ConcurrentStorageDecorator : IDataStorage
    {
        private readonly IDataStorage _innerStorage;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public ConcurrentStorageDecorator(IDataStorage innerStorage)
        {
            _innerStorage = innerStorage ?? throw new ArgumentNullException(nameof(innerStorage));
        }

        public bool TryRead(out IPacket? packet)
        {
            return _innerStorage.TryRead(out packet);
        }

        public async Task SaveAsync(IPacket? data)
        {
            await _semaphore.WaitAsync();
            try
            {
                await _innerStorage.SaveAsync(data);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ExportAsJsonAsync(string path)
        {
            await _semaphore.WaitAsync();
            try
            {
                await _innerStorage.ExportAsJsonAsync(path);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public IEnumerable<IPacket> GetStoredData()
        {
            return _innerStorage.GetStoredData();
        }
    }
}
