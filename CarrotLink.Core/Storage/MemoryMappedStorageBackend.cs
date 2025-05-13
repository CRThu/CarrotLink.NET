using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CarrotLink.Core.Storage
{
    public class ChunkedStorageBackend<T> : IStorageBackend<T>, IDisposable
    {
        #region 配置参数
        private const int CHUNK_SIZE = 4 * 1024 * 1024;   // 单个区块4MB
        private const string CHUNK_PREFIX = "chunk_";     // 文件命名前缀
        #endregion

        private readonly ConcurrentDictionary<int, MemoryMappedChunk> _chunks = new();
        private MemoryMappedChunk _currentWriteChunk;
        private MemoryMappedChunk _currentReadChunk;
        private long _totalWriteCount;
        private long _totalReadCount;
        private readonly string _basePath;
        private bool _disposed;

        public ChunkedStorageBackend(string storageName)
        {
            _basePath = Path.Combine(Path.GetTempPath(), storageName);
            Directory.CreateDirectory(_basePath);

            _currentWriteChunk = GetChunk(0);
            _currentReadChunk = GetChunk(0);
        }

        public void Enquene(T item)
        {
            var data = Serialize(item);
            var required = 4 + data.Length;

            if (!_currentWriteChunk.TryWrite(data, required))
            {
                _currentWriteChunk = GetChunk(_currentWriteChunk.ChunkId + 1);
                if (!_currentWriteChunk.TryWrite(data, required))
                    throw new IndexOutOfRangeException();
            }
            Interlocked.Increment(ref _totalWriteCount);
            return;
        }

        public bool TryDequeue(out T? item)
        {
            var chunk = _currentReadChunk ?? GetChunk(0);
            if (chunk == null)
            {
                item = default;
                return false;
            }
            _currentReadChunk = chunk;

            if (!_currentReadChunk.TryRead(out item))
            {
                var newChunk = GetChunk(_currentReadChunk.ChunkId + 1);
                if (newChunk == null)
                {
                    item = default;
                    return false;
                }
                //ReleaseChunk(_currentReadChunk);
                _currentReadChunk = newChunk;
                if (!_currentReadChunk.TryRead(out item))
                    throw new IndexOutOfRangeException();
            }
            Interlocked.Increment(ref _totalReadCount);
            return true;
        }

        private MemoryMappedChunk GetChunk(int index)
        {
            MemoryMappedChunk? mmc;
            if (_chunks.TryGetValue(index, out mmc))
                return mmc;

            var path = Path.Combine(_basePath, $"{CHUNK_PREFIX}{index:D10}");
            var mmf = MemoryMappedFile.CreateFromFile(
                path, FileMode.Create, null, CHUNK_SIZE,
                MemoryMappedFileAccess.ReadWrite);
            mmc = new MemoryMappedChunk(mmf, CHUNK_SIZE, path, index);
            if (_chunks.TryAdd(index, mmc))
                return mmc;

            throw new IndexOutOfRangeException();
        }

        private void ReleaseChunk(MemoryMappedChunk chunk)
        {
            chunk.Dispose();
        }

        private byte[] Serialize(T obj)
        {
            return JsonSerializer.SerializeToUtf8Bytes(obj);
        }

        private T? Deserialize(byte[] data)
        {
            return JsonSerializer.Deserialize<T>(data);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _currentWriteChunk = null;
            _currentReadChunk = null;

            foreach (var chunk in _chunks.Values)
            {
                chunk.Dispose();
            }

            //Directory.Delete(_basePath, true);
        }

        public long Count => _totalWriteCount - _totalReadCount;

        private class MemoryMappedChunk : IDisposable
        {
            private readonly MemoryMappedFile _mmf;
            private readonly MemoryMappedViewAccessor _accessor;
            private int _writePosition;
            private int _readPosition;
            private readonly int _capacity;
            public string FilePath { get; }
            public int ChunkId { get; }

            public MemoryMappedChunk(MemoryMappedFile mmf, int capacity, string filePath, int chunkid)
            {
                _mmf = mmf;
                _accessor = mmf.CreateViewAccessor();
                _capacity = capacity;
                FilePath = filePath;
                ChunkId = chunkid;
            }

            public bool TryWrite(byte[] data, int required)
            {
                if (_writePosition + required > _capacity)
                    return false;

                _accessor.Write(_writePosition, data.Length);
                _accessor.WriteArray(_writePosition + 4, data, 0, data.Length);
                Interlocked.Add(ref _writePosition, required);
                return true;
            }

            public bool TryRead(out T? item)
            {
                if (_readPosition >= _writePosition)
                {
                    item = default;
                    return false;
                }

                var length = _accessor.ReadInt32(_readPosition);
                var data = new byte[length];
                _accessor.ReadArray(_readPosition + 4, data, 0, length);

                try
                {
                    item = JsonSerializer.Deserialize<T>(data);
                    Interlocked.Add(ref _readPosition, 4 + length);
                    return true;
                }
                catch (JsonException)
                {
                    item = default;
                    return false;
                }
            }

            public bool HasData => _writePosition > 0;

            public void Dispose()
            {
                _accessor.Dispose();
                _mmf.Dispose();
                //try
                //{
                //    if (File.Exists(FilePath))
                //        File.Delete(FilePath);
                //}
                //catch
                //{
                //    throw;
                //}
            }
        }
    }
}