using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots
{
    public interface ISnapper
    {
        bool GhostFirst(byte entityid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp);
        bool GhostSecond(ushort entityid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp);

        bool UnpackDeltaFirst(byte entityid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp, ushort basisTimestamp);
        bool UnpackDeltaSecond(ushort entityid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp, ushort basisTimestamp);

        bool DeghostFirst(byte entityid, ushort timestamp);
        bool DeghostSecond(ushort entityid, ushort timestamp);

        bool DestructFirst(byte entityid);
        bool DestructSecond(ushort entityid);

        void Advance(ushort currentTime, float elapsedms);

        void Removed();
        void ClearEntities();
    }
}
