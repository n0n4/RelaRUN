using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots
{
    public interface ISnapSimulator
    {
        void Loaded(NetExecutorSnapper snapper);

        // Pre-advance: runs before ServerAdvance, useful for operations
        // across entities prior to advancement.
        void ServerPreAdvance();

        // Advance: starting at a timestamp, go forward [times] 
        // timestamps + [tickms] ms and create snapshots if server.
        // server should skip going forward [tickms] ms 
        void ServerAdvance();

        // Post-advance: runs after ServerAdvance, useful for operations
        // across entities after advancement.
        void ServerPostAdvance();

        void ClientAdvance(int times, float tickms);
    }
}
