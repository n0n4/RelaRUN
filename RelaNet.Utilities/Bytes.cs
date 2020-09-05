using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace RelaNet.Utilities
{
    public static class Bytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUShort(byte[] msg, ushort val, int index)
        {
            msg[index] = (byte)val;
            msg[index + 1] = (byte)(val >> 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUShort(byte[] msg, int index)
        {
            if(BitConverter.IsLittleEndian)
                return (ushort)(msg[index] | (msg[index + 1] << 8));
            return (ushort)((msg[index] << 8) | msg[index + 1]);

            //return BitConverter.ToUInt16(msg, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt(byte[] msg, int val, int index)
        {
            msg[index] = (byte)val;
            msg[index + 1] = (byte)(val >> 8);
            msg[index + 2] = (byte)(val >> 16);
            msg[index + 3] = (byte)(val >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt(byte[] msg, int index)
        {
            if (BitConverter.IsLittleEndian)
                return (msg[index] | (msg[index + 1] << 8) | (msg[index + 2] << 16) | (msg[index + 3] << 24));
            return ((msg[index] << 24) | (msg[index + 1] << 16) | (msg[index + 2] << 8) | msg[index + 3]);

            //return BitConverter.ToUInt16(msg, index);
        }

        public static int GetStringLength(string s)
        {
            if (s == null)
                s = string.Empty;
            // note that we filter null chars to avoid injection attacks
            s = s.Replace('\0', ' ') + "\0";
            return Encoding.UTF8.GetByteCount(s);
        }

        public static int WriteString(byte[] msg, string s, int index)
        {
            if (s == null)
                s = string.Empty;
            byte[] valinbytes = Encoding.UTF8.GetBytes(s.Replace('\0', ' ') + "\0"); // note we add null character to end of string, making
                                                                                     // this a null-terminated string
            if (valinbytes.Length + index > msg.Length) 
            {
                throw new Exception("Not enough space to write String!");
            }
            int i = 0;
            while (i < valinbytes.Length)
            {
                msg[index + i] = valinbytes[i];
                i++;
            }
            return valinbytes.Length;
        }

        public static string ReadString(byte[] msg, int index)
        {
            if (index + 1 > msg.Length)
            {
                throw new Exception("Not enough space to read String!");
            }
            // read characters until we hit the end of a null character
            int i = index;
            while (msg[i] != 0 && i < msg.Length) // searching for the null character
            {
                i++;
            }
            return Encoding.UTF8.GetString(msg, index, i - index);
        }

        public static string ReadString(byte[] msg, int index, out int count)
        {
            if (index + 1 > msg.Length)
            {
                throw new Exception("Not enough space to read String!");
            }
            // read characters until we hit the end of a null character
            int i = index;
            while (msg[i] != 0 && i < msg.Length) // searching for the null character
            {
                i++;
            }
            count = (i - index) + 1;
            return Encoding.UTF8.GetString(msg, index, i - index);
        }
    }
}
