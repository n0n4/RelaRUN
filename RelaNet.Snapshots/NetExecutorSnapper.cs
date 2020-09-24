using System;
using RelaNet.Messages;
using RelaNet.Sockets;

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
         * each header includes a scene number, if it does not match, the message is thrown out
         * 
         * An overview of processing snap messages:
         * - each packet is affixed with a header that specifies its timestamp
         * - when a client connects, the server tells it what timestamp is current (TC)
         * - whenever new packets are received, they are rejected unless they are
         *   between TC-15 and TC+16 (we store 32 snaps at once)
         * - the client adjusts its current timestamp independently of the server
         * - because these are computed on delta time, they shouldn't get out of sync
         *   even if the server or client has a hiccup
         *   (NOTE: when tracking time elapsed, make sure to SUBTRACT ticktime rather
         *    than setting to 0 once ticktime is hit)
         * 
         */

        // what do the actual messages look like?
        // EventPacketHeader 
        // [ushort - timestamp] [ushort - snapper sequence number] [byte - scene sequence number]

        // EventGhostFirstClass
        // [byte - entityid] [ushort - entitytype] [byte - length (N)] [NxBytes - full pack]
        // EventGhostSecondClass
        // [ushort - entityid] [ushort - entitytype] [byte - length (N)] [NxBytes - full pack]

        // EventDeltaFirstClass
        // [byte - entityid] [byte - length (N)] [NxBytes - delta]
        // EventDeltaSecondClass
        // [ushort - entityid] [byte - length (N)] [NxBytes - delta]
        
        // Deghost: the entity phases out of existence. The server will not send new snapshots
        //          until the entity is reghosted for us.
        
        // EventDeghostFirstClass
        // [byte - entityid]
        // EventDeghostSecondClass
        // [ushort - entityid]  

        // Destruct: we can delete the entity and its history. Unless the entire scene is
        //           being destroyed, should only happen after an entity is deghosted for 
        //           a significant amount of time.

        // EventDestructFirstClass
        // [byte - entityid]
        // EventDestructSecondClass
        // [ushort - entityid]

        // SnapSettings: sent when player first joins the server

        // EventSnapSettings
        // [ushort - starting timestamp] [byte - starting scene]

        // NewScene: tells client to throw out all existing entities

        // EventNewScene
        // [byte - scene number]

        // Resend: If we receive a delta that cannot be processed, we ask the server
        //         to resend a full pack (ghost) for that entity

        // EventResendFirstClass
        // [byte - entityid] [ushort - timestamp]
        // EventResendSecondClass
        // [ushort - entityid] [ushort - timestamp]

        public ushort EventPacketHeader = 0;
        public ushort EventGhostFirstClass = 1;
        public ushort EventGhostSecondClass = 2;
        public ushort EventDeltaFirstClass = 3;
        public ushort EventDeltaSecondClass = 4;
        public ushort EventDeghostFirstClass = 5;
        public ushort EventDeghostSecondClass = 6;
        public ushort EventDestructFirstClass = 7;
        public ushort EventDestructSecondClass = 8;
        public ushort EventSnapSettings = 9;
        public ushort EventNewScene = 10;
        // client events
        public ushort EventResendFirstClass = 11;
        public ushort EventResendSecondClass = 12;

        private NetServer Server;

        private Sent[] PlayerSents = new Sent[8];
        private float[] TimeSincePlayerSents = new float[8];
        public float TimeSincePlayerSentMax = 20; // send 50 times a second by default

        private ISnapper[] Snappers = new ISnapper[8];

        private byte SceneNumber = 0;

        todo; // when we receive a DELTA that we cannot process
            // we need to respond to the server and say, 
            // please resend the FULL of this entity

        // Constructor
        public NetExecutorSnapper()
        {
            for (int i = 0; i < PlayerSents.Length; i++)
                PlayerSents[i] = Server.BeginNewSend(NetServer.SpecialNormal);
        }



        // Interface Methods
        public ushort Register(NetServer server, ushort startIndex)
        {
            Server = server;

            if (Server.IsHost)
            {
                SceneNumber = 1;
            }

            todo;
        }

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
            // send unprocessed player sents

        }

        public void PreTick(float elapsedMS)
        {
            throw new NotImplementedException();
        }

        public int Receive(Receipt receipt, PlayerInfo pinfo, ushort eventid, int c)
        {
            throw new NotImplementedException();
        }


        
        // Class Methods
        private void ExpandSnappers(int targetindex)
        {
            int nlen = Snappers.Length;
            while (nlen <= targetindex) nlen *= 2;

            ISnapper[] ns = new ISnapper[nlen];
            for (int i = 0; i < Snappers.Length; i++)
                ns[i] = Snappers[i];

            Snappers = ns;
        }

        public ushort AddSnapper(ISnapper sn)
        {
            ushort index = 0;
            while (true)
            {
                if (index >= Snappers.Length)
                    ExpandSnappers(index);

                if (Snappers[index] == null)
                {
                    Snappers[index] = sn;
                    return index;
                }

                index++;

                if (index >= ushort.MaxValue)
                    throw new Exception("Tried to add new Snapper, but already have too many!");
            }
        }

        public void SetSnapper(ISnapper sn, ushort id)
        {
            if (id >= Snappers.Length)
                ExpandSnappers(id);

            Snappers[id] = sn;
        }

        public void RemoveSnappers()
        {
            for (int i = 0; i < Snappers.Length; i++)
            {
                if (Snappers[i] == null)
                    continue;

                Snappers[i].Removed();
                Snappers[i] = null;
            }
        }

        public void ServerBeginNewScene()
        {
            if (!Server.IsHost)
                throw new Exception("Tried to begin new Snapper scene as client.");

            // clear all entities
            for (int i = 0; i < Snappers.Length; i++)
                if (Snappers[i] != null)
                    Snappers[i].ClearEntities();

            // increment the scene number
            SceneNumber++;

            // inform our clients
            Sent send = Server.GetFastOrderedAllSend(3);
            send.WriteUShort(EventNewScene);
            send.WriteByte(SceneNumber);
        }

        public Sent GetPlayerSent(byte pid, int requestedLength)
        {
            if (requestedLength > UdpClientExtensions.MaxUdpSize)
                throw new Exception("Packet splitting not supported yet");

            Sent send = PlayerSents[pid];
            if (send.Length + requestedLength <= UdpClientExtensions.MaxUdpSize)
                return send;
            // send the current send

            todo; // use reliable or retry?
            Server.SendRetry(send, Server.PlayerInfos.Values[Server.PlayerInfos.IdsToIndices[pid]]);
            // create a new send
            send = Server.BeginNewSend(NetServer.SpecialNormal);
            PlayerSents[pid] = send;
            return send;
        }
    }
}
