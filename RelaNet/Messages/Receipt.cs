using RelaNet.Sockets;
using RelaStructures;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace RelaNet.Messages
{
    public class Receipt : IPoolable
    {
        public byte[] Data;
        public int Length;
        public IPEndPoint EndPoint;
        public int PoolId { get; private set; }
        private ReArrayIdPool<Receipt> Pool;

        // Header information
        public byte PlayerId;
        public byte TargetPlayerId;
        public ushort MessageId;
        public bool IsImmediate;

        public bool Processed;
        public bool CanBeReleased;

        public Receipt(ReArrayIdPool<Receipt> pool)
        {
            Data = new byte[UdpClientExtensions.MaxUdpSize];
            Length = 0;
            EndPoint = (IPEndPoint)UdpClientExtensions.anyV4Endpoint;
            PoolId = -1;
            Pool = pool;

            PlayerId = 0;
            TargetPlayerId = 0;
            MessageId = 0;
            IsImmediate = false;

            Processed = false;
            CanBeReleased = false;
        }

        public void Clear()
        {
            Length = 0;
            PlayerId = 0;
            TargetPlayerId = 0;
            MessageId = 0;
            IsImmediate = false;
            Processed = false;
            CanBeReleased = false;
        }

        public int GetPoolIndex()
        {
            return PoolId;
        }

        public void SetPoolIndex(int index)
        {
            PoolId = index;
        }

        public void Return()
        {
            Pool.ReturnId(PoolId);
        }
    }
}
