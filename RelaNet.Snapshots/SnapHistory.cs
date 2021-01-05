﻿using RelaStructures;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots
{
    public class SnapHistory<T> : IPoolable where T : struct
    {
        public ushort EntityId;
        public bool First = false;

        public T[] Shots;
        public ushort[] Timestamps;
        public byte[] Flags;
        public int LeadingIndex = 0;

        public int PoolId = 0;

        public const byte FlagEmpty = 0;
        public const byte FlagGold = 1; // from server
        public const byte FlagSilver = 2; // extrapolated, we made it up, it's fiction
        public const byte FlagDeghosted = 3;

        public T Prev;
        public ushort PrevTimestamp;
        public byte PrevFlag;
        public int PrevIndex;

        public T Current;
        public ushort CurrentTimestamp;
        public byte CurrentFlag;
        public int CurrentIndex;

        public T Next;
        public ushort NextTimestamp;
        public byte NextFlag;
        public int NextIndex;

        public SnapHistory(int length, bool first)
        {
            Shots = new T[length];
            Timestamps = new ushort[length];
            Flags = new byte[length];
            First = first;
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
            int index = LeadingIndex + (timestamp - Timestamps[LeadingIndex]);

            // if the timestamp is ahead of the leading edge,
            // it can only be so far (half of our storage window)
            // ahead before we have to reject it.

            // if the timestamp is behind our leading edge,
            // the same logic applies
            
            // half of the storage window is reserved for future
            // timestamps, and half is reserved for past timestamps.
            if (index > LeadingIndex + (Shots.Length / 2))
                return -1;

            if (index < LeadingIndex - (Shots.Length / 2))
                return -1;

            // allow up to one overflow in either direction
            // our consumers *must* handle the case of double overflow
            // and fail properly in that case (ret = -1)
            if (index >= Shots.Length)
            {
                index -= Shots.Length;
                if (index >= LeadingIndex)
                    return -1; // too far in the future!
            }
            else if (index < 0)
            {
                index += Shots.Length;
                if (index <= LeadingIndex)
                    return -1; // too far in the past!
            }
            return index;
        }

        public bool LoadCurrentByTimestamp(ushort timestamp)
        {
            int index = FindIndex(timestamp);
            if (index < 0)
                return false;
            LoadCurrent(index);
            return true;
        }

        public void LoadCurrent(int index)
        {
            CurrentTimestamp = Timestamps[index];
            CurrentIndex = index;
            CurrentFlag = Flags[index];
            Current = Shots[index];


            // load next
            NextIndex = index + 1;
            if (NextIndex == Shots.Length)
                NextIndex = 0;

            ushort expectedNextTimestamp = CurrentTimestamp;
            if (expectedNextTimestamp == ushort.MaxValue)
                expectedNextTimestamp = 0;
            else
                expectedNextTimestamp++;

            NextTimestamp = Timestamps[NextIndex];

            // if the next isn't what we expect, treat it as if it's empty.
            if (NextTimestamp != expectedNextTimestamp)
            {
                NextTimestamp = expectedNextTimestamp;
                NextFlag = FlagEmpty;
            }
            else
            {
                NextFlag = Flags[NextIndex];
                Next = Shots[NextIndex];
            }


            // load prev
            PrevIndex = index + 1;
            if (PrevIndex == Shots.Length)
                PrevIndex = 0;

            ushort expectedPrevTimestamp = CurrentTimestamp;
            if (expectedPrevTimestamp == 0)
                expectedPrevTimestamp = ushort.MaxValue;
            else
                expectedPrevTimestamp--;

            PrevTimestamp = Timestamps[PrevIndex];

            // if the next isn't what we expect, treat it as if it's empty.
            if (PrevTimestamp != expectedPrevTimestamp)
            {
                PrevTimestamp = expectedPrevTimestamp;
                PrevFlag = FlagEmpty;
            }
            else
            {
                PrevFlag = Flags[PrevIndex];
                Prev = Shots[PrevIndex];
            }
        }
    }
}
