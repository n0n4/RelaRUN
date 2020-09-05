using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace RelaNet.Utilities
{
    public static class Bits
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckBit(byte ack, int index)
        {
            return ((ack & (1 << index)) != 0);
        }

        // start with a byte of 0 and add each true bit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte AddTrueBit(byte ack, int index)
        {
            return (byte)(ack | (1 << index));
        }
    }
}
