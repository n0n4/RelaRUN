using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.FlatSnap
{
    public class FlatSnapData
    {
        public int Index;
        public uint Time;
        public float[] Floats;
        public byte[] Bytes;
        public ushort[] UShorts;
        public int[] Ints;

        public int FloatsPer;
        public int BytesPer;
        public int UShortsPer;
        public int IntsPer;

        public float[] NonNetFloats;
        public byte[] NonNetBytes;
        public ushort[] NonNetUShorts;
        public int[] NonNetInts;

        public int NonNetFloatsPer;
        public int NonNetBytesPer;
        public int NonNetUShortsPer;
        public int NonNetIntsPer;

        public FlatSnapData(int index, uint time, int max,
            int floatsPer, int bytesPer, int ushortsPer, int intsPer,
            int nonNetFloatsPer, int nonNetBytesPer, int nonNetUShortsPer, int nonNetIntsPer)
        {
            Index = index;
            Time = time;
            Floats = new float[max * floatsPer];
            Bytes = new byte[max * bytesPer];
            UShorts = new ushort[max * ushortsPer];
            Ints = new int[max * intsPer];

            FloatsPer = floatsPer;
            BytesPer = bytesPer;
            UShortsPer = ushortsPer;
            IntsPer = intsPer;

            NonNetFloats = new float[max * nonNetFloatsPer];
            NonNetBytes = new byte[max * nonNetBytesPer];
            NonNetUShorts = new ushort[max * nonNetUShortsPer];
            NonNetInts = new int[max * nonNetIntsPer];

            NonNetFloatsPer = nonNetFloatsPer;
            NonNetBytesPer = nonNetBytesPer;
            NonNetUShortsPer = nonNetUShortsPer;
            NonNetIntsPer = nonNetIntsPer;
        }

        public void Clear()
        {
            for (int i = 0; i < Floats.Length; i++)
                Floats[i] = default;

            for (int i = 0; i < Bytes.Length; i++)
                Bytes[i] = default;

            for (int i = 0; i < UShorts.Length; i++)
                UShorts[i] = default;

            for (int i = 0; i < Ints.Length; i++)
                Ints[i] = default;

            for (int i = 0; i < NonNetFloats.Length; i++)
                NonNetFloats[i] = default;

            for (int i = 0; i < NonNetBytes.Length; i++)
                NonNetBytes[i] = default;

            for (int i = 0; i < NonNetUShorts.Length; i++)
                NonNetUShorts[i] = default;

            for (int i = 0; i < NonNetInts.Length; i++)
                NonNetInts[i] = default;
        }

        public void Copy(FlatSnapData target, int max)
        {
            Buffer.BlockCopy(Floats, 0, target.Floats, 0, 4 * max * FloatsPer);
            Buffer.BlockCopy(Bytes, 0, target.Bytes, 0, max * BytesPer);
            Buffer.BlockCopy(UShorts, 0, target.UShorts, 0, 2 * max * UShortsPer);
            Buffer.BlockCopy(Ints, 0, target.Ints, 0, 4 * max * IntsPer);

            Buffer.BlockCopy(NonNetFloats, 0, target.NonNetFloats, 0, 4 * max * NonNetFloatsPer);
            Buffer.BlockCopy(NonNetBytes, 0, target.NonNetBytes, 0, max * NonNetBytesPer);
            Buffer.BlockCopy(NonNetUShorts, 0, target.NonNetUShorts, 0, 2 * max * NonNetUShortsPer);
            Buffer.BlockCopy(NonNetInts, 0, target.NonNetInts, 0, 4 * max * NonNetIntsPer);
        }

        public void CopyEntityNonNet(FlatSnapData target, int entityId)
        {
            Buffer.BlockCopy(NonNetFloats, entityId * NonNetFloatsPer, target.NonNetFloats, entityId * NonNetFloatsPer, 4 * NonNetFloatsPer);
            Buffer.BlockCopy(NonNetBytes, entityId * NonNetBytesPer, target.NonNetBytes, entityId * NonNetBytesPer, NonNetBytesPer);
            Buffer.BlockCopy(NonNetUShorts, entityId * NonNetUShortsPer, target.NonNetUShorts, entityId * NonNetUShortsPer, 2 * NonNetUShortsPer);
            Buffer.BlockCopy(NonNetInts, entityId * NonNetIntsPer, target.NonNetInts, entityId * NonNetIntsPer, 4 * NonNetIntsPer);
        }

        public void Resize(int newMax)
        {
            float[] oldFloats = Floats;
            byte[] oldBytes = Bytes;
            ushort[] oldUShorts = UShorts;
            int[] oldInts = Ints;

            Floats = new float[newMax * FloatsPer];
            Bytes = new byte[newMax * BytesPer];
            UShorts = new ushort[newMax * UShortsPer];
            Ints = new int[newMax * IntsPer];

            for (int i = 0; i < oldFloats.Length; i++)
                Floats[i] = oldFloats[i];

            for (int i = 0; i < oldBytes.Length; i++)
                Bytes[i] = oldBytes[i];

            for (int i = 0; i < oldUShorts.Length; i++)
                UShorts[i] = oldUShorts[i];

            for (int i = 0; i < oldInts.Length; i++)
                Ints[i] = oldInts[i];

            float[] oldNonNetFloats = NonNetFloats;
            byte[] oldNonNetBytes = NonNetBytes;
            ushort[] oldNonNetUShorts = NonNetUShorts;
            int[] oldNonNetInts = NonNetInts;

            NonNetFloats = new float[newMax * NonNetFloatsPer];
            NonNetBytes = new byte[newMax * NonNetBytesPer];
            NonNetUShorts = new ushort[newMax * NonNetUShortsPer];
            NonNetInts = new int[newMax * NonNetIntsPer];

            for (int i = 0; i < oldNonNetFloats.Length; i++)
                NonNetFloats[i] = oldNonNetFloats[i];

            for (int i = 0; i < oldNonNetBytes.Length; i++)
                NonNetBytes[i] = oldNonNetBytes[i];

            for (int i = 0; i < oldNonNetUShorts.Length; i++)
                NonNetUShorts[i] = oldNonNetUShorts[i];

            for (int i = 0; i < oldNonNetInts.Length; i++)
                NonNetInts[i] = oldNonNetInts[i];
        }
    }
}
