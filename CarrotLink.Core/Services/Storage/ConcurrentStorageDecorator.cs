using CarrotLink.Core.Protocols.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services.Storage
{
    public class ConcurrentStorageDecorator : IDataStorage
    {
        private readonly IDataStorage _innerStorage;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public ConcurrentStorageDecorator(IDataStorage innerStorage)
        {
            _innerStorage = innerStorage ?? throw new ArgumentNullException(nameof(innerStorage));
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
    }
}
