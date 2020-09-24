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
        public delegate void ExtrapolateDelegate(ref T target, T basis, float elapsedms);

        public ClearDelegate ClearAction;
        public UnpackFullDelegate UnpackFullAction;
        public UnpackDeltaDelegate UnpackDeltaAction;
        public ExtrapolateDelegate ExtrapolateAction;


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

        public ushort CurrentTime = 0;
        public float TickMS = 0;

        public Snapper(ClearDelegate clearAction, UnpackFullDelegate unpackFullAction,
            UnpackDeltaDelegate unpackDeltaAction, ExtrapolateDelegate extrapAction)
        {
            ClearAction = clearAction;
            UnpackFullAction = unpackFullAction;
            UnpackDeltaAction = unpackDeltaAction;
            ExtrapolateAction = extrapAction;

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

            // if entity already exists, do unpackfull instead
            if (FirstEntityIdToInnerId[entityid] != -1)
            {
                return UnpackFull(FirstData, FirstEntityIdToInnerId[entityid],
                    blob, blobstart, blobcount, timestamp);
            }

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

            // if entity already exists, do unpackfull instead
            if (SecondEntityIdToInnerId[entityid] != -1)
            {
                return UnpackFull(SecondData, SecondEntityIdToInnerId[entityid],
                    blob, blobstart, blobcount, timestamp);
            }

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
            sh.Flags[0] = SnapHistory<T>.FlagGold;
            sh.LeadingIndex = 0;

            // when we ghost an entity, we need to advance it up until
            // the present timestamp
            // unless the given timestamp is *after* the current time,
            // in which case we need to move the leading index backwards
            int expectedIndex = CurrentTime - timestamp;
            if (expectedIndex > 0)
            {
                while (sh.Timestamps[sh.LeadingIndex] != CurrentTime)
                    AdvanceLogic(sh, TickMS);
            }
            else if (expectedIndex < 0)
            {
                int newindex = sh.Shots.Length + expectedIndex;
                if (newindex < 0)
                {
                    // now we have a problem. we received this ghost far too early
                    // basically we need to throw it out
                    sh.Flags[0] = SnapHistory<T>.FlagEmpty;
                    sh.Timestamps[0] = CurrentTime;
                }
                else
                {
                    sh.LeadingIndex = newindex;
                    sh.Timestamps[sh.LeadingIndex] = CurrentTime;
                }
            }

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

        public bool UnpackFullSecond(ushort entityid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp)
        {
            // if ``, can't possibly belong to this snapper
            if (entityid >= SecondEntityIdToInnerId.Length)
                return false;

            // if ``, this snapper doesn't have this entity
            int innerid = SecondEntityIdToInnerId[entityid];
            if (innerid == -1)
                return false;

            return UnpackFull(SecondData, innerid, blob, blobstart, blobcount, timestamp);
        }

        private bool UnpackFull(ReArrayIdPool<SnapHistory<T>> data,
            int innerid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp)
        {
            SnapHistory<T> sh = data.Values[data.IdsToIndices[innerid]];
            
            int index = sh.FindIndex(timestamp);
            if (index < 0 || index >= sh.Shots.Length)
                return false; // out of bounds

            UnpackFullAction(ref sh.Shots[index], blob, blobstart, blobcount);
            sh.Timestamps[index] = timestamp;
            sh.Flags[index] = SnapHistory<T>.FlagGold;

            // handle flag rollover
            Rollover(sh, index, timestamp);

            return true;
        }

        private void Rollover(SnapHistory<T> sh, int index, ushort timestamp)
        {
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
            int nindex = index + 1;
            ushort nextTime = timestamp;
            int rolls = 1;
            while (true)
            {
                if (nindex == sh.Shots.Length)
                    nindex = 0;

                if (nextTime == ushort.MaxValue)
                    nextTime = 0;
                else
                    nextTime++;

                // we can only rollover onto silver flags
                if (sh.Flags[nindex] != SnapHistory<T>.FlagSilver && sh.Flags[nindex] != SnapHistory<T>.FlagEmpty)
                    break;

                // can only rollover onto subsequent timestamps
                if (sh.Timestamps[nindex] != nextTime)
                    break;

                // now we're ready to roll
                ExtrapolateAction(ref sh.Shots[nindex], sh.Shots[index], TickMS * rolls);
                rolls++;

                nindex++;
            }
        }



        // Unpack Delta
        public bool UnpackDeltaFirst(byte entityid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp, ushort basisTimestamp)
        {
            // if ``, can't possibly belong to this snapper
            if (entityid >= FirstEntityIdToInnerId.Length)
                return false;

            // if ``, this snapper doesn't have this entity
            int innerid = FirstEntityIdToInnerId[entityid];
            if (innerid == -1)
                return false;

            return UnpackDelta(FirstData, innerid, blob, blobstart, blobcount, timestamp, basisTimestamp);
        }

        public bool UnpackDeltaSecond(ushort entityid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp, ushort basisTimestamp)
        {
            // if ``, can't possibly belong to this snapper
            if (entityid >= SecondEntityIdToInnerId.Length)
                return false;

            // if ``, this snapper doesn't have this entity
            int innerid = SecondEntityIdToInnerId[entityid];
            if (innerid == -1)
                return false;

            return UnpackDelta(SecondData, innerid, blob, blobstart, blobcount, timestamp, basisTimestamp);
        }

        private bool UnpackDelta(ReArrayIdPool<SnapHistory<T>> data,
            int innerid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp, ushort basisTimestamp)
        {
            SnapHistory<T> sh = data.Values[data.IdsToIndices[innerid]];

            int index = sh.FindIndex(timestamp);
            if (index < 0 || index >= sh.Shots.Length)
                return false; // out of bounds

            int basisIndex = sh.FindIndex(basisTimestamp);
            if (index < 0 || index >= sh.Shots.Length)
                return false; // out of bounds

            UnpackDeltaAction(ref sh.Shots[index], sh.Shots[basisIndex], blob, blobstart, blobcount);
            sh.Timestamps[index] = timestamp;
            sh.Flags[index] = SnapHistory<T>.FlagGold;

            // handle flag rollover
            Rollover(sh, index, timestamp);

            return true;
        }



        // Deghost
        public bool DeghostFirst(byte entityid, ushort timestamp)
        {
            // if ``, can't possibly belong to this snapper
            if (entityid >= FirstEntityIdToInnerId.Length)
                return false;

            // if ``, this snapper doesn't have this entity
            int innerid = FirstEntityIdToInnerId[entityid];
            if (innerid == -1)
                return false;

            return Deghost(FirstData, innerid, timestamp);
        }

        public bool DeghostSecond(ushort entityid, ushort timestamp)
        {
            // if ``, can't possibly belong to this snapper
            if (entityid >= SecondEntityIdToInnerId.Length)
                return false;

            // if ``, this snapper doesn't have this entity
            int innerid = SecondEntityIdToInnerId[entityid];
            if (innerid == -1)
                return false;

            return Deghost(SecondData, innerid, timestamp);
        }

        private bool Deghost(ReArrayIdPool<SnapHistory<T>> data,
            int innerid, ushort timestamp)
        {
            SnapHistory<T> sh = data.Values[data.IdsToIndices[innerid]];

            int index = sh.FindIndex(timestamp);
            if (index < 0 || index >= sh.Shots.Length)
                return false; // out of bounds

            sh.Flags[index] = SnapHistory<T>.FlagDeghosted;

            // scroll forward and deghost subsequent timestamps!

            // what to do if deghost and ghost arrive out of order?
            // simple: by only overwriting SILVER flags, we ensure that deghosting
            // will not overwrite fresh / more recent data

            int nindex = index + 1;
            ushort nextTime = timestamp;
            while (true)
            {
                if (nindex == sh.Shots.Length)
                    nindex = 0;

                if (nextTime == ushort.MaxValue)
                    nextTime = 0;
                else
                    nextTime++;

                // we can only rollover onto silver flags
                if (sh.Flags[nindex] != SnapHistory<T>.FlagSilver && sh.Flags[nindex] != SnapHistory<T>.FlagEmpty)
                    break;

                // can only rollover onto subsequent timestamps
                if (sh.Timestamps[nindex] != nextTime)
                    break;

                // now we're ready to roll
                sh.Flags[nindex] = SnapHistory<T>.FlagDeghosted;

                nindex++;
            }

            return true;
        }   



        // Destruct
        public bool DestructFirst(byte entityid)
        {
            // if ``, can't possibly belong to this snapper
            if (entityid >= FirstEntityIdToInnerId.Length)
                return false;

            // if ``, this snapper doesn't have this entity
            int innerid = FirstEntityIdToInnerId[entityid];
            if (innerid == -1)
                return false;

            FirstData.ReturnId(innerid);
            FirstEntityIdToInnerId[entityid] = -1;
            return true;
        }

        public bool DestructSecond(ushort entityid)
        {
            // if ``, can't possibly belong to this snapper
            if (entityid >= SecondEntityIdToInnerId.Length)
                return false;

            // if ``, this snapper doesn't have this entity
            int innerid = SecondEntityIdToInnerId[entityid];
            if (innerid == -1)
                return false;

            SecondData.ReturnId(innerid);
            SecondEntityIdToInnerId[entityid] = -1;
            return true;
        }



        // Prime Logic

        // Advancement occurs every tick
        public void Advance(ushort currentTime, float elapsedms)
        {
            CurrentTime = currentTime;
            TickMS = elapsedms;

            // when we advance, push every leading edge forward
            for (int i = 0; i < FirstData.Count; i++)
                AdvanceLogic(FirstData.Values[i], elapsedms);

            for (int i = 0; i < SecondData.Count; i++)
                AdvanceLogic(SecondData.Values[i], elapsedms);
        }

        private void AdvanceLogic(SnapHistory<T> sh, float elapsedms)
        {
            ushort last = sh.Timestamps[sh.LeadingIndex];
            ushort next = last == ushort.MaxValue ? (ushort)0 : (ushort)(last + 1);
            int startIndex = sh.LeadingIndex;

            sh.LeadingIndex++;
            if (sh.LeadingIndex == sh.Shots.Length)
                sh.LeadingIndex = 0;

            if (sh.Timestamps[sh.LeadingIndex] != next)
            {
                // we haven't received the next snap yet, so copy the previous one
                // need to extrapolate
                ExtrapolateAction(ref sh.Shots[sh.LeadingIndex], sh.Shots[startIndex], elapsedms);
                sh.Timestamps[sh.LeadingIndex] = next;
                sh.Flags[sh.LeadingIndex] = SnapHistory<T>.FlagSilver;
                // as a silver flag, it will be overwritten if we receive gold from the server

                // if the previous snapshot was deghosted,
                // deghost this one as well.
                if (sh.Flags[startIndex] == SnapHistory<T>.FlagDeghosted)
                    sh.Flags[sh.LeadingIndex] = SnapHistory<T>.FlagDeghosted;
            }
        }

        // Cleanup
        public void Removed()
        {
            // Called when this snapper is removed
            todo; 
        }

        public void ClearEntities()
        {
            // Called when all entities of this snapper should be removed
            todo; // ?
        }
    }
}
