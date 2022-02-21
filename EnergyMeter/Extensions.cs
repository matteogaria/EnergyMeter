using System;
using System.IO;
using System.Linq;
using System.Text;

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

        public static byte[] ParseBytes(this string hex)
        {
            hex = hex.Replace(" ", "");
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }

    public static class Binary_Extensions
    {
        public static void WriteUInt16Reverse(this BinaryWriter bw, ushort x)
        {
            bw.Write((ushort)((ushort)((x & 0xff) << 8) | ((x >> 8) & 0xff)));
        }

        public static ushort ReadUInt16Reverse(this BinaryReader br)
        {
            ushort x = br.ReadUInt16();
            return (ushort)((ushort)((x & 0xff) << 8) | ((x >> 8) & 0xff));
        }

        public static uint ReadUInt32Reverse(this BinaryReader br)
        {
            uint low = br.ReadUInt16Reverse();
            uint hi = br.ReadUInt16Reverse();

            return (uint)low | (hi << 16);
        }
    }
}
