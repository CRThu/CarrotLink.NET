using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Utility
{
    public static class SpanEx
    {
        /// <summary>
        /// 从 ReadOnlySpan<char> 高效解析浮点数，支持十进制和 "0x" 前缀的十六进制。
        /// </summary>
        public static bool TryParseNumSpan(this ReadOnlySpan<char> span, out double result)
        {
            if (span.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (ulong.TryParse(span.Slice(2), NumberStyles.HexNumber, null, out ulong hexValue))
                {
                    result = (double)hexValue;
                    return true;
                }
            }

            return double.TryParse(span, out result);
        }

        /// <summary>=
        /// 从 ReadOnlySpan<char> 高效解析16进制64位无符号数，支持"0x" 前缀
        /// </summary>
        /// <param name="span">包含16进制字符的span</param>
        /// <param name="result">解析成功后的ulong结果</param>
        /// <returns>是否解析成功</returns>
        public static bool TryParseHexSpan(this ReadOnlySpan<char> span, out ulong result)
        {
            if (span.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                span = span.Slice(2);
            }
            return ulong.TryParse(span, NumberStyles.HexNumber, null, out result);
        }
    }
}
