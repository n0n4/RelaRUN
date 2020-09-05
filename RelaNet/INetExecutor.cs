using RelaNet.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet
{
    public interface INetExecutor
    {
        ushort Register(NetServer server, ushort startIndex); // must return number of events reserved
        int Receive(Receipt receipt, PlayerInfo pinfo, ushort eventid, int c); // must return new c
        void PreTick(float elapsedMS);
        void PostTick(float elapsedMS);

        void PlayerAdded(PlayerInfo pinfo);
        void PlayerRemoved(PlayerInfo pinfo);
    }
}
