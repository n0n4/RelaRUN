using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.FlatSnap
{
    public class FlatSnapInput
    {
        public int Time;
        public float[] Floats;
        public byte[] Bytes;

        public FlatSnapInput(int floatCount, int byteCount)
        {
            Time = -1;
            Floats = new float[floatCount];
            Bytes = new byte[byteCount];
        }

        public void Clear()
        {
            Time = -1;
            for (int i = 0; i < Floats.Length; i++)
                Floats[i] = 0;

            for (int i = 0; i < Bytes.Length; i++)
                Bytes[i] = 0;
        }
    }
}
