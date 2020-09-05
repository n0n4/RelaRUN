using RelaNet.Messages;
using RelaStructures;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace RelaNet.Sockets
{
    public class VirtualSocket : ISocket
    {
        public ISocket[] Targets;
        public IPEndPoint[] Addresses;
        private ReArrayIdPool<Receipt> Pool;
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

        public VirtualSocket(ISocket[] targets, IPEndPoint[] addresses,
            int maxQueueSize, IPEndPoint fromPoint)
        {
            Targets = targets;
            Addresses = addresses;
            FromPoint = fromPoint;

            Pool = new ReArrayIdPool<Receipt>(10, maxQueueSize,
                PoolCreate, (obj) => { obj.Clear(); });
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

            Receipt receipt = Pool.Request();
            Buffer.BlockCopy(msg, 0, receipt.Data, 0, len);
            receipt.Length = len;
            receipt.EndPoint = from;
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

        public bool CanRead(int skips)
        {
            // must be at least two elements to read, since the last element is always the
            // empty buffer being written to
            return (Pool.Count - skips) > 1;
        }

        public Receipt Read(int skips)
        {
            // we read the second to last from the end
            // because the last is the empty one being written to
            // and returning the second to last item means that
            // the fewest possible indices will need to be moved
            // since every higher index must be moved down a spot once one is removed
            return Pool.Values[(Pool.Count - skips) - 2];
        }

        public void EndRead(int skips)
        {
            Pool.ReturnIndex((Pool.Count - skips) - 2);
        }


        // Cleanup
        public void Close()
        {
            // nothing doing
        }
    }
}
