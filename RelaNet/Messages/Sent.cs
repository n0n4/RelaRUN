using RelaNet.Sockets;
using RelaNet.Utilities;
using RelaStructures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace RelaNet.Messages
{
    public class Sent : IPoolable
    {
        public byte[] Data;
        public int Length;

        public int Retries;
        public byte[] TargetPids;
        public int AwaitingLength; // how many targets have we added
        public float[] TargetWaits; // how long have we waited

        public ushort MessageId;
        public bool HasMessageId;
        public int PoolIndex;
        public bool IsImmediate;
        public bool IsOrdered;

        public byte[][] Acks;
        public byte[] AckPids;
        public int AckCount;

        public ushort[] FastOrderValues; // per player, order index at time sent was made
        public BitArray FastOrderSets; // per player, whether fast order value has been set
        public bool IsFastOrdered;

        public bool Finalized;

        public Action<byte, ushort> HandshakeCallback;

        public const int AckPosition = 5; // the index of the handshake within a sent's buffer
        public const int MidPosition = 2; // the index of the message id within a sent's buffer
        public const int PidPosition = 0; // ''               player id  ''
        public const int TargetPidPosition = 1; // ''               player id  ''
        public const int SpecialPosition = 4; // ''           special id ''

        public const int EmptySizeWithAck = AckPosition + 12;
        public const int OrderValuePosition = AckPosition + 12;
        public const int EmptySizeWithOrderValue = OrderValuePosition + 2;


        public Sent()
        {
            Data = new byte[UdpClientExtensions.MaxUdpSize];
            Length = 0;

            Retries = 0;
            TargetPids = new byte[8];
            AwaitingLength = 0;
            TargetWaits = new float[8];

            MessageId = 0;
            HasMessageId = false;
            PoolIndex = -1;
            IsImmediate = false;
            IsOrdered = false;

            Acks = new byte[8][];
            for (int i = 0; i < Acks.Length; i++)
                Acks[i] = new byte[12];
            AckPids = new byte[8];
            AckCount = 0;

            FastOrderValues = new ushort[8];
            FastOrderSets = new BitArray(256);
            IsFastOrdered = false;

            HandshakeCallback = null;

            Finalized = false;
        }

        public void AddTarget(byte pid)
        {
            if (Finalized)
                throw new Exception("Tried to add new player to finalized sent.");

            if (TargetPids.Length <= AwaitingLength)
            {
                // expand the targetPids array
                byte[] ntp = new byte[TargetPids.Length * 2];
                for (int i = 0; i < TargetPids.Length; i++)
                    ntp[i] = TargetPids[i];
                TargetPids = ntp;
            }
            if (TargetWaits.Length <= AwaitingLength)
            {
                // expand targetWaits array
                float[] ntw = new float[TargetWaits.Length * 2];
                for (int i = 0; i < TargetWaits.Length; i++)
                    ntw[i] = TargetWaits[i];
                TargetWaits = ntw;
            }
            TargetPids[AwaitingLength] = pid;
            TargetWaits[AwaitingLength] = 0;
            AwaitingLength++;
        }

        public void RemoveTargetIndex(int index, bool skiphandshake)
        {
            if(AwaitingLength <= 1)
            {
                AwaitingLength = 0;
                return;
            }

            if (!skiphandshake && HandshakeCallback != null)
                HandshakeCallback(TargetPids[index], MessageId);

            AwaitingLength--;
            TargetPids[index] = TargetPids[AwaitingLength];

            
        }

        // returns index of the ack within Acks
        // so then you would do sent.Acks[returnedIndex][0] = ...
        public int AddAck(byte pid)
        {
            int index = AckCount;
            if(Acks.Length <= index)
            {
                // we need to expand the arrays
                byte[][] nacks = new byte[Acks.Length * 2][];
                byte[] nackpids = new byte[Acks.Length * 2];
                for (int i = 0; i < Acks.Length; i++)
                {
                    nacks[i] = Acks[i];
                    nackpids[i] = AckPids[i];
                }
                for (int i = Acks.Length; i < nacks.Length; i++)
                    nacks[i] = new byte[12];

                Acks = nacks;
                AckPids = nackpids;
            }

            AckPids[index] = pid;
            AckCount++;
            return index;
        }

        public void LoadAck(byte pid)
        {
            // load the given ack out of our Acks list and put it
            // into Data at the appropriate offset
            byte[] nack = Acks[pid];
            Buffer.BlockCopy(nack, 0, Data, AckPosition, 12);
        }

        public void AddFastOrderValue(byte pid, ushort value)
        {
            while (FastOrderValues.Length <= pid)
            {
                // must expand
                ushort[] nfov = new ushort[FastOrderValues.Length * 2];
                for (int i = 0; i < FastOrderValues.Length; i++)
                    nfov[i] = FastOrderValues[i];
                FastOrderValues = nfov;
            }
            FastOrderValues[pid] = value;
            FastOrderSets[pid] = true;
        }

        public void LoadFastOrderValue(byte pid)
        {
            Bytes.WriteUShort(Data, FastOrderValues[pid], OrderValuePosition);
        }

        public void Clear()
        {
            Length = 0;
            HasMessageId = false;
            AwaitingLength = 0;
            Retries = 0;
            AckCount = 0;
            IsImmediate = false;
            IsOrdered = false;
            Finalized = false;
            if (IsFastOrdered)
                FastOrderSets.SetAll(false);
            IsFastOrdered = false;
            HandshakeCallback = null;
        }

        public void SetPoolIndex(int index)
        {
            PoolIndex = index;
        }

        public int GetPoolIndex()
        {
            return PoolIndex;
        }


        #region Writing Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte b)
        {
            Data[Length] = b; Length++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUShort(ushort u)
        {
            Bytes.WriteUShort(Data, u, Length); Length += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt(int i)
        {
            Bytes.WriteInt(Data, i, Length); Length += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(string s)
        {
            Length += Bytes.WriteString(Data, s, Length);
        }
        #endregion
    }
}
