using RelaStructures;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots
{
    public class Snapper<T> : ISnapper where T : struct
    {
        // Delegates
        public delegate void ClearDelegate(ref T obj);
        public delegate void UnpackFullDelegate(ref T obj, byte[] blob, int start, int count);
        public delegate void UnpackDeltaDelegate(ref T obj, T basis, byte[] blob, int start, int count);

        public ClearDelegate ClearAction;
        public UnpackFullDelegate UnpackFullAction;
        public UnpackDeltaDelegate UnpackDeltaAction;


        // defn. Entity Id
        // assigned by NetExecutorSnapper and networked
        // defn. Inner Id
        // assigned by Snapper and not networked (used in StructReArrays)

        // Entity data
        public ReArrayIdPool<SnapHistory<T>> FirstData;
        public ReArrayIdPool<SnapHistory<T>> SecondData;

        // Entity map
        public int[] FirstEntityIdToInnerId;
        public int[] SecondEntityIdToInnerId;

        public Snapper(ClearDelegate clearAction, UnpackFullDelegate unpackFullAction,
            UnpackDeltaDelegate unpackDeltaAction)
        {
            ClearAction = clearAction;
            UnpackFullAction = unpackFullAction;
            UnpackDeltaAction = unpackDeltaAction;

            // setup entity data
            FirstData = new ReArrayIdPool<SnapHistory<T>>(4, byte.MaxValue + 1,
                () =>
                {
                    return new SnapHistory<T>(32);
                },
                (s) =>
                {
                    for (int i = 0; i < s.Shots.Length; i++)
                        ClearAction(ref s.Shots[i]);
                    s.Clear();
                });

            SecondData = new ReArrayIdPool<SnapHistory<T>>(4, ushort.MaxValue + 1,
                () =>
                {
                    return new SnapHistory<T>(32);
                },
                (s) =>
                {
                    for (int i = 0; i < s.Shots.Length; i++)
                        ClearAction(ref s.Shots[i]);
                    s.Clear();
                });

            // setup entityid to innerid maps
            FirstEntityIdToInnerId = new int[8];
            for (int i = 0; i < FirstEntityIdToInnerId.Length; i++)
                FirstEntityIdToInnerId[i] = -1;

            SecondEntityIdToInnerId = new int[8];
            for (int i = 0; i < SecondEntityIdToInnerId.Length; i++)
                SecondEntityIdToInnerId[i] = -1;
        }



        // Array helpers
        private void ExpandFirstEntityIdMap(byte target)
        {
            int newlen = FirstEntityIdToInnerId.Length * 2;
            while (newlen <= target)
                newlen *= 2;

            int[] na = new int[newlen];
            for (int i = 0; i < FirstEntityIdToInnerId.Length; i++)
                na[i] = FirstEntityIdToInnerId[i];
            for (int i = FirstEntityIdToInnerId.Length; i < na.Length; i++)
                na[i] = -1;

            FirstEntityIdToInnerId = na;
        }

        private void ExpandSecondEntityIdMap(ushort target)
        {
            int newlen = SecondEntityIdToInnerId.Length * 2;
            while (newlen <= target)
                newlen *= 2;

            int[] na = new int[newlen];
            for (int i = 0; i < SecondEntityIdToInnerId.Length; i++)
                na[i] = SecondEntityIdToInnerId[i];
            for (int i = SecondEntityIdToInnerId.Length; i < na.Length; i++)
                na[i] = -1;

            SecondEntityIdToInnerId = na;
        }



        // Ghosting
        public bool GhostFirst(byte entityid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp)
        {
            if (FirstData.Count > byte.MaxValue)
                return false;

            if (entityid > FirstEntityIdToInnerId.Length)
                ExpandFirstEntityIdMap(entityid);

            FirstEntityIdToInnerId[entityid] = 
                (byte)Ghost(FirstData,
                    entityid,
                    blob, blobstart, blobcount,
                    timestamp);

            return true;
        }

        public bool GhostSecond(ushort entityid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp)
        {
            if (SecondData.Count > ushort.MaxValue)
                return false;

            if (entityid > SecondEntityIdToInnerId.Length)
                ExpandSecondEntityIdMap(entityid);

            SecondEntityIdToInnerId[entityid] = 
                (ushort)Ghost(SecondData,
                    entityid,
                    blob, blobstart, blobcount,
                    timestamp);

            return true;
        }

        private int Ghost(ReArrayIdPool<SnapHistory<T>> data,
            ushort entityid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp)
        {
            SnapHistory<T> sh = data.Request();

            sh.EntityId = entityid;
            UnpackFullAction(ref sh.Shots[0], blob, blobstart, blobcount);
            sh.Timestamps[0] = timestamp;
            sh.Flags[0] = 1;
            sh.LeadingIndex = 0;

            // return the innerid
            return sh.PoolId;
        }



        // Unpack Full
        public bool UnpackFullFirst(byte entityid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp)
        {
            // if ``, can't possibly belong to this snapper
            if (entityid >= FirstEntityIdToInnerId.Length)
                return false;

            // if ``, this snapper doesn't have this entity
            int innerid = FirstEntityIdToInnerId[entityid];
            if (innerid == -1)
                return false;

            UnpackFull(FirstData, innerid, blob, blobstart, blobcount, timestamp);
            return true;
        }

        public bool UnpackFullSecond(byte entityid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp)
        {

        }

        private void UnpackFull(ReArrayIdPool<SnapHistory<T>> data,
            int innerid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp)
        {
            SnapHistory<T> sh = data.Values[data.IdsToIndices[innerid]];

            todo; // this leadingindex logic is wrong
            // we need to figure out where the index is based on the 
            // timestamp at the leadingindex
            // e.g. if this timestamp is 100 and the leadingindex timestamp is
            //      98, then we need to insert at [leadingindex+2]
            // or if we get timestamp of 95, we insert at [leading-3]

            todo; // every tick, every object should duplicate latest snapshot
            // forward with rollover unless it has received a new snapshot
            // however when we rollover we need to handle extrapolation!
            

            UnpackFullAction(ref sh.Shots[sh.LeadingIndex], blob, blobstart, blobcount);
            sh.Timestamps[sh.LeadingIndex] = timestamp;
            todo; // handle flags

            // flags need to act a certain way to allow us to have
            // snapshots roll over when no changes are needed
            // e.g.:
            // 1 = from server (GOLD STANDARD)
            // 2 = roll over, this occurs when no snapshot is received
            //     we assume shot is unchanged and use it again for next
            //     timestamp
            // now imagine we have some snaps:
            // aaaaabb
            // 1222212
            //   ^ and here we receive a new snapshot c
            // we SCROLL FORWARD and replace all 2s with the new snapshot:
            // aacccbb
            // 1212212
            // this way we can let client continue to scroll forward but 
            // also correct itself properly
        }
    }
}
