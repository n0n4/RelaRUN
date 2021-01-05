using RelaNet.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots
{
    public interface ISnapPacker<T> where T : struct
    {
        void Clear(ref T obj);

        byte PrepPackFull(T obj);
        byte PrepPackDelta(T obj, T basis);

        void PackFull(Sent sent);
        void PackDelta(Sent sent);

        void UnpackFull(ref T obj, byte[] blob, int start, int count);
        void UnpackDelta(ref T obj, T basis, byte[] blob, int start, int count);
    }
}
