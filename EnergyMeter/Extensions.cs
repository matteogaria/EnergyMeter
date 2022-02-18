using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnergyMeter
{
    public static class ByteArray_Extensions
    {
        public static string ToHex(this byte[] bytes, bool spacing = true)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var b in bytes)
                sb.Append(b.ToString("X2") + (spacing ? " " : ""));

            return sb.ToString();
        }
    }
}
