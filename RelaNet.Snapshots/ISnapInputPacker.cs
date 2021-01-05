using RelaNet.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots
{
    public interface ISnapInputPacker<T> where T : struct
    {
        // must return c + bytes read
        int Read(ref T into, Receipt receipt, int c);

        // must return length, in bytes, that will be written from T
        int GetWriteLength(T from);
        void Write(T from, Sent sent);
    }
}
