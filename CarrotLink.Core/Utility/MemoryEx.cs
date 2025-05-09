using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Utility
{
    public static class MemoryEx
    {
        public static byte[] ToUnsafeArray(this Memory<byte> memory)
        {
            if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> seg))
            {
                if (seg.Offset == 0 && seg.Count == seg.Array!.Length)
                {
                    return seg.Array;
                }
            }
            return memory.ToArray();
        }

        public static byte[] ToUnsafeArray(this ReadOnlyMemory<byte> memory)
        {
            if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> seg))
            {
                if (seg.Offset == 0 && seg.Count == seg.Array!.Length)
                {
                    return seg.Array;
                }
            }
            return memory.ToArray();
        }
    }
}
