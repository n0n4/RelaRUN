using System;
using System.Collections.Generic;
using System.Text;
using RelaNet.Messages;
using RelaNet.Utilities;

namespace RelaNet.Snapshots.Basic2d
{
    public struct PackerBasic2d : ISnapPacker<NentBasic2d, PackInfoBasic2d>
    {
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

        public void PackDelta(Sent sent, PackInfoBasic2d packinfo)
        {
            sent.WriteByte(packinfo.DeltaFlag);
            if ((packinfo.DeltaFlag & DELTA_FLAG_ID1) != 0)
                sent.WriteByte(packinfo.Active.Id1);

            if ((packinfo.DeltaFlag & DELTA_FLAG_ID2) != 0)
                sent.WriteUShort(packinfo.Active.Id2);

            if ((packinfo.DeltaFlag & DELTA_FLAG_X) != 0)
                sent.WriteFloat(packinfo.Active.X);

            if ((packinfo.DeltaFlag & DELTA_FLAG_Y) != 0)
                sent.WriteFloat(packinfo.Active.Y);

            if ((packinfo.DeltaFlag & DELTA_FLAG_ROT) != 0)
                sent.WriteFloat(packinfo.Active.Rot);

            if ((packinfo.DeltaFlag & DELTA_FLAG_XVEL) != 0)
                sent.WriteFloat(packinfo.Active.XVel);

            if ((packinfo.DeltaFlag & DELTA_FLAG_YVEL) != 0)
                sent.WriteFloat(packinfo.Active.YVel);

            if ((packinfo.DeltaFlag & DELTA_FLAG_FREE1) != 0)
                sent.WriteFloat(packinfo.Active.Free1);
        }

        public void PackFull(Sent sent, PackInfoBasic2d packinfo)
        {
            sent.WriteByte(packinfo.Active.Id1);
            sent.WriteUShort(packinfo.Active.Id2);
            sent.WriteFloat(packinfo.Active.X);
            sent.WriteFloat(packinfo.Active.Y);
            sent.WriteFloat(packinfo.Active.Rot);
            sent.WriteFloat(packinfo.Active.XVel);
            sent.WriteFloat(packinfo.Active.YVel);
            sent.WriteFloat(packinfo.Active.Free1);
        }

        public byte PrepPackDelta(NentBasic2d obj, NentBasic2d basis,
            out PackInfoBasic2d packinfo)
        {
            byte len = 1;
            packinfo = new PackInfoBasic2d();
            packinfo.Active = obj;
            packinfo.DeltaFlag = DELTA_FLAG_NONE;

            if (obj.Id1 != basis.Id1)
            {
                len++;
                packinfo.DeltaFlag |= DELTA_FLAG_ID1;
            }

            if (obj.Id2 != basis.Id2)
            {
                len += 2;
                packinfo.DeltaFlag |= DELTA_FLAG_ID2;
            }

            if (obj.X != basis.X)
            {
                len += 4;
                packinfo.DeltaFlag |= DELTA_FLAG_X;
            }

            if (obj.Y != basis.Y)
            {
                len += 4;
                packinfo.DeltaFlag |= DELTA_FLAG_Y;
            }

            if (obj.Rot != basis.Rot)
            {
                len += 4;
                packinfo.DeltaFlag |= DELTA_FLAG_ROT;
            }

            if (obj.XVel != basis.XVel)
            {
                len += 4;
                packinfo.DeltaFlag |= DELTA_FLAG_XVEL;
            }

            if (obj.YVel != basis.YVel)
            {
                len += 4;
                packinfo.DeltaFlag |= DELTA_FLAG_YVEL;
            }

            if (obj.Free1 != basis.Free1)
            {
                len += 4;
                packinfo.DeltaFlag |= DELTA_FLAG_FREE1;
            }

            return len;
        }

        public byte PrepPackFull(NentBasic2d obj,
            out PackInfoBasic2d packinfo)
        {
            packinfo = new PackInfoBasic2d();
            packinfo.Active = obj;
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
