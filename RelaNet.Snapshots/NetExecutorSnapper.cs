using System;
using RelaNet.Messages;

namespace RelaNet.Snapshots
{
    public class NetExecutorSnapper : INetExecutor
    {
        /*
         * 
         * Theory
         * maintain own Sents per player
         * start each Sent with a header message that contains a sequence number
         * and a timestamp
         * track each entity snapshot under each sequence number and the timestamp
         * 
         * 
         */

        // what do the actual messages look like?
        // EventPacketHeader 
        // [ushort - timestamp] [ushort - snapper sequence number]

        // EventGhostFirstClass
        // [byte - entityid] [ushort - entitytype] [byte - length (N)] [NxBytes - full pack]
        // EventGhostSecondClass
        // [ushort - entityid] [ushort - entitytype] [byte - length (N)] [NxBytes - full pack]

        // EventDeltaFirstClass
        // [byte - entityid] [byte - length (N)] [NxBytes - delta]
        // EventDeltaSecondClass
        // [ushort - entityid] [byte - length (N)] [NxBytes - delta]

        // EventDeghostFirstClass
        // [byte - entityid]
        // EventDeghostSecondClass
        // [ushort - entityid]  

        public ushort EventPacketHeader = 0;
        public ushort EventGhostFirstClass = 1;
        public ushort EventGhostSecondClass = 2;
        public ushort EventDeltaFirstClass = 3;
        public ushort EventDeltaSecondClass = 4;
        public ushort EventDeghostFirstClass = 5;
        public ushort EventDeghostSecondClass = 6;

        private Sent[] PlayerSents = new Sent[8];


        public void PlayerAdded(PlayerInfo pinfo)
        {
            throw new NotImplementedException();
        }

        public void PlayerRemoved(PlayerInfo pinfo)
        {
            throw new NotImplementedException();
        }

        public void PostTick(float elapsedMS)
        {
            throw new NotImplementedException();
        }

        public void PreTick(float elapsedMS)
        {
            throw new NotImplementedException();
        }

        public int Receive(Receipt receipt, PlayerInfo pinfo, ushort eventid, int c)
        {
            throw new NotImplementedException();
        }

        public ushort Register(NetServer server, ushort startIndex)
        {
            throw new NotImplementedException();
        }
    }
}
