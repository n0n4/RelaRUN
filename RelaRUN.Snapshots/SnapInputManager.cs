﻿using RelaRUN.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.Snapshots
{
    public class SnapInputManager<T, U> : ISnapInputManager 
        where T : struct 
        where U : struct, ISnapInputPacker<T>
    {
        // should return c + number of bytes read
        // put result in 'into'
        //public delegate int ReadDelegate(ref T into, Receipt receipt, int c);

        private int WriteFixedLength = -1;
        //private Func<T, int> WriteGetLengthAction;
        //private Action<T, Sent> WriteAction;
        //private ReadDelegate ReadAction;

        private readonly U Packer;

        private NetExecutorSnapper Snapper;
        private byte InputIndex;

        // store the inputs in an ordered list
            // during expected operation, this will run very smoothly
            // (will just be appending to the end 95% of the time)
            // and this makes processing dead simple
            // (loop until you hit one outside timestamp range)

        // in order to speed this, also we should use a circle buffer
            // rather than moving inputs down to 0 after removing spent inputs
            
        public T[] ClientInputs;
        public ushort[] ClientInputTimestamps;
        public float[] ClientInputTickMS;
        public int ClientInputStart;
        public int ClientInputCount;

        // Finalized: client inputs are written first without a timestamp
        // then when they get finalized, a timestamp is assigned.
        // this process is required because the client inputs may be written
        // before the netsnapper has PreTicked for this frame, and they would
        // therefore have an invalid (old) timestamp if we took the current one
        // at that point in time.
        public bool[] ClientInputRequiresFinalize;


        public T[][] ServerInputs;
        public ushort[][] ServerInputTimestamps;
        public float[][] ServerInputTickMS;
        public int[] ServerInputStart;
        public int[] ServerInputCount;

        // write: write T to the byte buffer in Sent
        // read: read from the byte buffer in Receipt to T
        // writeLen: if non null, calculate how many bytes writing T will take
        // fixedLength: if not -1, will be used instead of writeLen
        //              (more efficient if your command is always same length)
        public SnapInputManager(int fixedLength = -1)
        {
            Packer = new U();

            // note: if fixedlength is not -1, it will be used instead of writeLen
            WriteFixedLength = fixedLength;
        }

        public void Loaded(NetExecutorSnapper snapper, byte inputIndex)
        {
            Snapper = snapper;
            InputIndex = inputIndex;

            if (snapper.Server.IsHost)
            {
                ServerInputs = new T[2][];
                ServerInputTimestamps = new ushort[2][];
                ServerInputTickMS = new float[2][];
                ServerInputStart = new int[2];
                ServerInputCount = new int[2];

                for (int i = 0; i < ServerInputs.Length; i++)
                {
                    ServerInputs[i] = new T[32];
                    ServerInputTimestamps[i] = new ushort[32];
                    ServerInputTickMS[i] = new float[32];
                }
            }
            else
            {
                ClientInputs = new T[32];
                ClientInputTimestamps = new ushort[32];
                ClientInputTickMS = new float[32];
                ClientInputRequiresFinalize = new bool[32];
            }
        }

        // server input
        public int ReadInput(Receipt receipt, int c, ushort timestamp, float tickms)
        {
            // find a new T to read into
            byte pid = receipt.PlayerId;
            if (ServerInputCount[pid] == ServerInputs[pid].Length)
            {
                // must resize
                ServerInputs[pid] = ResizeStructs(ServerInputs[pid], ServerInputs[pid].Length * 2,
                    ServerInputStart[pid], ServerInputCount[pid]);
                ServerInputTimestamps[pid] = ResizeStructs(ServerInputTimestamps[pid], ServerInputTimestamps[pid].Length * 2,
                    ServerInputStart[pid], ServerInputCount[pid]);
                ServerInputTickMS[pid] = ResizeStructs(ServerInputTickMS[pid], ServerInputTickMS[pid].Length * 2,
                    ServerInputStart[pid], ServerInputCount[pid]);
                ServerInputStart[pid] = 0;
            }

            // now the hard part.
            // we need to add this new input IN ORDER of timestamp/tickms
            // so we need to scroll through the inputs and see which it belongs after

            // start checking from the end, because in proper functioning network
            // we'll be appending to the end most often
            int placeIndex = ServerInputStart[pid] + ServerInputCount[pid];
            if (placeIndex >= ServerInputs[pid].Length)
                placeIndex -= ServerInputs[pid].Length;

            int checkIndex = placeIndex - 1;
            if (checkIndex < 0)
                checkIndex += ServerInputs[pid].Length;

            bool found = false;
            for (int i = 0; i < ServerInputCount[pid]; i++)
            {
                // if we're after the timestamp + tickms in check, place here
                if (ServerInputTimestamps[pid][checkIndex] == timestamp)
                {
                    // simple case: same timestamp
                    if (ServerInputTickMS[pid][checkIndex] <= tickms)
                    {
                        // we're good
                        found = true;
                        break;
                    }
                }
                else
                {
                    // if it's a different timestamp, we need to figure out if it's 
                    // before or after the one we're looking to place
                    if (timestamp >= ushort.MaxValue / 2)
                    {
                        ushort teststamp = timestamp;
                        teststamp -= (ushort.MaxValue / 2);
                        if (ServerInputTimestamps[pid][checkIndex] >= teststamp
                            && ServerInputTimestamps[pid][checkIndex] <= timestamp)
                        {
                            found = true;
                            break;
                        }
                    }
                    else
                    {
                        ushort teststamp = ushort.MaxValue;
                        teststamp -= (ushort)((ushort.MaxValue / 2) - timestamp);
                        if (ServerInputTimestamps[pid][checkIndex] >= teststamp
                            || ServerInputTimestamps[pid][checkIndex] < timestamp)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                // decrement both
                placeIndex = checkIndex;
                checkIndex--;
                if (checkIndex < 0)
                    checkIndex += ServerInputs[pid].Length;
            }


            if (!found)
            {
                // if we don't know where to put it, then it must go before the
                // first entry
                // unless we have no entries, in which case it goes into the first
                // slot
                if (ServerInputCount[pid] == 0)
                {
                    placeIndex = ServerInputStart[pid];
                }
                else
                {
                    ServerInputStart[pid]--;
                    placeIndex = ServerInputStart[pid];
                    // need to move the start back
                    if (placeIndex < 0)
                    {
                        placeIndex += ServerInputs.Length;
                        ServerInputStart[pid] += ServerInputs.Length;
                    }
                }
            }

            // now we're guaranteed to have a placeindex we can put this in.
            c = Packer.Read(ref ServerInputs[pid][placeIndex], receipt, c);
            ServerInputTimestamps[pid][placeIndex] = timestamp;
            ServerInputTickMS[pid][placeIndex] = tickms;
            ServerInputCount[pid]++;

            return c;
        }

        // client input
        public void WriteInput(T t)
        {
            if (ClientInputCount == ClientInputs.Length)
            {
                // must resize
                ClientInputs = ResizeStructs(ClientInputs, ClientInputs.Length * 2, 
                    ClientInputStart, ClientInputCount);
                ClientInputTimestamps = ResizeStructs(ClientInputTimestamps, ClientInputTimestamps.Length * 2,
                    ClientInputStart, ClientInputCount);
                ClientInputTickMS = ResizeStructs(ClientInputTickMS, ClientInputTickMS.Length * 2,
                    ClientInputStart, ClientInputCount);
                ClientInputRequiresFinalize = ResizeStructs(ClientInputRequiresFinalize, ClientInputRequiresFinalize.Length * 2,
                    ClientInputStart, ClientInputCount);
                ClientInputStart = 0;
            }

            int index = ClientInputStart + ClientInputCount;
            if (index >= ClientInputs.Length)
                index -= ClientInputs.Length;
            ClientInputs[index] = t;
            
            // mark it as RequiresFinalize
            // we will finish writing this input once NetExecutorSnapper gets to PreTick
            ClientInputRequiresFinalize[index] = true;

            ClientInputCount++;
        }

        // should only be called by the NetExecutorSnapper
        public void FinalizeInputs()
        {
            // loop over our client inputs backwards (end-first)
            // when we hit one that is finalized, break, since all the further ones
            // must be finalized already (remember: we always add new inputs to the end)

            // sanity: if we don't have any inputs, don't bother finalizing...
            if (ClientInputCount <= 0)
                return;

            int index = ClientInputStart + ClientInputCount - 1;
            if (index >= ClientInputs.Length)
                index -= ClientInputs.Length;
            for (int i = 0; i < ClientInputs.Length; i++)
            {
                if (ClientInputRequiresFinalize[index] == false)
                    break; // done

                // if we got here: it must need to be finalized
                // substitute in the proper times and write to the server

                // note: although we send these inputs with ClientInputTime to the server,
                // on our end we store them earlier (at ClientTime)
                // this is because the client is rendering themselves slightly in the future
                // compared to the server.
                ClientInputTimestamps[index] = Snapper.ClientTime;
                ClientInputTickMS[index] = Snapper.ClientTickMS + Snapper.ClientInputTickMSInputOffset;
                ClientInputRequiresFinalize[index] = false;

                // network it
                int slen = WriteFixedLength;
                if (slen == -1)
                    slen = Packer.GetWriteLength(ClientInputs[index]);
                Sent sent = Snapper.GetClientInputSent(InputIndex, slen);
                Packer.Write(ClientInputs[index], sent);

                // adjust index
                index--;
                if (index < 0)
                    index += ClientInputs.Length;
            }
        }

        public void PlayerAdded(byte pid)
        {
            if (Snapper.Server.IsHost)
            {
                // expand server arrays if needed
                if (pid >= ServerInputs.Length)
                {
                    int nlen = ServerInputs.Length * 2;
                    while (nlen <= pid) nlen *= 2;
                    T[][] ni = new T[nlen][];
                    ushort[][] nt = new ushort[nlen][];
                    float[][] nms = new float[nlen][];
                    int[] nstart = new int[nlen];
                    int[] ncount = new int[nlen];

                    for (int i =0; i < ServerInputs.Length; i++)
                    {
                        ni[i] = ServerInputs[i];
                        nt[i] = ServerInputTimestamps[i];
                        nms[i] = ServerInputTickMS[i];
                        nstart[i] = ServerInputStart[i];
                        ncount[i] = ServerInputCount[i];
                    }

                    for (int i = ServerInputs.Length; i < nlen; i++)
                    {
                        ni[i] = new T[32];
                        nt[i] = new ushort[32];
                        nms[i] = new float[32];
                        nstart[i] = 0;
                        ncount[i] = 0;
                    }

                    ServerInputs = ni;
                    ServerInputTimestamps = nt;
                    ServerInputTickMS = nms;
                    ServerInputStart = nstart;
                    ServerInputCount = ncount;
                }
            }
        }


        // Release
        public void ClientReleaseInputs(ushort timestamp)
        {
            int index = ClientInputStart;
            int max = ClientInputCount;
            for (int i = 0; i < max; i++, index++)
            {
                if (index >= ClientInputTimestamps.Length)
                    index -= ClientInputTimestamps.Length;

                ushort ts = ClientInputTimestamps[index];
                // only preserve inputs which are within 500 of the current time
                // therefore, only break when we hit one of these inputs
                if (timestamp > ushort.MaxValue - 500
                    && (ts > timestamp
                    || ts < 500))
                    break; // in the future region

                if (timestamp < ushort.MaxValue - 500
                    && ts > timestamp && ts < timestamp + 500)
                    break; // in the future region

                // if the timestamp matches what we're releasing,
                // move forward in our circle buffer
                ClientInputStart++;
                if (ClientInputStart >= ClientInputTimestamps.Length)
                    ClientInputStart -= ClientInputTimestamps.Length;

                ClientInputCount--;
            }
        }

        public void ServerReleaseInputs(ushort timestamp)
        {
            // check for each player
            for (int p = 0; p < ServerInputs.Length; p++)
            {
                int max = ServerInputCount[p];
                int index = ServerInputStart[p];
                for (int i = 0; i < max; i++, index++)
                {
                    if (index >= ServerInputTimestamps[p].Length)
                        index -= ServerInputTimestamps[p].Length;

                    ushort ts = ServerInputTimestamps[p][index];
                    // only preserve inputs which are within 500 of the current time
                    // therefore, only break when we hit one of these inputs
                    if (timestamp > ushort.MaxValue - 500
                        && (ts > timestamp
                        || ts < 500))
                        break; // in the future region

                    if (timestamp < ushort.MaxValue - 500
                        && ts > timestamp && ts < timestamp + 500)
                        break; // in the future region

                    // if the timestamp matches what we're releasing,
                    // move forward in our circle buffer
                    ServerInputStart[p]++;
                    if (ServerInputStart[p] >= ServerInputTimestamps[p].Length)
                        ServerInputStart[p] -= ServerInputTimestamps[p].Length;
                    ServerInputCount[p]--;
                }
            }
        }



        // Accessors
        private T[] EmptyT = new T[0];
        private ushort[] EmptyTimestamps = new ushort[0];
        private float[] EmptyTickMS = new float[0];
        public T[] GetPlayerInputs(byte pid, out int start, out int count,
            out ushort[] timestamps, out float[] tickms)
        {
            if (!Snapper.Server.IsHost)
            {
                // if we're the client, there's a special case where, if our pid
                // is the one requested, we need to return ClientInput rather than
                // ServerInput
                if (pid == Snapper.Server.OurPlayerId)
                {
                    start = ClientInputStart;
                    count = ClientInputCount;
                    timestamps = ClientInputTimestamps;
                    tickms = ClientInputTickMS;
                    return ClientInputs;
                }

                // if it's not our pid, we return empty since the client
                // will never have input information about other players
                start = 0;
                count = 0;
                timestamps = EmptyTimestamps;
                tickms = EmptyTickMS;
                return EmptyT;
            }

            // if we are the host, we simply return from serverinputs
            start = ServerInputStart[pid];
            count = ServerInputCount[pid];
            timestamps = ServerInputTimestamps[pid];
            tickms = ServerInputTickMS[pid];
            return ServerInputs[pid];
        }



        // Helpers
        private V[] ResizeStructs<V>(V[] a, int nlen, int oldstart, int oldcount)
        {
            V[] b = new V[nlen];

            // reorder
            for (int i = 0; i < oldcount; i++)
            {
                int o = oldstart + i;
                if (o >= a.Length)
                    o -= a.Length;
                b[i] = a[o];
            }

            // the new start is equal to 0
            return b;
        }
    }
}
