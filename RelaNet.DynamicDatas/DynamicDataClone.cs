using RelaNet.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.DynamicDatas
{
    public class DynamicDataClone
    {
        // has the properties of the DynamicDataEntry and only stores
        // its own modifications thereupon

        public DynamicDataEntry Entry;

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

        private bool[] Stales;

        public static int EntryPacketSplitSize = 160;

        public DynamicDataClone(DynamicDataEntry entry)
        {
            Entry = entry;

            if (entry != null)
            {
                Stales = new bool[entry.DataType.TotalCount];

                if (entry.DataType.Bools > 0)
                {
                    Bools = new bool[entry.DataType.Bools];
                    if (Bools.Length > 0)
                    {
                        PackedBoolSize = (8 + (Bools.Length - (Bools.Length % 8))) / 8;
                        for (int i = 0; i < entry.DataType.Bools; i++)
                            Bools[i] = entry.Bools[i];
                    }
                }
                if (entry.DataType.Bytes > 0)
                {
                    Bytes = new byte[entry.DataType.Bytes];
                    for (int i = 0; i < entry.DataType.Bytes; i++)
                        Bytes[i] = entry.Bytes[i];
                }
                if (entry.DataType.UShorts > 0)
                {
                    UShorts = new ushort[entry.DataType.UShorts];
                    for (int i = 0; i < entry.DataType.UShorts; i++)
                        UShorts[i] = entry.UShorts[i];
                }
                if (entry.DataType.Ints > 0)
                {
                    Ints = new int[entry.DataType.Ints];
                    for (int i = 0; i < entry.DataType.Ints; i++)
                            Ints[i] = entry.Ints[i];
                }
                if (entry.DataType.Floats > 0)
                {
                    Floats = new float[entry.DataType.Floats];
                    for (int i = 0; i < entry.DataType.Floats; i++)
                            Floats[i] = entry.Floats[i];
                }
                if (entry.DataType.Doubles > 0)
                {
                    Doubles = new double[entry.DataType.Doubles];
                    for (int i = 0; i < entry.DataType.Doubles; i++)
                            Doubles[i] = entry.Doubles[i];
                }
                if (entry.DataType.Strings > 0)
                {
                    Strings = new string[entry.DataType.Strings];
                    for (int i = 0; i < entry.DataType.Strings; i++)
                        Strings[i] = entry.GetStringAtIndex(i);
                    StringLengths = new int[entry.DataType.Strings];
                }
            }
        }

        // reuse a clone
        // we will try to reuse our arrays if possible
        public void LoadFromEntry(DynamicDataEntry entry)
        {
            Entry = entry;

            Stales = new bool[entry.DataType.TotalCount];

            if (entry.DataType.Bools > 0)
            {
                if (Bools == null || Bools.Length < entry.DataType.Bools)
                    Bools = new bool[entry.DataType.Bools];
                if (Bools.Length > 0)
                {
                    PackedBoolSize = (8 + (Bools.Length - (Bools.Length % 8))) / 8;
                    for (int i = 0; i < entry.DataType.Bools; i++)
                        Bools[i] = entry.Bools[i];
                }
            }
            else
            {
                PackedBoolSize = 0;
            }

            if (entry.DataType.Bytes > 0)
            {
                if (Bytes == null || Bytes.Length < entry.DataType.Bytes)
                    Bytes = new byte[entry.DataType.Bytes];
                for (int i = 0; i < entry.DataType.Bytes; i++)
                    Bytes[i] = entry.Bytes[i];
            }

            if (entry.DataType.UShorts > 0)
            {
                if (UShorts == null || UShorts.Length < entry.DataType.UShorts)
                    UShorts = new ushort[entry.DataType.UShorts];
                for (int i = 0; i < entry.DataType.UShorts; i++)
                    UShorts[i] = entry.UShorts[i];
            }

            if (entry.DataType.Ints > 0)
            {
                if (Ints == null || Ints.Length < entry.DataType.Ints)
                    Ints = new int[entry.DataType.Ints];
                for (int i = 0; i < entry.DataType.Ints; i++)
                    Ints[i] = entry.Ints[i];
            }

            if (entry.DataType.Floats > 0)
            {
                if (Floats == null || Floats.Length < entry.DataType.Floats)
                    Floats = new float[entry.DataType.Floats];
                for (int i = 0; i < entry.DataType.Floats; i++)
                    Floats[i] = entry.Floats[i];
            }

            if (entry.DataType.Doubles > 0)
            {
                if (Doubles == null || Doubles.Length < entry.DataType.Doubles)
                    Doubles = new double[entry.DataType.Doubles];
                for (int i = 0; i < entry.DataType.Doubles; i++)
                    Doubles[i] = entry.Doubles[i];
            }

            if (entry.DataType.Strings > 0)
            {
                if (Strings == null || Strings.Length < entry.DataType.Strings)
                    Strings = new string[entry.DataType.Strings];
                for (int i = 0; i < entry.DataType.Strings; i++)
                    Strings[i] = entry.GetStringAtIndex(i);
                if (StringLengths == null || StringLengths.Length < entry.DataType.Strings)
                    StringLengths = new int[entry.DataType.Strings];
            }
        }

        // stale update
        #region stale update
        // stales are fields that have been changed recently
        // to only send fields which are stale, use the full update methods with delta = true
        // (if delta is false, all fields different from base are networked)
        // once you have sent a stale update, call ClearStales 
        private void TrySetStale(ushort index)
        {
            Stales[index] = true;
            /*for (int i = 0; i < StaleCount; i++)
            {
                if (Stales[i] == index)
                    return;
            }
            Stales[StaleCount] = index;
            StaleCount++;*/
        }

        public void ClearStales()
        {
            for (int i = 0; i < Stales.Length; i++)
                Stales[i] = false;
        }
        #endregion update

        // send full update
        #region full update
        // network every field that is not default
        private int WritingCurrentItem = 0;
        private int WritingStringDepth = 0;

        public bool IsDoneWriting()
        {
            return WritingCurrentItem == Entry.DataType.TotalCount;
        }

        public void BeginWrite()
        {
            WritingCurrentItem = 0;
            WritingStringDepth = 0;
        }

        public int GetNextPacketLength(bool delta)
        {
            int len = 3;

            int itemMin = 0;
            int itemMax = Entry.DataType.Bools;

            int item = WritingCurrentItem;
            // bools
            // bools are always networked
            while (item >= itemMin && item < itemMax)
            {
                len += 1;
                item += 8;
                if (item > itemMax)
                    item = itemMax;
                if (len >= EntryPacketSplitSize)
                    return len;
            }

            // bytes
            itemMin += Entry.DataType.Bools;
            itemMax += Entry.DataType.Bytes;
            if (Entry.DataType.Bytes > 0)
                len++;
            while (item >= itemMin && item < itemMax)
            {
                len += ((!delta && Bytes[item - itemMin] == Entry.Bytes[item - itemMin])
                    || (delta && !Stales[item]) ? 0 : 2);
                item += 1;

                if (len >= EntryPacketSplitSize)
                    return len;
            }

            // ushorts
            itemMin += Entry.DataType.Bytes;
            itemMax += Entry.DataType.UShorts;
            if (Entry.DataType.UShorts > 0)
                len++;
            while (item >= itemMin && item < itemMax)
            {
                len += ((!delta && UShorts[item - itemMin] == Entry.UShorts[item - itemMin])
                    || (delta && !Stales[item]) ? 0 : 3);
                item += 1;

                if (len >= EntryPacketSplitSize)
                    return len;
            }

            // ints
            itemMin += Entry.DataType.UShorts;
            itemMax += Entry.DataType.Ints;
            if (Entry.DataType.Ints > 0)
                len++;
            while (item >= itemMin && item < itemMax)
            {
                len += ((!delta && Ints[item - itemMin] == Entry.Ints[item - itemMin])
                    || (delta && !Stales[item]) ? 0 : 5);
                item += 1;

                if (len >= EntryPacketSplitSize)
                    return len;
            }

            // floats
            itemMin += Entry.DataType.Ints;
            itemMax += Entry.DataType.Floats;
            if (Entry.DataType.Floats > 0)
                len++;
            while (item >= itemMin && item < itemMax)
            {
                len += ((!delta && Floats[item - itemMin] == Entry.Floats[item - itemMin])
                    || (delta && !Stales[item]) ? 0 : 5);
                item += 1;

                if (len >= EntryPacketSplitSize)
                    return len;
            }

            // doubles
            itemMin += Entry.DataType.Floats;
            itemMax += Entry.DataType.Doubles;
            if (Entry.DataType.Doubles > 0)
                len++;
            while (item >= itemMin && item < itemMax)
            {
                len += ((!delta && Doubles[item - itemMin] == Entry.Doubles[item - itemMin])
                    || (delta && !Stales[item]) ? 0 : 9);
                item += 1;

                if (len >= EntryPacketSplitSize)
                    return len;
            }

            // strings
            itemMin += Entry.DataType.Doubles;
            itemMax += Entry.DataType.Strings;
            int sdepth = WritingStringDepth;
            int possibleLen = 0;
            if (Entry.DataType.Strings > 0)
                len++;
            while (item >= itemMin && item < itemMax)
            {
                if ((!delta && Strings[item - itemMin] == Entry.GetStringAtIndex(item - itemMin))
                    || (delta && !Stales[item]))
                {
                    item += 1;
                    continue;
                }

                possibleLen = 3 + StringLengths[item - itemMin] - sdepth;
                sdepth = 0; // if we're part way through a string,
                // apply that depth, but only to the first string we consider

                // need to handle splitting
                if (possibleLen + len > EntryPacketSplitSize)
                {
                    return EntryPacketSplitSize;
                }

                len += possibleLen;
                item += 1;

                if (len >= EntryPacketSplitSize)
                    return len;
            }

            return len;
        }

        public void WriteNextPacket(Sent sent, bool delta)
        {
            sent.Length += WriteNextPacket(sent.Data, sent.Length, delta);
        }

        public int WriteNextPacket(byte[] data, int start, bool delta)
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
            int itemMax = Entry.DataType.Bools;

            int origCurrentItem = WritingCurrentItem;
            byte totalWritten = 0;

            // bools
            byte scratch = 0;
            while (WritingCurrentItem >= itemMin && WritingCurrentItem < itemMax)
            {
                scratch = 0;
                // write bools into this byte
                for (int i = 0; i < 8 && i + WritingCurrentItem < itemMax; i++)
                {
                    if (Bools[WritingCurrentItem])
                    {
                        scratch = Utilities.Bits.AddTrueBit(scratch, i);
                        totalWritten++;
                    }
                }

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
            itemMin += Entry.DataType.Bools;
            itemMax += Entry.DataType.Bytes;
            byte sectionWritten = 0;
            int sectionWrittenIndex = 0;
            if (Entry.DataType.Bytes > 0)
            {
                sectionWrittenIndex = c;
                c++;
            }
            while (WritingCurrentItem >= itemMin && WritingCurrentItem < itemMax)
            {
                if ((!delta && Bytes[WritingCurrentItem - itemMin] == Entry.Bytes[WritingCurrentItem - itemMin])
                    || (delta && !Stales[WritingCurrentItem]))
                {
                    WritingCurrentItem++;
                    continue;
                }
                data[c] = (byte)(WritingCurrentItem - itemMin);
                c++;
                data[c] = Bytes[WritingCurrentItem - itemMin];
                c++;
                WritingCurrentItem++;
                sectionWritten++;
                totalWritten++;

                if (c >= maxc)
                {
                    data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem);
                    data[sectionWrittenIndex] = sectionWritten;
                    return c - start;
                }
            }
            if (Entry.DataType.Bytes > 0)
                data[sectionWrittenIndex] = sectionWritten;

            // ushorts
            itemMin += Entry.DataType.Bytes;
            itemMax += Entry.DataType.UShorts;
            sectionWritten = 0;
            sectionWrittenIndex = 0;
            if (Entry.DataType.UShorts > 0)
            {
                sectionWrittenIndex = c;
                c++;
            }
            while (WritingCurrentItem >= itemMin && WritingCurrentItem < itemMax)
            {
                if ((!delta && UShorts[WritingCurrentItem - itemMin] == Entry.UShorts[WritingCurrentItem - itemMin])
                    || (delta && !Stales[WritingCurrentItem]))
                {
                    WritingCurrentItem++;
                    continue;
                }
                data[c] = (byte)(WritingCurrentItem - itemMin);
                c++;
                Utilities.Bytes.WriteUShort(data, UShorts[WritingCurrentItem - itemMin], c);
                c += 2;
                WritingCurrentItem++;
                sectionWritten++;
                totalWritten++;

                if (c >= maxc)
                {
                    data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem);
                    data[sectionWrittenIndex] = sectionWritten;
                    return c - start;
                }
            }
            if (Entry.DataType.UShorts > 0)
                data[sectionWrittenIndex] = sectionWritten;

            // ints
            itemMin += Entry.DataType.UShorts;
            itemMax += Entry.DataType.Ints;
            sectionWritten = 0;
            sectionWrittenIndex = 0;
            if (Entry.DataType.Ints > 0)
            {
                sectionWrittenIndex = c;
                c++;
            }
            while (WritingCurrentItem >= itemMin && WritingCurrentItem < itemMax)
            {
                if ((!delta && Ints[WritingCurrentItem - itemMin] == Entry.Ints[WritingCurrentItem - itemMin])
                    || (delta && !Stales[WritingCurrentItem]))
                {
                    WritingCurrentItem++;
                    continue;
                }
                data[c] = (byte)(WritingCurrentItem - itemMin);
                c++;
                Utilities.Bytes.WriteInt(data, Ints[WritingCurrentItem - itemMin], c);
                c += 4;
                WritingCurrentItem++;
                sectionWritten++;
                totalWritten++;

                if (c >= maxc)
                {
                    data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem);
                    data[sectionWrittenIndex] = sectionWritten;
                    return c - start;
                }
            }
            if (Entry.DataType.Ints > 0)
                data[sectionWrittenIndex] = sectionWritten;

            // floats
            itemMin += Entry.DataType.Ints;
            itemMax += Entry.DataType.Floats;
            sectionWritten = 0;
            sectionWrittenIndex = 0;
            if (Entry.DataType.Floats > 0)
            {
                sectionWrittenIndex = c;
                c++;
            }
            while (WritingCurrentItem >= itemMin && WritingCurrentItem < itemMax)
            {
                if ((!delta && Floats[WritingCurrentItem - itemMin] == Entry.Floats[WritingCurrentItem - itemMin])
                    || (delta && !Stales[WritingCurrentItem]))
                {
                    WritingCurrentItem++;
                    continue;
                }
                data[c] = (byte)(WritingCurrentItem - itemMin);
                c++;
                Utilities.Bytes.WriteFloat(data, Floats[WritingCurrentItem - itemMin], c);
                c += 4;
                WritingCurrentItem++;
                sectionWritten++;
                totalWritten++;

                if (c >= maxc)
                {
                    data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem);
                    data[sectionWrittenIndex] = sectionWritten;
                    return c - start;
                }
            }
            if (Entry.DataType.Floats > 0)
                data[sectionWrittenIndex] = sectionWritten;

            // doubles
            itemMin += Entry.DataType.Floats;
            itemMax += Entry.DataType.Doubles;
            sectionWritten = 0;
            sectionWrittenIndex = 0;
            if (Entry.DataType.Doubles > 0)
            {
                sectionWrittenIndex = c;
                c++;
            }
            while (WritingCurrentItem >= itemMin && WritingCurrentItem < itemMax)
            {
                if ((!delta && Doubles[WritingCurrentItem - itemMin] == Entry.Doubles[WritingCurrentItem - itemMin])
                    || (delta && !Stales[WritingCurrentItem]))
                {
                    WritingCurrentItem++;
                    continue;
                }
                data[c] = (byte)(WritingCurrentItem - itemMin);
                c++;
                Utilities.Bytes.WriteDouble(data, Doubles[WritingCurrentItem - itemMin], c);
                c += 8;
                WritingCurrentItem++;
                sectionWritten++;
                totalWritten++;

                if (c >= maxc)
                {
                    data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem);
                    data[sectionWrittenIndex] = sectionWritten;
                    return c - start;
                }
            }
            if (Entry.DataType.Doubles > 0)
                data[sectionWrittenIndex] = sectionWritten;

            // strings
            itemMin += Entry.DataType.Doubles;
            itemMax += Entry.DataType.Strings;
            sectionWritten = 0;
            sectionWrittenIndex = 0;
            if (Entry.DataType.Strings > 0)
            {
                sectionWrittenIndex = c;
                c++;
            }
            int possibleLen = 0;
            string str;
            while (WritingCurrentItem >= itemMin && WritingCurrentItem < itemMax)
            {
                if ((!delta && Strings[WritingCurrentItem - itemMin] == Entry.GetStringAtIndex(WritingCurrentItem - itemMin))
                    || (delta && !Stales[WritingCurrentItem]))
                {
                    WritingCurrentItem++;
                    continue;
                }
                // write the header
                data[c] = (byte)(WritingCurrentItem - itemMin);
                c++;
                // ushort: string depth
                Utilities.Bytes.WriteUShort(data, (ushort)WritingStringDepth, c);
                c += 2;

                str = Strings[WritingCurrentItem - itemMin];
                possibleLen = StringLengths[WritingCurrentItem - itemMin] - WritingStringDepth;
                sectionWritten++;
                totalWritten++;

                // need to handle splitting
                if (possibleLen + c > maxc)
                {
                    // now the question is, where does this -1 come from?
                    // well, when we write a string, that process adds a \0 to the end
                    // so if we don't take 1 off the end now, we'll end up going 1 over
                    // what we promised to write in the getnextpacketlength
                    int writelen = maxc - c - 1;
                    // write a substring
                    c += Utilities.Bytes.WriteString(data, str.Substring(WritingStringDepth, writelen), c);

                    WritingStringDepth += writelen;
                    // this +1 is necessary because otherwise the read will end on the 
                    // item before this one.
                    // we don't simply add 1 to writingcurrentitem though, because
                    // we need to continue writing that item (did not write all of it yet)
                    data[totalIndex] = (byte)(WritingCurrentItem - origCurrentItem + 1);
                    data[sectionWrittenIndex] = sectionWritten;
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
                    data[sectionWrittenIndex] = sectionWritten;
                    return c - start;
                }
            }
            if (Entry.DataType.Strings > 0)
                data[sectionWrittenIndex] = sectionWritten;

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
            c++;

            // read the body of the message
            int itemMin = 0;
            int itemMax = Entry.DataType.Bools;

            // bools
            byte scratch = 0;
            while (itemIndex >= itemMin && itemIndex < itemMax)
            {
                scratch = data[c];
                c++;
                // read out of the byte
                for (int i = 0; i < 8 && i + itemIndex < itemMax; i++)
                {
                    Bools[itemIndex + i] = Utilities.Bits.CheckBit(scratch, i);
                    itemCount--;
                }

                itemIndex += 8;
                if (itemIndex > itemMax)
                    itemIndex = itemMax;
                if (itemCount == 0)
                    return c - start;
            }

            byte nextItems;
            byte nextItemId;

            // bytes
            itemMin += Entry.DataType.Bools;
            itemMax += Entry.DataType.Bytes;
            if (Entry.DataType.Bytes > 0)
            {
                nextItems = data[c];
                c++;
                while (nextItems > 0)
                {
                    nextItemId = data[c];
                    c++;
                    Bytes[nextItemId] = data[c];
                    c++;
                    nextItems--;
                    itemCount--;
                }
                if (itemCount == 0)
                    return c - start;
            }

            // ushorts
            itemMin += Entry.DataType.Bytes;
            itemMax += Entry.DataType.UShorts;
            if (Entry.DataType.UShorts > 0)
            {
                nextItems = data[c];
                c++;
                while (nextItems > 0)
                {
                    nextItemId = data[c];
                    c++;
                    UShorts[nextItemId] = Utilities.Bytes.ReadUShort(data, c);
                    c += 2;
                    nextItems--;
                    itemCount--;
                }
                if (itemCount == 0)
                    return c - start;
            }

            // ints
            itemMin += Entry.DataType.UShorts;
            itemMax += Entry.DataType.Ints;
            if (Entry.DataType.Ints > 0)
            {
                nextItems = data[c];
                c++;
                while (nextItems > 0)
                {
                    nextItemId = data[c];
                    c++;
                    Ints[nextItemId] = Utilities.Bytes.ReadInt(data, c);
                    c += 4;
                    nextItems--;
                    itemCount--;
                }
                if (itemCount == 0)
                    return c - start;
            }

            // floats
            itemMin += Entry.DataType.Ints;
            itemMax += Entry.DataType.Floats;
            if (Entry.DataType.Floats > 0)
            {
                nextItems = data[c];
                c++;
                while (nextItems > 0)
                {
                    nextItemId = data[c];
                    c++;
                    Floats[nextItemId] = Utilities.Bytes.ReadFloat(data, c);
                    c += 4;
                    nextItems--;
                    itemCount--;
                }
                if (itemCount == 0)
                    return c - start;
            }

            // doubles
            itemMin += Entry.DataType.Floats;
            itemMax += Entry.DataType.Doubles;
            if (Entry.DataType.Doubles > 0)
            {
                nextItems = data[c];
                c++;
                while (nextItems > 0)
                {
                    nextItemId = data[c];
                    c++;
                    Doubles[nextItemId] = Utilities.Bytes.ReadDouble(data, c);
                    c += 8;
                    nextItems--;
                    itemCount--;
                }
                if (itemCount == 0)
                    return c - start;
            }

            // strings
            itemMin += Entry.DataType.Doubles;
            itemMax += Entry.DataType.Strings;
            int stringDepth = 0;
            int countRead = 0;
            string str;
            if (Entry.DataType.Strings > 0)
            {
                nextItems = data[c];
                c++;
                while(nextItems > 0)
                {
                    nextItemId = data[c];
                    c++;

                    // read the string header
                    stringDepth = Utilities.Bytes.ReadUShort(data, c);
                    c += 2;

                    // Do we need to recalculate string length here?
                    // technically we should, but the client should never
                    // need to use that info, so there's no point in doing
                    // so here.

                    str = Utilities.Bytes.ReadString(data, c, out countRead);
                    c += countRead;
                    if (stringDepth == 0)
                    {
                        Strings[nextItemId] = str;
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
                        Strings[nextItemId] = Strings[nextItemId] + str;
                    }

                    nextItems--;
                    itemCount--;
                }
                if (itemCount == 0)
                    return c - start;
            }
            return c - start;
        }
        #endregion full update

        // accessor methods
        #region accessors
        // bools
        public int GetBoolIndex(string name)
        {
            for (int i = 0; i < Entry.DataType.Bools; i++)
                if (Entry.DataType.Names[i] == name)
                    return i;
            return -1;
        }

        public bool GetBool(string name)
        {
            int index = GetBoolIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to get bool '" + name + "' but it does not exist for type '" + Entry.DataType.TypeName + "'");

            return Bools[index];
        }

        public void SetBool(string name, bool value)
        {
            int index = GetBoolIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to set bool '" + name + "' but it does not exist for type '" + Entry.DataType.TypeName + "'");

            Bools[index] = value;
            TrySetStale((ushort)index);
        }

        // bytes
        public int GetByteIndex(string name)
        {
            int offset = Entry.DataType.Bools;
            int max = offset + Entry.DataType.Bytes;
            for (int i = offset; i < max; i++)
                if (Entry.DataType.Names[i] == name)
                    return i - offset;
            return -1;
        }

        public byte GetByte(string name)
        {
            int index = GetByteIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to get byte '" + name + "' but it does not exist for type '" + Entry.DataType.TypeName + "'");

            return Bytes[index];
        }

        public void SetByte(string name, byte value)
        {
            int index = GetByteIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to set byte '" + name + "' but it does not exist for type '" + Entry.DataType.TypeName + "'");

            Bytes[index] = value;
            TrySetStale((ushort)(Entry.DataType.Bools + index));
        }

        // ushorts
        public int GetUShortIndex(string name)
        {
            int offset = Entry.DataType.Bools + Entry.DataType.Bytes;
            int max = offset + Entry.DataType.UShorts;
            for (int i = offset; i < max; i++)
                if (Entry.DataType.Names[i] == name)
                    return i - offset;
            return -1;
        }

        public ushort GetUShort(string name)
        {
            int index = GetUShortIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to get ushort '" + name + "' but it does not exist for type '" + Entry.DataType.TypeName + "'");

            return UShorts[index];
        }

        public void SetUShort(string name, ushort value)
        {
            int index = GetUShortIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to set ushort '" + name + "' but it does not exist for type '" + Entry.DataType.TypeName + "'");

            UShorts[index] = value;
            TrySetStale((ushort)(Entry.DataType.Bools + Entry.DataType.Bytes + index));
        }

        // ints
        public int GetIntIndex(string name)
        {
            int offset = Entry.DataType.Bools + Entry.DataType.Bytes + Entry.DataType.UShorts;
            int max = offset + Entry.DataType.Ints;
            for (int i = offset; i < max; i++)
                if (Entry.DataType.Names[i] == name)
                    return i - offset;
            return -1;
        }

        public int GetInt(string name)
        {
            int index = GetIntIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to get int '" + name + "' but it does not exist for type '" + Entry.DataType.TypeName + "'");

            return Ints[index];
        }

        public void SetInt(string name, int value)
        {
            int index = GetIntIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to set int '" + name + "' but it does not exist for type '" + Entry.DataType.TypeName + "'");

            Ints[index] = value;
            TrySetStale((ushort)(Entry.DataType.Bools + Entry.DataType.Bytes +
                Entry.DataType.UShorts + index));
        }

        // floats
        public int GetFloatIndex(string name)
        {
            int offset = Entry.DataType.Bools + Entry.DataType.Bytes + Entry.DataType.UShorts + Entry.DataType.Ints;
            int max = offset + Entry.DataType.Floats;
            for (int i = offset; i < max; i++)
                if (Entry.DataType.Names[i] == name)
                    return i - offset;
            return -1;
        }

        public float GetFloat(string name)
        {
            int index = GetFloatIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to get float '" + name + "' but it does not exist for type '" + Entry.DataType.TypeName + "'");

            return Floats[index];
        }

        public void SetFloat(string name, float value)
        {
            int index = GetFloatIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to set float '" + name + "' but it does not exist for type '" + Entry.DataType.TypeName + "'");

            Floats[index] = value;
            TrySetStale((ushort)(Entry.DataType.Bools + Entry.DataType.Bytes +
                Entry.DataType.UShorts + Entry.DataType.Ints + index));
        }

        // doubles
        public int GetDoubleIndex(string name)
        {
            int offset = Entry.DataType.Bools + Entry.DataType.Bytes + Entry.DataType.UShorts
                + Entry.DataType.Ints + Entry.DataType.Floats;
            int max = offset + Entry.DataType.Doubles;
            for (int i = offset; i < max; i++)
                if (Entry.DataType.Names[i] == name)
                    return i - offset;
            return -1;
        }

        public double GetDouble(string name)
        {
            int index = GetDoubleIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to get double '" + name + "' but it does not exist for type '" + Entry.DataType.TypeName + "'");

            return Doubles[index];
        }

        public void SetDouble(string name, double value)
        {
            int index = GetDoubleIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to set double '" + name + "' but it does not exist for type '" + Entry.DataType.TypeName + "'");

            Doubles[index] = value;
            TrySetStale((ushort)(Entry.DataType.Bools + Entry.DataType.Bytes +
                Entry.DataType.UShorts + Entry.DataType.Ints +
                Entry.DataType.Floats + index));
        }

        // strings
        public int GetStringIndex(string name)
        {
            int offset = Entry.DataType.Bools + Entry.DataType.Bytes + Entry.DataType.UShorts
                + Entry.DataType.Ints + Entry.DataType.Floats + Entry.DataType.Doubles;
            int max = offset + Entry.DataType.Strings;
            for (int i = offset; i < max; i++)
                if (Entry.DataType.Names[i] == name)
                    return i - offset;
            return -1;
        }

        public string GetString(string name)
        {
            int index = GetStringIndex(name);
            if (index == -1)
                throw new Exception("DynamicDataEntry tried to get string '" + name + "' but it does not exist for type '" + Entry.DataType.TypeName + "'");

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
                throw new Exception("DynamicDataEntry tried to set string '" + name + "' but it does not exist for type '" + Entry.DataType.TypeName + "'");

            Strings[index] = value;
            StringLengths[index] = Utilities.Bytes.GetStringLength(value);

            // recalculate total string lengths
            TotalStringLengths = 0;
            for (int i = 0; i < Entry.DataType.Strings; i++)
                TotalStringLengths += StringLengths[i];

            TrySetStale((ushort)(Entry.DataType.Bools + Entry.DataType.Bytes +
                Entry.DataType.UShorts + Entry.DataType.Ints +
                Entry.DataType.Floats + Entry.DataType.Doubles + index));
        }
        #endregion accessors
    }
}
