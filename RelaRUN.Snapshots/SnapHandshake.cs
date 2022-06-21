using RelaStructures;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.Snapshots
{
    public class SnapHandshake : IPoolable
    {
        public int PoolIndex = -1;
        public ushort Timestamp = 0;

        public ushort AckNo = 0;

        public byte[] FirstEntities = new byte[8];
        public int FirstEntityCount = 0;
        public ushort[] SecondEntities = new ushort[8];
        public int SecondEntityCount = 0;

        public byte[] FirstResends = new byte[4];
        public ushort[] FirstResendsTimestamp = new ushort[4];
        public int FirstResendsCount = 0;

        public ushort[] SecondResends = new ushort[4];
        public ushort[] SecondResendsTimestamp = new ushort[4];
        public int SecondResendsCount = 0;

        public int GetPoolIndex()
        {
            return PoolIndex;
        }

        public void SetPoolIndex(int index)
        {
            PoolIndex = index;
        }

        public void Clear()
        {
            AckNo = 0;
            FirstEntityCount = 0;
            SecondEntityCount = 0;
            FirstResendsCount = 0;
            SecondResendsCount = 0;
        }

        public void AddFirstEntity(byte eid)
        {
            if (FirstEntities.Length <= FirstEntityCount)
            {
                // resize
                byte[] nfe = new byte[FirstEntityCount * 2];
                for (int i = 0; i < FirstEntityCount; i++)
                    nfe[i] = FirstEntities[i];
                FirstEntities = nfe;
            }

            FirstEntities[FirstEntityCount] = eid;
            FirstEntityCount++;
        }

        public void AddSecondEntity(ushort eid)
        {
            if (SecondEntities.Length <= SecondEntityCount)
            {
                // resize
                ushort[] nfe = new ushort[SecondEntityCount * 2];
                for (int i = 0; i < SecondEntityCount; i++)
                    nfe[i] = SecondEntities[i];
                SecondEntities = nfe;
            }

            SecondEntities[SecondEntityCount] = eid;
            SecondEntityCount++;
        }

        public void AddFirstResend(byte eid, ushort timestamp)
        {
            if (FirstResends.Length <= FirstResendsCount)
            {
                // resize
                byte[] nr = new byte[FirstResendsCount * 2];
                ushort[] nt = new ushort[FirstResendsCount * 2];
                for (int i = 0; i < FirstResendsCount; i++)
                {
                    nr[i] = FirstResends[i];
                    nt[i] = FirstResendsTimestamp[i];
                }
                FirstResends = nr;
                FirstResendsTimestamp = nt;
            }

            FirstResends[FirstResendsCount] = eid;
            FirstResendsTimestamp[FirstResendsCount] = timestamp;
            FirstResendsCount++;
        }

        public void AddSecondResend(ushort eid, ushort timestamp)
        {
            if (SecondResends.Length <= SecondResendsCount)
            {
                // resize
                ushort[] nr = new ushort[SecondResendsCount * 2];
                ushort[] nt = new ushort[SecondResendsCount * 2];
                for (int i = 0; i < SecondResendsCount; i++)
                {
                    nr[i] = SecondResends[i];
                    nt[i] = SecondResendsTimestamp[i];
                }
                SecondResends = nr;
                SecondResendsTimestamp = nt;
            }

            SecondResends[SecondResendsCount] = eid;
            SecondResendsTimestamp[SecondResendsCount] = timestamp;
            SecondResendsCount++;
        }
    }
}
