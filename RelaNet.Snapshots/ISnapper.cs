using RelaNet.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots
{
    public interface ISnapper
    {
        void Register(NetExecutorSnapper netSnapper, byte etype);
        byte GetEntityType();

        byte PrepGhostFirst(byte entityid, ushort timestamp);
        byte PrepGhostSecond(ushort entityid, ushort timestamp);
        void WriteGhostFirst(Sent sent);
        void WriteGhostSecond(Sent sent);
        
        bool PrepDeltaFirst(byte entityid, ushort timestamp, ushort basisTimestamp,
            out byte len);
        bool PrepDeltaSecond(ushort entityid, ushort timestamp, ushort basisTimestamp,
            out byte len);
        void WriteDeltaFirst(Sent sent);
        void WriteDeltaSecond(Sent sent);

        bool UnpackGhostFirst(byte entityid,
            byte[] blob, int blobstart, int blobcount,
            ushort timestamp);
        bool UnpackGhostSecond(ushort entityid,
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

        void Advance(ushort currentTime);
        void LoadTimestamp(ushort timestamp);

        void Removed();
        void ClearEntities();
    }
}
