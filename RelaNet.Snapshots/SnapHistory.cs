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

        public const byte FlagEmpty = 0;
        public const byte FlagGold = 1; // from server
        public const byte FlagSilver = 2; // extrapolated, we made it up, it's fiction
        public const byte FlagDeghosted = 3;

        public SnapHistory(int length)
        {
            Shots = new T[length];
            Timestamps = new ushort[length];
            Flags = new byte[length];
        }

        public void Clear()
        {
            for (int i = 0; i < Flags.Length; i++)
                Flags[i] = FlagEmpty;
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
            return LeadingIndex + (timestamp - Timestamps[LeadingIndex]);
        }
    }
}
