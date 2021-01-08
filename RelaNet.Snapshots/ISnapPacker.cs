using RelaNet.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots
{
    public interface ISnapPacker<T, V> 
        where T : struct // entity struct
        where V : struct // pack info struct
    {
        void Clear(ref T obj);

        byte PrepPackFull(T obj, out V packinfo);
        byte PrepPackDelta(T obj, T basis, out V packinfo);

        void PackFull(Sent sent, V packinfo);
        void PackDelta(Sent sent, V packinfo);

        void UnpackFull(ref T obj, byte[] blob, int start, int count);
        void UnpackDelta(ref T obj, T basis, byte[] blob, int start, int count);
    }
}
