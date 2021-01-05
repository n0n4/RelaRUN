using System;
using System.Collections.Generic;
using System.Text;
using RelaNet.Messages;
using RelaNet.Utilities;

namespace RelaNet.Snapshots.Basic2d
{
    public struct SnapPackerBasic2d : ISnapPacker<NentBasic2d>
    {
        public NentBasic2d ActiveObj;
        public NentBasic2d BasisObj;
        public byte DeltaFlag;

        public const byte DELTA_FLAG_NONE = 0;
        public const byte DELTA_FLAG_X = 1;
        public const byte DELTA_FLAG_Y = 2;
        public const byte DELTA_FLAG_ROT = 4;
        public const byte DELTA_FLAG_XVEL = 8;
        public const byte DELTA_FLAG_YVEL = 16;
        public const byte DELTA_FLAG_ID1 = 32;
        public const byte DELTA_FLAG_ID2 = 64;
        public const byte DELTA_FLAG_FREE1 = 128;

        public void Clear(ref NentBasic2d obj)
        {
            obj.X = 0;
            obj.Y = 0;
            obj.Rot = 0;
            obj.XVel = 0;
            obj.YVel = 0;
            obj.Id1 = 0;
            obj.Id2 = 0;
            obj.Free1 = 0;
        }

        public void PackDelta(Sent sent)
        {
            sent.WriteByte(DeltaFlag);
            if ((DeltaFlag & DELTA_FLAG_ID1) != 0)
                sent.WriteByte(ActiveObj.Id1);

            if ((DeltaFlag & DELTA_FLAG_ID2) != 0)
                sent.WriteUShort(ActiveObj.Id2);

            if ((DeltaFlag & DELTA_FLAG_X) != 0)
                sent.WriteFloat(ActiveObj.X);

            if ((DeltaFlag & DELTA_FLAG_Y) != 0)
                sent.WriteFloat(ActiveObj.Y);

            if ((DeltaFlag & DELTA_FLAG_ROT) != 0)
                sent.WriteFloat(ActiveObj.Rot);

            if ((DeltaFlag & DELTA_FLAG_XVEL) != 0)
                sent.WriteFloat(ActiveObj.XVel);

            if ((DeltaFlag & DELTA_FLAG_YVEL) != 0)
                sent.WriteFloat(ActiveObj.YVel);

            if ((DeltaFlag & DELTA_FLAG_FREE1) != 0)
                sent.WriteFloat(ActiveObj.Free1);
        }

        public void PackFull(Sent sent)
        {
            sent.WriteByte(ActiveObj.Id1);
            sent.WriteUShort(ActiveObj.Id2);
            sent.WriteFloat(ActiveObj.X);
            sent.WriteFloat(ActiveObj.Y);
            sent.WriteFloat(ActiveObj.Rot);
            sent.WriteFloat(ActiveObj.XVel);
            sent.WriteFloat(ActiveObj.YVel);
            sent.WriteFloat(ActiveObj.Free1);
        }

        public byte PrepPackDelta(NentBasic2d obj, NentBasic2d basis)
        {
            byte len = 1;
            ActiveObj = obj;
            BasisObj = basis;
            DeltaFlag = DELTA_FLAG_NONE;

            if (obj.Id1 != basis.Id1)
            {
                len++;
                DeltaFlag |= DELTA_FLAG_ID1;
            }

            if (obj.Id2 != basis.Id2)
            {
                len += 2;
                DeltaFlag |= DELTA_FLAG_ID2;
            }

            if (obj.X != basis.X)
            {
                len += 4;
                DeltaFlag |= DELTA_FLAG_X;
            }

            if (obj.Y != basis.Y)
            {
                len += 4;
                DeltaFlag |= DELTA_FLAG_Y;
            }

            if (obj.Rot != basis.Rot)
            {
                len += 4;
                DeltaFlag |= DELTA_FLAG_ROT;
            }

            if (obj.XVel != basis.XVel)
            {
                len += 4;
                DeltaFlag |= DELTA_FLAG_XVEL;
            }

            if (obj.YVel != basis.YVel)
            {
                len += 4;
                DeltaFlag |= DELTA_FLAG_YVEL;
            }

            if (obj.Free1 != basis.Free1)
            {
                len += 4;
                DeltaFlag |= DELTA_FLAG_FREE1;
            }

            return len;
        }

        public byte PrepPackFull(NentBasic2d obj)
        {
            ActiveObj = obj;
            return 1 + 2 + (4 * 6);
        }

        public void UnpackDelta(ref NentBasic2d obj, NentBasic2d basis, byte[] blob, int start, int count)
        {
            byte dflag = blob[start]; start++;

            if ((dflag & DELTA_FLAG_ID1) != 0)
            {
                obj.Id1 = blob[start]; start++;
            }
            else
            {
                obj.Id1 = basis.Id1;
            }

            if ((dflag & DELTA_FLAG_ID2) != 0)
            {
                obj.Id2 = Bytes.ReadUShort(blob, start); start += 2;
            }
            else
            {
                obj.Id2 = basis.Id2;
            }

            if ((dflag & DELTA_FLAG_X) != 0)
            {
                obj.X = Bytes.ReadFloat(blob, start); start += 4;
            }
            else
            {
                obj.X = basis.X;
            }

            if ((dflag & DELTA_FLAG_Y) != 0)
            {
                obj.Y = Bytes.ReadFloat(blob, start); start += 4;
            }
            else
            {
                obj.Y = basis.Y;
            }

            if ((dflag & DELTA_FLAG_ROT) != 0)
            {
                obj.Rot = Bytes.ReadFloat(blob, start); start += 4;
            }
            else
            {
                obj.Rot = basis.Rot;
            }

            if ((dflag & DELTA_FLAG_XVEL) != 0)
            {
                obj.XVel = Bytes.ReadFloat(blob, start); start += 4;
            }
            else
            {
                obj.XVel = basis.XVel;
            }

            if ((dflag & DELTA_FLAG_YVEL) != 0)
            {
                obj.YVel = Bytes.ReadFloat(blob, start); start += 4;
            }
            else
            {
                obj.YVel = basis.YVel;
            }

            if ((dflag & DELTA_FLAG_FREE1) != 0)
            {
                obj.Free1 = Bytes.ReadFloat(blob, start); start += 4;
            }
            else
            {
                obj.Free1 = basis.Free1;
            }
        }

        public void UnpackFull(ref NentBasic2d obj, byte[] blob, int start, int count)
        {
            obj.Id1 = blob[start]; start++;
            obj.Id2 = Bytes.ReadUShort(blob, start); start += 2;
            obj.X = Bytes.ReadFloat(blob, start); start += 4;
            obj.Y = Bytes.ReadFloat(blob, start); start += 4;
            obj.Rot = Bytes.ReadFloat(blob, start); start += 4;
            obj.XVel = Bytes.ReadFloat(blob, start); start += 4;
            obj.YVel = Bytes.ReadFloat(blob, start); start += 4;
            obj.Free1 = Bytes.ReadFloat(blob, start); start += 4;
        }
    }
}
