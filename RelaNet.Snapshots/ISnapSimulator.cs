using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots
{
    public interface ISnapSimulator
    {
        void Loaded(NetExecutorSnapper snapper);

        // Advance: starting at a timestamp, go forward [times] 
        // timestamps + [tickms] ms and create snapshots if server.
        // server should skip going forward [tickms] ms 
        void ServerAdvance();

        void ClientAdvance(int times, float tickms);
    }
}
