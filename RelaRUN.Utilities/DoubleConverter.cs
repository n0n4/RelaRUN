using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace RelaRUN.Utilities
{
    [StructLayout(LayoutKind.Explicit)]
    public struct DoubleConverter
    {
        [FieldOffset(0)]
        public double DoubleValue;

        [FieldOffset(0)]
        public byte Byte0;
        [FieldOffset(1)]
        public byte Byte1;
        [FieldOffset(2)]
        public byte Byte2;
        [FieldOffset(3)]
        public byte Byte3;
        [FieldOffset(4)]
        public byte Byte4;
        [FieldOffset(5)]
        public byte Byte5;
        [FieldOffset(6)]
        public byte Byte6;
        [FieldOffset(7)]
        public byte Byte7;
    }
}
