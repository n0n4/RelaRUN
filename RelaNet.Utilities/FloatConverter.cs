using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace RelaNet.Utilities
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FloatConverter
    {
        [FieldOffset(0)]
        public float FloatValue;

        [FieldOffset(0)]
        public byte Byte0;
        [FieldOffset(1)]
        public byte Byte1;
        [FieldOffset(2)]
        public byte Byte2;
        [FieldOffset(3)]
        public byte Byte3;
    }
}
