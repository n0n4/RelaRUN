using RelaStructures;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots
{
    public class SnapHistory<T> : IPoolable where T : struct
    {
        public ushort EntityId;

        public T[] Shots;
        public ushort[] Timestamps;
        public byte[] Flags;
        public int LeadingIndex = 0;

        public int PoolId = 0;


        public SnapHistory(int length)
        {
            Shots = new T[length];
            Timestamps = new ushort[length];
            Flags = new byte[length];
        }

        public void Clear()
        {
            for (int i = 0; i < Flags.Length; i++)
                Flags[i] = 0;
        }

        public int GetPoolIndex()
        {
            return PoolId;
        }

        public void SetPoolIndex(int index)
        {
            PoolId = index;
        }

        public int FindIndex(ushort timestamp)
        {
            int i = 0;
            // okay so the hard part here is, what if the timestamps are more than
            // length apart?

            int dif = timestamp - Timestamps[LeadingIndex];
        }
    }
}
