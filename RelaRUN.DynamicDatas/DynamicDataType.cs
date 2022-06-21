using RelaRUN.Messages;
using System;

namespace RelaRUN.DynamicDatas
{
    public class DynamicDataType
    {
        public string TypeName { get; private set; } = string.Empty;
        public int TypeNameLength { get; private set; } = 0;

        public byte Bools { get; private set; } = 0;
        public byte Bytes { get; private set; } = 0;
        public byte UShorts { get; private set; } = 0;
        public byte Ints { get; private set; } = 0;
        public byte Floats { get; private set; } = 0;
        public byte Doubles { get; private set; } = 0;
        public byte Strings { get; private set; } = 0;

        public string[] Names { get; private set; }
        public int[] NameLengths { get; private set; }
        public int TotalCount { get; private set; } = 0;

        public static int NamePacketSplitSize = 160;
        
        public DynamicDataType(
            string typeName,
            string[] bools = null,
            string[] bytes = null,
            string[] ushorts = null,
            string[] ints = null,
            string[] floats = null,
            string[] doubles = null,
            string[] strings = null)
        {
            TypeName = typeName;
            TypeNameLength = Utilities.Bytes.GetStringLength(typeName);

            int total = 0;

            if (bools != null)
            {
                Bools = (byte)bools.Length;
                total += bools.Length;
            }
            if (bytes != null)
            {
                Bytes = (byte)bytes.Length;
                total += bytes.Length;
            }
            if (ushorts != null)
            {
                UShorts = (byte)ushorts.Length;
                total += ushorts.Length;
            }
            if (ints != null)
            {
                Ints = (byte)ints.Length;
                total += ints.Length;
            }
            if (floats != null)
            {
                Floats = (byte)floats.Length;
                total += floats.Length;
            }
            if (doubles != null)
            {
                Doubles = (byte)doubles.Length;
                total += doubles.Length;
            }
            if (strings != null)
            {
                Strings = (byte)strings.Length;
                total += strings.Length;
            }

            TotalCount = total;
            Names = new string[total];
            NameLengths = new int[total];

            int c = 0;
            if (bools != null)
            {
                for (int i = 0; i < bools.Length; i++)
                {
                    Names[c] = bools[i];
                    NameLengths[c] = RelaRUN.Utilities.Bytes.GetStringLength(Names[c]);
                    c++;
                }
            }
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    Names[c] = bytes[i];
                    NameLengths[c] = RelaRUN.Utilities.Bytes.GetStringLength(Names[c]);
                    c++;
                }
            }
            if (ushorts != null)
            {
                for (int i = 0; i < ushorts.Length; i++)
                {
                    Names[c] = ushorts[i];
                    NameLengths[c] = RelaRUN.Utilities.Bytes.GetStringLength(Names[c]);
                    c++;
                }
            }
            if (ints != null)
            {
                for (int i = 0; i < ints.Length; i++)
                {
                    Names[c] = ints[i];
                    NameLengths[c] = RelaRUN.Utilities.Bytes.GetStringLength(Names[c]);
                    c++;
                }
            }
            if (floats != null)
            {
                for (int i = 0; i < floats.Length; i++)
                {
                    Names[c] = floats[i];
                    NameLengths[c] = RelaRUN.Utilities.Bytes.GetStringLength(Names[c]);
                    c++;
                }
            }
            if (doubles != null)
            {
                for (int i = 0; i < doubles.Length; i++)
                {
                    Names[c] = doubles[i];
                    NameLengths[c] = RelaRUN.Utilities.Bytes.GetStringLength(Names[c]);
                    c++;
                }
            }
            if (strings != null)
            {
                for (int i = 0; i < strings.Length; i++)
                {
                    Names[c] = strings[i];
                    NameLengths[c] = RelaRUN.Utilities.Bytes.GetStringLength(Names[c]);
                    c++;
                }
            }
        }

        // reuse a type
        public void LoadFromConfig(
            string typeName,
            string[] bools = null,
            string[] bytes = null,
            string[] ushorts = null,
            string[] ints = null,
            string[] floats = null,
            string[] doubles = null,
            string[] strings = null)
        {
            TypeName = typeName;
            TypeNameLength = Utilities.Bytes.GetStringLength(typeName);

            int total = 0;

            if (bools != null)
            {
                Bools = (byte)bools.Length;
                total += bools.Length;
            }
            else
                Bools = 0;

            if (bytes != null)
            {
                Bytes = (byte)bytes.Length;
                total += bytes.Length;
            }
            else
                Bytes = 0;

            if (ushorts != null)
            {
                UShorts = (byte)ushorts.Length;
                total += ushorts.Length;
            }
            else
                UShorts = 0;

            if (ints != null)
            {
                Ints = (byte)ints.Length;
                total += ints.Length;
            }
            else
                Ints = 0;

            if (floats != null)
            {
                Floats = (byte)floats.Length;
                total += floats.Length;
            }
            else
                Floats = 0;

            if (doubles != null)
            {
                Doubles = (byte)doubles.Length;
                total += doubles.Length;
            }
            else
                Doubles = 0;

            if (strings != null)
            {
                Strings = (byte)strings.Length;
                total += strings.Length;
            }
            else
                Strings = 0;

            TotalCount = total;
            if (Names == null || total > Names.Length)
            {
                Names = new string[total];
                NameLengths = new int[total];
            }

            int c = 0;
            if (bools != null)
            {
                for (int i = 0; i < bools.Length; i++)
                {
                    Names[c] = bools[i];
                    NameLengths[c] = RelaRUN.Utilities.Bytes.GetStringLength(Names[c]);
                    c++;
                }
            }
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    Names[c] = bytes[i];
                    NameLengths[c] = RelaRUN.Utilities.Bytes.GetStringLength(Names[c]);
                    c++;
                }
            }
            if (ushorts != null)
            {
                for (int i = 0; i < ushorts.Length; i++)
                {
                    Names[c] = ushorts[i];
                    NameLengths[c] = RelaRUN.Utilities.Bytes.GetStringLength(Names[c]);
                    c++;
                }
            }
            if (ints != null)
            {
                for (int i = 0; i < ints.Length; i++)
                {
                    Names[c] = ints[i];
                    NameLengths[c] = RelaRUN.Utilities.Bytes.GetStringLength(Names[c]);
                    c++;
                }
            }
            if (floats != null)
            {
                for (int i = 0; i < floats.Length; i++)
                {
                    Names[c] = floats[i];
                    NameLengths[c] = RelaRUN.Utilities.Bytes.GetStringLength(Names[c]);
                    c++;
                }
            }
            if (doubles != null)
            {
                for (int i = 0; i < doubles.Length; i++)
                {
                    Names[c] = doubles[i];
                    NameLengths[c] = RelaRUN.Utilities.Bytes.GetStringLength(Names[c]);
                    c++;
                }
            }
            if (strings != null)
            {
                for (int i = 0; i < strings.Length; i++)
                {
                    Names[c] = strings[i];
                    NameLengths[c] = RelaRUN.Utilities.Bytes.GetStringLength(Names[c]);
                    c++;
                }
            }
        }

        public void Clear()
        {
            Bools = 0;
            Bytes = 0;
            UShorts = 0;
            Ints = 0;
            Floats = 0;
            Doubles = 0;
            Strings = 0;
            TotalCount = 0;

            if (Names != null)
            {
                for (int i = 0; i < Names.Length; i++)
                {
                    Names[i] = string.Empty;
                    NameLengths[i] = 0;
                }
            }
        }



        // Header Methods
        public int GetHeaderLength()
        {
            return 7 + TypeNameLength;
        }

        public int WriteHeader(byte[] msg, int start)
        {
            msg[start] = Bools;
            msg[start + 1] = Bytes;
            msg[start + 2] = UShorts;
            msg[start + 3] = Ints;
            msg[start + 4] = Floats;
            msg[start + 5] = Doubles;
            msg[start + 6] = Strings;

            Utilities.Bytes.WriteString(msg, TypeName, start + 7);

            return GetHeaderLength();
        }

        public void WriteHeader(Sent sent)
        {
            sent.Length += WriteHeader(sent.Data, sent.Length);
        }

        public int LoadHeader(byte[] msg, int start)
        {
            for (int i = 0; i < TotalCount; i++)
            {
                Names[i] = string.Empty;
                NameLengths[i] = 0;
            }

            int total = 0;
            Bools = msg[start]; total += Bools;
            Bytes = msg[start + 1]; total += Bytes;
            UShorts = msg[start + 2]; total += UShorts;
            Ints = msg[start + 3]; total += Ints;
            Floats = msg[start + 4]; total += Floats;
            Doubles = msg[start + 5]; total += Doubles;
            Strings = msg[start + 6]; total += Strings;

            TotalCount = total;
            if (Names.Length < total)
            {
                Names = new string[total];
                NameLengths = new int[total];
            }

            // now read the typename
            int typenamelen = 0;
            TypeName = Utilities.Bytes.ReadString(msg, start + 7, out typenamelen);
            TypeNameLength = typenamelen;

            return GetHeaderLength();
        }



        // Names Methods
        public int GetNamesCount()
        {
            int splits = 1;
            int currentCount = 3;
            for (int i = 0; i < TotalCount; i++)
            {
                currentCount += NameLengths[i];
                if (currentCount >= NamePacketSplitSize)
                {
                    splits++;
                    currentCount = 3;
                }
            }

            return splits;
        }

        private int NameWriteIndex = 0;
        public int BeginNamesWrite()
        {
            NameWriteIndex = 0;

            return GetNamesCount();
        }

        public int GetNextNamesLength()
        {
            int currentCount = 3;
            for (int i = NameWriteIndex; i < TotalCount; i++)
            {
                currentCount += NameLengths[i];
                if (currentCount >= NamePacketSplitSize)
                {
                    return currentCount;
                }
            }
            return currentCount;
        }

        public int WriteNextNames(byte[] msg, int start)
        {
            // packet structure:
            // [ushort - name starting index] [byte - number of names (N)] [stringxN - the next N names]

            // write in starting index
            Utilities.Bytes.WriteUShort(msg, (ushort)NameWriteIndex, start);

            int currentCount = 3;
            int written = 0;
            for (int i = NameWriteIndex; i < TotalCount; i++)
            {
                Utilities.Bytes.WriteString(msg, Names[i], start + currentCount);
                currentCount += NameLengths[i];
                written++;
                if (currentCount >= NamePacketSplitSize)
                {
                    break;
                }
            }

            NameWriteIndex += written;
            // write in number of names contained here
            msg[start + 2] = (byte)written;
            return currentCount;
        }

        public void WriteNextNames(Sent sent)
        {
            sent.Length += WriteNextNames(sent.Data, sent.Length);
        }
        
        public int LoadNames(byte[] msg, int start)
        {
            // first, read the starting index
            int nameIndex = Utilities.Bytes.ReadUShort(msg, start); start += 2;
            // and the count
            byte count = msg[start]; start++;

            string name;
            int namelen;
            int readbytes = 0;
            for (int i = 0; i < count; i++)
            {
                name = Utilities.Bytes.ReadString(msg, start, out namelen);
                start += namelen;
                readbytes += namelen;

                if (nameIndex < Names.Length)
                    Names[nameIndex] = name;

                nameIndex++;
            }

            return readbytes + 3;
        }
    }
}
