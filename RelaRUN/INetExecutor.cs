using RelaRUN.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN
{
    public interface INetExecutor
    {
        ushort Register(NetServer server, ushort startIndex); // must return number of events reserved
        int Receive(Receipt receipt, PlayerInfo pinfo, ushort eventid, int c); // must return new c
        void PreTick(float elapsedMS);
        void PostTick(float elapsedMS);

        void PlayerAdded(PlayerInfo pinfo);
        void PlayerRemoved(PlayerInfo pinfo);

        // note: this is when we, as a client, confirm a connection to a server
        // and gain a playerid. This is NOT when a new client connects (see PlayerAdded)
        void ClientConnected(); // primarily for updating sents with valid pids
    }
}
