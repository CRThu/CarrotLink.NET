﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Utility
{
    public static class StringEx
    {
        public static bool TryToDouble(this string input, out double result)
        {
            if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    ulong hexValue = Convert.ToUInt64(input.Substring(2), 16);
                    result = (double)hexValue;
                    return true;
                }
                catch
                {
                    result = 0;
                    return false;
                }
            }

            return double.TryParse(input, out result);
        }
    }
}
