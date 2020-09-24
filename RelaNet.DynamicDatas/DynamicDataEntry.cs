using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.DynamicDatas
{
    public class DynamicDataEntry
    {
        public DynamicDataType DataType;

        public bool[] Bools;
        public int PackedBoolSize = 0;
        public byte[] Bytes;
        public ushort[] UShorts;
        public int[] Ints;
        public float[] Floats;
        public double[] Doubles;
        private string[] Strings;
        private int[] StringLengths;
        private int TotalStringLengths;

        public static int EntryPacketSplitSize = 160;

        public DynamicDataEntry(DynamicDataType type)
        {
            DataType = type;

            if (type != null)
            {
                if (type.Bools > 0)
                {
                    Bools = new bool[type.Bools];
                    if (Bools.Length > 0)
                    {
                        PackedBoolSize = (8 + (Bools.Length - (Bools.Length % 8))) / 8;
                    }
                }
                if (type.Bytes > 0)
                    Bytes = new byte[type.Bytes];
                if (type.UShorts > 0)
                    UShorts = new ushort[type.UShorts];
                if (type.Ints > 0)
                    Ints = new int[type.Ints];
                if (type.Floats > 0)
                    Floats = new float[type.Floats];
                if (type.Doubles > 0)
                    Doubles = new double[type.Doubles];
                if (type.Strings > 0)
                {
                    Strings = new string[type.Strings];
                    StringLengths = new int[type.Strings];
                }
            }
        }

        // pack/unpack methods
        public int GetPacketCount()
        {
            int len = PackedBoolSize 
                + (Bytes != null ? Bytes.Length : 0)
                + (UShorts != null ? (UShorts.Length * 2) : 0)
                + (Ints != null ? (Ints.Length * 4) : 0)
                + (Floats != null ? (Floats.Length * 4) : 0)
                + (Doubles != null ? (Doubles.Length * 8) : 0)
                + TotalStringLengths;

            return (EntryPacketSplitSize + (len - (len % EntryPacketSplitSize))) / EntryPacketSplitSize;
        }

        private int WritingPacket = 0;
        private int WritingCurrentItem = 0;
        private int WritingStringDepth = 0;
        public void BeginWrite()
        {
            WritingPacket = 0;
            WritingCurrentItem = 0;
            WritingStringDepth = 0;
        }

        public int GetNextPacketLength()
        {
            int len = 0;
            int bonuslen = 3; // size of the header

            int itemMin = 0;
            int itemMax = DataType.Bools;

            int item = WritingCurrentItem;
            // bools
            while (item >= itemMin && item < itemMax)
            {
                len += 1;
                item += 8;
                if (item > itemMax)
                    item = itemMax;
                if (len >= EntryPacketSplitSize)
                    return len + bonuslen;
            }

            // bytes
            itemMin += DataType.Bools;
            itemMax += DataType.Bytes;
            while (item >= itemMin && item < itemMax)
            {
                len += 1;
                item += 1;

                if (len >= EntryPacketSplitSize)
                    return len + bonuslen;
            }

            // ushorts
            itemMin += DataType.Bytes;
            itemMax += DataType.UShorts;
            while (item >= itemMin && item < itemMax)
            {
                len += 2;
                item += 1;

                if (len >= EntryPacketSplitSize)
                    return len + bonuslen;
            }

            // ints
            itemMin += DataType.UShorts;
            itemMax += DataType.Ints;
            while (item >= itemMin && item < itemMax)
            {
                len += 4;
                item += 1;

                if (len >= EntryPacketSplitSize)
                    return len + bonuslen;
            }

            // floats
            itemMin += DataType.Ints;
            itemMax += DataType.Floats;
            while (item >= itemMin && item < itemMax)
            {
                len += 4;
                item += 1;

                if (len >= EntryPacketSplitSize)
                    return len + bonuslen;
            }

            // doubles
            itemMin += DataType.Floats;
            itemMax += DataType.Doubles;
            while (item >= itemMin && item < itemMax)
            {
                len += 8;
                item += 1;

                if (len >= EntryPacketSplitSize)
                    return len + bonuslen;
            }

            // strings
            itemMin += DataType.Doubles;
            itemMax += DataType.Strings;
            int sdepth = WritingStringDepth;
            int possibleLen = 0;
            while (item >= itemMin && item < itemMax)
            {
                possibleLen = 2 + StringLengths[item - itemMin] - sdepth;
                sdepth = 0; // if we're part way through a string,
                // apply that depth, but only to the first string we consider

                // need to handle splitting
                if (possibleLen + len > EntryPacketSplitSize)
                {
                    return EntryPacketSplitSize + bonuslen;
                }

                len += possibleLen;
                item += 1;

                if (len >= EntryPacketSplitSize)
                    return len + bonuslen;
            }

            return len + bonuslen;
        }

        public int WriteNextPacket(byte[] data, int start)
        {
            int maxc = start + EntryPacketSplitSize;

            int c = start;

            // write header
            // write starting item number
            Utilities.Bytes.WriteUShort(data, (ushort)WritingCurrentItem, start);
            c += 2;
            // leave a space for the number of items written
            int totalIndex = c;
            c++;

            int itemMin = 0;
            int itemMax = DataType.Bools;

            int origCurrentItem = WritingCurrentItem;

            // bools
            byte scratch = 0;
            while (WritingCurrentItem >= itemMin && WritingCurrentItem < itemMax)
            {
                scratch = 0;
                // write bools into this byte
                for (int i = 0; i < 8 && i + WritingCurrentItem < itemMax; i++)
                    if (Bools[WritingCurrentItem])
                        scratch = Utilities.Bits.AddTrueBit(scratch, i);

                data[c] = scratch;
                c++;
                WritingCurrentItem += 8;
                if (WritingCurrentItem > itemMax)
                    WritingCurrentItem = itemMax;
                if (c >= maxc)
                {
                    data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem);
                    return c - start;
                }
            }

            // bytes
            itemMin += DataType.Bools;
            itemMax += DataType.Bytes;
            while (WritingCurrentItem >= itemMin && WritingCurrentItem < itemMax)
            {
                data[c] = Bytes[WritingCurrentItem - itemMin];
                c++;
                WritingCurrentItem++;

                if (c >= maxc)
                {
                    data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem);
                    return c - start;
                }
            }

            // ushorts
            itemMin += DataType.Bytes;
            itemMax += DataType.UShorts;
            while (WritingCurrentItem >= itemMin && WritingCurrentItem < itemMax)
            {
                Utilities.Bytes.WriteUShort(data, UShorts[WritingCurrentItem - itemMin], c);
                c += 2;
                WritingCurrentItem++;

                if (c >= maxc)
                {
                    data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem);
                    return c - start;
                }
            }

            // ints
            itemMin += DataType.UShorts;
            itemMax += DataType.Ints;
            while (WritingCurrentItem >= itemMin && WritingCurrentItem < itemMax)
            {
                Utilities.Bytes.WriteInt(data, Ints[WritingCurrentItem - itemMin], c);
                c += 4;
                WritingCurrentItem++;

                if (c >= maxc)
                {
                    data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem);
                    return c - start;
                }
            }

            // floats
            itemMin += DataType.Ints;
            itemMax += DataType.Floats;
            while (WritingCurrentItem >= itemMin && WritingCurrentItem < itemMax)
            {
                Utilities.Bytes.WriteFloat(data, Floats[WritingCurrentItem - itemMin], c);
                c += 4;
                WritingCurrentItem++;

                if (c >= maxc)
                {
                    data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem);
                    return c - start;
                }
            }

            // doubles
            itemMin += DataType.Floats;
            itemMax += DataType.Doubles;
            while (WritingCurrentItem >= itemMin && WritingCurrentItem < itemMax)
            {
                Utilities.Bytes.WriteDouble(data, Doubles[WritingCurrentItem - itemMin], c);
                c += 8;
                WritingCurrentItem++;

                if (c >= maxc)
                {
                    data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem);
                    return c - start;
                }
            }

            // strings
            itemMin += DataType.Doubles;
            itemMax += DataType.Strings;
            int possibleLen = 0;
            string str;
            while (WritingCurrentItem >= itemMin && WritingCurrentItem < itemMax)
            {
                // write the header
                // ushort: string depth
                Utilities.Bytes.WriteUShort(data, (ushort)WritingStringDepth, c);
                c += 2;

                str = Strings[WritingCurrentItem - itemMin];
                possibleLen = 2 + StringLengths[WritingCurrentItem - itemMin] - WritingStringDepth;

                // need to handle splitting
                if (possibleLen + c > maxc)
                {
                    int writelen = maxc - c;
                    // write a substring
                    c += Utilities.Bytes.WriteString(data, str.Substring(WritingStringDepth, writelen), c);

                    WritingStringDepth += writelen;
                    data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem);
                    return c - start;
                }

                if (WritingStringDepth == 0)
                    c += Utilities.Bytes.WriteString(data, str, c);
                else
                    c += Utilities.Bytes.WriteString(data, str.Substring(WritingStringDepth), c);
                
                WritingCurrentItem++;
                WritingStringDepth = 0;

                if (c >= maxc)
                {
                    data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem);
                    return c - start;
                }
            }
            
            data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem);
            return c - start;
        }

        // unpack
        public int ReadPacket(byte[] data, int start)
        {
            // read header
            // first is ushort: starting item
            // second is byte: how many items in packet
            int c = start;
            int itemIndex = Utilities.Bytes.ReadUShort(data, c);
            c += 2;

            byte itemCount = data[c];
            byte startItemCount = itemCount;
            c++;

            // read the body of the message
            int itemMin = 0;
            int itemMax = DataType.Bools;

            // bools
            byte scratch = 0;
            while (itemIndex >= itemMin && itemIndex < itemMax)
            {
                scratch = data[c];
                c++;
                // read out of the byte
                for (int i = 0; i < 8 && i + itemIndex < itemMax; i++)
                    Bools[itemIndex + i] = Utilities.Bits.CheckBit(scratch, i);
                
                itemIndex += 8;
                if (itemIndex > itemMax)
                    itemIndex = itemMax;
                if (itemIndex >= startItemCount)
                    return c - start;
            }

            // bytes
            itemMin += DataType.Bools;
            itemMax += DataType.Bytes;
            while (itemIndex >= itemMin && itemIndex < itemMax)
            {
                Bytes[itemIndex - itemMin] = data[c];
                c++;
                itemIndex++;

                if (itemIndex >= startItemCount)
                    return c - start;
            }

            // ushorts
            itemMin += DataType.Bytes;
            itemMax += DataType.UShorts;
            while (itemIndex >= itemMin && itemIndex < itemMax)
            {
                UShorts[itemIndex - itemMin] = Utilities.Bytes.ReadUShort(data, c);
                c += 2;
                itemIndex++;

                if (itemIndex >= startItemCount)
                    return c - start;
            }

            // ints
            itemMin += DataType.UShorts;
            itemMax += DataType.Ints;
            while (itemIndex >= itemMin && itemIndex < itemMax)
            {
                Ints[itemIndex - itemMin] = Utilities.Bytes.ReadInt(data, c);
                c += 4;
                itemIndex++;

                if (itemIndex >= startItemCount)
                    return c - start;
            }

            // floats
            itemMin += DataType.Ints;
            itemMax += DataType.Floats;
            while (itemIndex >= itemMin && itemIndex < itemMax)
            {
                Floats[itemIndex - itemMin] = Utilities.Bytes.ReadFloat(data, c);
                c += 4;
                itemIndex++;

                if (itemIndex >= startItemCount)
                    return c - start;
            }

            // doubles
            itemMin += DataType.Floats;
            itemMax += DataType.Doubles;
            while (itemIndex >= itemMin && itemIndex < itemMax)
            {
                Doubles[itemIndex - itemMin] = Utilities.Bytes.ReadDouble(data, c);
                c += 8;
                itemIndex++;

                if (itemIndex >= startItemCount)
                    return c - start;
            }

            // strings
            itemMin += DataType.Doubles;
            itemMax += DataType.Strings;
            int stringDepth = 0;
            string str;
            while (itemIndex >= itemMin && itemIndex < itemMax)
            {
                // write the header
                // ushort: string depth
                stringDepth = Utilities.Bytes.ReadUShort(data, c);
                c += 2;

                // Do we need to recalculate string length here?
                // technically we should, but the client should never
                // need to use that info, so there's no point in doing
                // so here.

                str = Utilities.Bytes.ReadString(data, c);
                if (stringDepth == 0)
                {
                    Strings[itemIndex - itemMin] = str;
                }
                else
                {
                    // NOTE: so we're not using the full strength of the string depth
                    // here, because we just blindly assume that the packets will
                    // be received in order, so just construct the string one bit
                    // after another.
                    // this system will only work if these packets are sent in an
                    // ordered fashion.
                    // if unordered support was desired, this would need to be
                    // changed.
                    Strings[itemIndex - itemMin] = Strings[itemIndex - itemMin] + str;
                }
                
                itemIndex++;

                if (itemIndex >= startItemCount)
                    return c - start;
            }

            return c - start;
        }

        // accessor methods

        // bools
        public int GetBoolIndex(string name)
        {
            for (int i = 0; i < DataType.Bools; i++)
                if (DataType.Names[i] == name)
                    return i;
            return -1;
        }

        public bool GetBool(string name)
        {
            int index = GetBoolIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to get bool '" + name + "' but it does not exist for type '" + DataType.TypeName + "'");

            return Bools[index];
        }

        public void SetBool(string name, bool value)
        {
            int index = GetBoolIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to set bool '" + name + "' but it does not exist for type '" + DataType.TypeName + "'");

            Bools[index] = value;
        }

        // bytes
        public int GetByteIndex(string name)
        {
            int offset = DataType.Bools;
            int max = offset + DataType.Bytes;
            for (int i = offset; i < max; i++)
                if (DataType.Names[i] == name)
                    return i - offset;
            return -1;
        }

        public byte GetByte(string name)
        {
            int index = GetByteIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to get byte '" + name + "' but it does not exist for type '" + DataType.TypeName + "'");

            return Bytes[index];
        }

        public void SetByte(string name, byte value)
        {
            int index = GetByteIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to set byte '" + name + "' but it does not exist for type '" + DataType.TypeName + "'");

            Bytes[index] = value;
        }

        // ushorts
        public int GetUShortIndex(string name)
        {
            int offset = DataType.Bools + DataType.Bytes;
            int max = offset + DataType.UShorts;
            for (int i = offset; i < max; i++)
                if (DataType.Names[i] == name)
                    return i - offset;
            return -1;
        }

        public ushort GetUShort(string name)
        {
            int index = GetUShortIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to get ushort '" + name + "' but it does not exist for type '" + DataType.TypeName + "'");

            return UShorts[index];
        }

        public void SetUShort(string name, ushort value)
        {
            int index = GetUShortIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to set ushort '" + name + "' but it does not exist for type '" + DataType.TypeName + "'");

            UShorts[index] = value;
        }

        // ints
        public int GetIntIndex(string name)
        {
            int offset = DataType.Bools + DataType.Bytes + DataType.UShorts;
            int max = offset + DataType.Ints;
            for (int i = offset; i < max; i++)
                if (DataType.Names[i] == name)
                    return i - offset;
            return -1;
        }

        public int GetInt(string name)
        {
            int index = GetIntIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to get int '" + name + "' but it does not exist for type '" + DataType.TypeName + "'");

            return Ints[index];
        }

        public void SetInt(string name, int value)
        {
            int index = GetIntIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to set int '" + name + "' but it does not exist for type '" + DataType.TypeName + "'");

            Ints[index] = value;
        }

        // floats
        public int GetFloatIndex(string name)
        {
            int offset = DataType.Bools + DataType.Bytes + DataType.UShorts + DataType.Ints;
            int max = offset + DataType.Floats;
            for (int i = offset; i < max; i++)
                if (DataType.Names[i] == name)
                    return i - offset;
            return -1;
        }

        public float GetFloat(string name)
        {
            int index = GetFloatIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to get float '" + name + "' but it does not exist for type '" + DataType.TypeName + "'");

            return Floats[index];
        }

        public void SetFloat(string name, float value)
        {
            int index = GetFloatIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to set float '" + name + "' but it does not exist for type '" + DataType.TypeName + "'");

            Floats[index] = value;
        }

        // doubles
        public int GetDoubleIndex(string name)
        {
            int offset = DataType.Bools + DataType.Bytes + DataType.UShorts + DataType.Ints + DataType.Floats;
            int max = offset + DataType.Doubles;
            for (int i = offset; i < max; i++)
                if (DataType.Names[i] == name)
                    return i - offset;
            return -1;
        }

        public double GetDouble(string name)
        {
            int index = GetDoubleIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to get double '" + name + "' but it does not exist for type '" + DataType.TypeName + "'");

            return Doubles[index];
        }

        public void SetDouble(string name, double value)
        {
            int index = GetDoubleIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to set double '" + name + "' but it does not exist for type '" + DataType.TypeName + "'");

            Doubles[index] = value;
        }

        // strings
        public int GetStringIndex(string name)
        {
            int offset = DataType.Bools + DataType.Bytes + DataType.UShorts + DataType.Ints + DataType.Floats
                + DataType.Doubles;
            int max = offset + DataType.Strings;
            for (int i = offset; i < max; i++)
                if (DataType.Names[i] == name)
                    return i - offset;
            return -1;
        }

        public string GetString(string name)
        {
            int index = GetStringIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to get string '" + name + "' but it does not exist for type '" + DataType.TypeName + "'");

            return Strings[index];
        }

        public string GetStringAtIndex(int index)
        {
            return Strings[index];
        }

        public void SetString(string name, string value)
        {
            int index = GetStringIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to set string '" + name + "' but it does not exist for type '" + DataType.TypeName + "'");

            Strings[index] = value;
            StringLengths[index] = Utilities.Bytes.GetStringLength(value);

            // recalculate total string lengths
            TotalStringLengths = 0;
            for (int i = 0; i < Strings.Length; i++)
                TotalStringLengths += StringLengths[i];
        }
    }
}
