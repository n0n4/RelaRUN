﻿using RelaRUN.Messages;
using RelaStructures;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace RelaRUN.Sockets
{
    public class VirtualSocket : ISocket
    {
        public ISocket[] Targets;
        public IPEndPoint[] Addresses;
        private ReArrayIdPool<Receipt> Pool;
        private Receipt WritingReceipt;
        public IPEndPoint FromPoint; // endpoint that client sees our messages
                                     // as arriving from (ip / port)

        public Random Random = new Random();
        public double SimulatedDropChance = 0;
        public double SimulatedLatencyMin = 0;
        public double SimulatedLatencyMax = 0;
        private class LatentSend
        {
            public double RemainingTime = 0;
            public byte[] Buffer = new byte[UdpClientExtensions.MaxUdpSize];
            public int Length = 0;
            public IPEndPoint Target;
        }
        private LatentSend[] LatentSends = new LatentSend[1];
        private int LatentSendCount = 0;

        private int ReadingIndex = 0;

        public VirtualSocket(ISocket[] targets, IPEndPoint[] addresses,
            int maxQueueSize, IPEndPoint fromPoint)
        {
            Targets = targets;
            Addresses = addresses;
            FromPoint = fromPoint;

            Pool = new ReArrayIdPool<Receipt>(10, maxQueueSize,
                PoolCreate, (obj) => { obj.Clear(); });
            WritingReceipt = Pool.Request();
        }

        public void Send(byte[] msg, int len, IPEndPoint target)
        {
            if (SimulatedLatencyMin > 0 || SimulatedLatencyMax > 0)
            {
                while (LatentSends.Length <= LatentSendCount)
                {
                    // must expand
                    LatentSend[] nls = new LatentSend[LatentSends.Length * 2];
                    for (int i = 0; i < LatentSends.Length; i++)
                        nls[i] = LatentSends[i];
                    LatentSends = nls;
                }
                if (LatentSends[LatentSendCount] == null)
                    LatentSends[LatentSendCount] = new LatentSend();
                LatentSend ls = LatentSends[LatentSendCount];
                LatentSendCount++;
                Buffer.BlockCopy(msg, 0, ls.Buffer, 0, len);
                ls.Length = len;
                ls.RemainingTime = SimulatedLatencyMin + Random.NextDouble() * (SimulatedLatencyMax - SimulatedLatencyMin);
                ls.Target = target;
                return;
            }

            for (int i = 0; i < Targets.Length; i++)
            {
                if (Addresses[i].Address.Equals(target.Address) && Addresses[i].Port == target.Port)
                {
                    Targets[i].Receive(msg, len, FromPoint);
                    break;
                }
            }
        }

        public void Receive(byte[] msg, int len, IPEndPoint from)
        {
            if (SimulatedDropChance > 0 && Random.NextDouble() < SimulatedDropChance)
                return; // just ignore the packet
            
            Buffer.BlockCopy(msg, 0, WritingReceipt.Data, 0, len);
            WritingReceipt.Length = len;
            WritingReceipt.EndPoint = from;

            if (len != 0)
                WritingReceipt = Pool.Request();
        }

        public void Tick(float elapsedms)
        {
            for (int i = 0; i < LatentSendCount; i++)
            {
                LatentSend ls = LatentSends[i];
                ls.RemainingTime -= elapsedms;
                if (ls.RemainingTime <= 0)
                {
                    for (int o = 0; o < Targets.Length; o++)
                    {
                        if (Addresses[o].Address.Equals(ls.Target.Address) && Addresses[o].Port == ls.Target.Port)
                        {
                            Targets[o].Receive(ls.Buffer, ls.Length, FromPoint);
                            break;
                        }
                    }

                    // now remove
                    LatentSendCount--;
                    LatentSends[i] = LatentSends[LatentSendCount];
                    LatentSends[LatentSendCount] = ls;
                }
            }
        }


        // Read from the message pool
        private Receipt PoolCreate()
        {
            return new Receipt(Pool);
        }

        public void StartRead()
        {
            ReadingIndex = 0;
            // caveat: if the first receipt is the receiving one, skip it
            if (Pool.Count == 0)
                return;
            if (Pool.Values[ReadingIndex].PoolId == WritingReceipt.PoolId)
                ReadingIndex++;
        }

        public bool CanRead()
        {
            return ReadingIndex < Pool.Count;
        }

        public Receipt Read()
        {
            int index = ReadingIndex;
            // increment the index
            ReadingIndex++;
            // skip next if it is the receiving
            if (Pool.Values[ReadingIndex].PoolId == WritingReceipt.PoolId)
                ReadingIndex++;
            return Pool.Values[index];
        }

        public void EndRead()
        {
            for (int i = Pool.Count - 1; i >= 0; i--)
            {
                if (Pool.Values[i].CanBeReleased)
                    Pool.ReturnIndex(i);
            }
        }


        // Cleanup
        public void Close()
        {
            // nothing doing
        }
    }
}
