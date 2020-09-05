using RelaNet.Messages;
using RelaNet.Sockets;
using RelaNet.Utilities;
using RelaStructures;
using System;
using System.Collections;
using System.Net;

namespace RelaNet
{
    public class NetServer
    {
        public NetLogger NetLogger;
        public bool Opened { private set; get; } = false;
        public ISocket Socket;

        public const byte SpecialNormal = 0; // for regular reliable or retry messages
        public const byte SpecialIsImmediate = 1;
        public const byte SpecialChallengeRequest = 2;
        public const byte SpecialChallengeResponse = 3;
        public const byte SpecialOrdered = 4;

        public const ushort EventDisconnect = 1;
        public const ushort EventNewClient = 2;
        public const ushort EventLostClient = 3;
        public const ushort EventHeartbeat = 4;
        public const ushort EventIgnore = 5;
        public const ushort EventChangeMaxPlayers = 6;

        // Executors process messages that the server receives
        public INetExecutor[] Executors = new INetExecutor[4];
        public int ExecutorCount = 0;
        private ushort ExecutorEventCount = 7; // <-- This must be equal to 1 + the highest value
                                               // default event (EventChangeMaxPlayers at time of writing)
        private ushort[] ExecutorEventStart = new ushort[4];


        // Players
        public StructReArray<PlayerInfo> PlayerInfos;

        public string Password = string.Empty;
        public int MaxPlayers { private set; get; } = 8;


        // Sents
        private ReArrayIdPool<Sent> Sents;
        private ushort NextMessageId = 1;
        private int[] MessageIdToPoolIdMap;
        private int[] PlayerLastReceivedMessageId; // last messageid we have received from a player
        private ushort[][] PlayerReceipts; // what we have received from other players
        private BitArray[] OurReceipts; // what other players have received from us
        private float[] TimeSinceReceivedPlayer;
        public float TimeSinceReceivedPlayerMax = 60000; // if we don't receive for this many ms
                                                         // then remove the client
        public float TimeSinceMissingReliableMax = 90000; // if a reliabe message is not acked in this
                                                          // amount of time, remove the client
        private float TimeSinceHeartbeat = 0;
        public float TimeSinceHeartbeatMax = 1000; // how often to send a heartbeat message
        public float TimeSinceResends = 0; // time between resending old messages
        public float TimeSinceResendsMax = 15;
        
        // Our Info
        public byte OurPlayerId = 0; // server is playerid 0 by default
        public string OurName = "";
        public bool IsHost = true;


        // Convenience Sents
        private Sent CurrentReliableAllSent; // convenience for sending reliable messages to all
        private float TimeSinceReliableAllSent = 0;
        public float TimeSinceReliableAllSentMax = 20; // send about 50 times a second by default

        private Sent CurrentRetryAllSent;
        private float TimeSinceRetryAllSent = 0;
        public float TimeSinceRetryAllSentMax = 20;
        public int RetryAllDefault = 3; // default number of times to retry

        private Sent[] CurrentReliablePlayerSent; // convenience for sending reliable messages to specific players
        private float TimeSinceReliablePlayerSent = 0;
        private float TimeSinceReliablePlayerSentMax = 20;

        private Sent CurrentOrderedAllSent; // convenience for sending Ordered messages to all
        private float TimeSinceOrderedAllSent = 0;
        public float TimeSinceOrderedAllSentMax = 20; // send about 50 times a second by default
        private ushort[] OrderedMids = new ushort[10];
        private int OrderedCount = 0;

        private Sent CurrentFastOrderedAllSent; // convenience for sending Fast-Ordered messages to all
        private Sent[] CurrentFastOrderedPlayerSent; // convenience for sending reliable messages to specific players
        private float TimeSinceFastOrderedSent = 0;
        public float TimeSinceFastOrderedSentMax = 20;
        private ushort[] FastOrderPlayerIndex = new ushort[2]; // order that we're sending
        private ushort[] FastOrderPlayerReceiptIndex = new ushort[2]; // order that we're recieving
        private bool FastOrderAccessedAll = false;
        private bool FastOrderAccessedPlayer = false;


        // For client challenge request
        private bool InChallengeRequest = false;
        private Action<EChallengeResponse> ChallengeRequestCallback = null;
        private float TimeSinceChallengeAttempt = 0;
        public float TimeSinceChallengeAttemptMax = 200; // send about 5 times a second by default
        private float TimeSinceChallengeStarted = 0;
        public float TimeSinceChallengeStartedMax = 30000; // wait 30 seconds max for challenge response
        public int ChallengeKey = 0;
        public string ChallengePassword = "";
        public string ChallengeName = "";
        public bool ClientConnected { private set; get; } = false;


        // This random must only be used for inconsequential, no sync required activities
        // for instance, generating a challenge key.
        public Random NonSyncRandom = new Random();


        // Callbacks
        public Action<string> AbandonCallback = null;
        public Action<PlayerInfo> PlayerRemovedCallback = null;
        public Action<PlayerInfo> PlayerAddedCallback = null;
        public Action<int> MaxPlayersChangedCallback = null;



        public NetServer(bool ishost, IPEndPoint hostendpoint = null, Action<string> logCallback = null)
        {
            IsHost = ishost;

            // create net logger (default behavior if not specified)
            if (logCallback == null)
                logCallback = (s) => { };
            NetLogger = new NetLogger(logCallback);

            PlayerInfos = new StructReArray<PlayerInfo>(16, byte.MaxValue,
                PlayerInfoClearAction, PlayerInfoMoveAction);
            
            Sents = new ReArrayIdPool<Sent>(100, 100000,
                () => { return new Sent(); },
                (s) => {
                    // when a sent is returned, clear it from our messageidtopoolidmap
                    if (s.HasMessageId)
                        MessageIdToPoolIdMap[s.MessageId] = -1;
                    s.Clear();
                });

            CurrentReliableAllSent = BeginNewSend(SpecialNormal);
            CurrentRetryAllSent = BeginNewSend(SpecialNormal);
            CurrentOrderedAllSent = BeginNewSend(SpecialNormal);

            MessageIdToPoolIdMap = new int[ushort.MaxValue + 1];
            for (int i = 0; i < MessageIdToPoolIdMap.Length; i++)
                MessageIdToPoolIdMap[i] = -1;

            // last received starts at -1 to indicate that nothing has been received
            PlayerLastReceivedMessageId = new int[] { -1 };

            // generate Receipts by starting with every index pointing to itself
            // as no messages have been received
            PlayerReceipts = new ushort[1][];
            PlayerReceipts[0] = new ushort[ushort.MaxValue + 1];
            for (int i = 0; i < PlayerReceipts[0].Length; i++)
                PlayerReceipts[0][i] = (ushort)i;

            OurReceipts = new BitArray[2];
            for (int i = 0; i < OurReceipts.Length; i++)
                OurReceipts[i] = new BitArray(ushort.MaxValue + 1);

            TimeSinceReceivedPlayer = new float[1];
            CurrentReliablePlayerSent = new Sent[1];
            CurrentFastOrderedPlayerSent = new Sent[1];

            if (!ishost)
            {
                // do client startup
                if (hostendpoint == null)
                    throw new Exception("Must specify host endpoint for client net");
                // add a player for the host
                AddPlayer(hostendpoint, "*HOST", 0);
            }
            else
            {
                // reserve a spot for the host (inactive so we don't send to ourself)
                AddPlayer((IPEndPoint)UdpClientExtensions.anyV4Endpoint, "*HOST", 0, active: false);
                OurName = "*HOST";
            }
        }


        // Executors
        public void AddExecutor(INetExecutor exec)
        {
            if (ExecutorCount == Executors.Length)
            {
                // need to resize
                INetExecutor[] newexecs = new INetExecutor[ExecutorCount * 2];
                for (int i = 0; i < ExecutorCount; i++)
                    newexecs[i] = Executors[i];
                Executors = newexecs;

                ushort[] newexecindices = new ushort[ExecutorCount * 2];
                for (int i = 0; i < ExecutorCount; i++)
                    newexecindices[i] = ExecutorEventStart[i];
                ExecutorEventStart = newexecindices;
            }

            Executors[ExecutorCount] = exec;
            ExecutorEventStart[ExecutorCount] = ExecutorEventCount;
            ExecutorCount++;

            // now register the executor
            ExecutorEventCount += exec.Register(this, ExecutorEventCount);
        }


        // PlayerInfos
        private void PlayerInfoClearAction(ref PlayerInfo obj)
        {
            obj.Clear();
        }

        private void PlayerInfoMoveAction(ref PlayerInfo from, ref PlayerInfo to)
        {
            from.Move(ref to);
        }


        // Open and Close
        public void OpenUdp(int port, int maxPort, int maxQueueSize = 100000)
        {
            // note about maxQueueSize: default allows up to 50mb of messages
            // to be stored at once for processing, which should be extremely
            // in excess of what is necessary for regular operation.

            if (Opened)
                throw new Exception("Server already open, cannot open again!");

            Opened = true;
            Socket = new UdpSocket(port, maxPort, maxQueueSize, NetLogger);
        }

        public void OpenVirtualToUDPServer(NetServer targetServer,
            IPEndPoint endpoint, int maxQueueSize = 100000)
        {
            if (Opened)
                throw new Exception("Server already open, cannot open again!");

            UdpSocket usoc = targetServer.Socket as UdpSocket;

            ISocket[] clients = new ISocket[] { targetServer.Socket };
            IPEndPoint[] addresses = new IPEndPoint[] { usoc.DirectFromPoint };

            Opened = true;
            VirtualSocket vs = new VirtualSocket(clients, addresses, maxQueueSize, endpoint);
            Socket = vs;

            usoc.AddDirectTarget(vs, endpoint);
        }

        public VirtualSocket OpenVirtual(VirtualSocket[] clients, IPEndPoint[] addresses,
            IPEndPoint endpoint, int maxQueueSize = 100000)
        {
            // note: endpoint is where OUR messages appear to come from

            if (Opened)
                throw new Exception("Server already open, cannot open again!");

            Opened = true;
            VirtualSocket vs = new VirtualSocket(clients, addresses, maxQueueSize, endpoint);
            Socket = vs;
            return vs;
        }

        private void Close()
        {
            Socket.Close();
            Opened = false;
        }

        public void Abandon(string reason)
        {
            SendDisconnect(reason);
            if (AbandonCallback != null)
                AbandonCallback(reason);
            if (NetLogger.On)
                NetLogger.Log("Abandoning NetServer: " + reason);
            ClientConnected = false;
            Close();
        }


        // Processing
        public void Tick(float elapsedms)
        {
            // handle pretick for each executor
            for (int i = 0; i < ExecutorCount; i++)
                Executors[i].PreTick(elapsedms);

            Socket.Tick(elapsedms);

            if (IsHost)
            {
                for (int i = 1; i < TimeSinceReceivedPlayer.Length; i++)
                {
                    // make sure this player exists before we time for them
                    if (!PlayerInfos.Values[PlayerInfos.IdsToIndices[i]].Active)
                        continue;

                    TimeSinceReceivedPlayer[i] += elapsedms;

                    if (TimeSinceReceivedPlayer[i] >= TimeSinceReceivedPlayerMax)
                    {
                        RemovePlayer((byte)i, "Connection inactivity");
                    }
                }
            }
            else
            {
                TimeSinceReceivedPlayer[0] += elapsedms;
                if(TimeSinceReceivedPlayer[0] >= TimeSinceReceivedPlayerMax)
                {
                    Abandon("Received no messages from server for " + TimeSinceReceivedPlayerMax + " ms.");
                    return;
                }
            }
            
            ProcessFinalize();

            // process resends FIRST, so that if we send any new messages in the 
            // following work, we do not immediately duplicate those sends
            TimeSinceResends += elapsedms;
            if (TimeSinceResends >= TimeSinceResendsMax)
            {
                ProcessResends(TimeSinceResends);
                TimeSinceResends = 0;
            }

            Process();

            // send any batched sents and refresh them
            if (TimeSinceReliableAllSent < TimeSinceReliableAllSentMax)
                TimeSinceReliableAllSent += elapsedms;
            // only send if it has any packets in it
            if (TimeSinceReliableAllSent >= TimeSinceReliableAllSentMax &&
                CurrentReliableAllSent.Length > Sent.EmptySizeWithAck)
            {
                TimeSinceReliableAllSent = 0;
                SendReliableAll(CurrentReliableAllSent);
                CurrentReliableAllSent = BeginNewSend(SpecialNormal);
            }

            if (TimeSinceRetryAllSent < TimeSinceRetryAllSentMax)
                TimeSinceRetryAllSent += elapsedms;
            // only send if it has any packets in it
            if (TimeSinceRetryAllSent >= TimeSinceRetryAllSentMax &&
                CurrentRetryAllSent.Length > Sent.EmptySizeWithAck)
            {
                TimeSinceRetryAllSent = 0;
                SendRetryAll(CurrentRetryAllSent);
                CurrentRetryAllSent = BeginNewSend(SpecialNormal);
            }
            
            TimeSinceReliablePlayerSent += elapsedms;
            if (TimeSinceReliablePlayerSent >= TimeSinceReliablePlayerSentMax &&
                CurrentReliablePlayerSent.Length > Sent.EmptySizeWithAck)
            {
                TimeSinceReliablePlayerSent = 0;
                Sent psent;
                for (int i = 0; i < CurrentReliablePlayerSent.Length; i++)
                {
                    psent = CurrentReliablePlayerSent[i];
                    if (psent == null)
                        continue;
                    // only send if it has any packets in it
                    if (psent.Length > Sent.EmptySizeWithAck)
                    {
                        SendReliable(psent, PlayerInfos.Values[PlayerInfos.IdsToIndices[i]]);
                        CurrentReliablePlayerSent[i] = BeginNewSend(SpecialNormal);
                    }
                }
            }

            if (TimeSinceOrderedAllSent < TimeSinceOrderedAllSentMax)
                TimeSinceOrderedAllSent += elapsedms;
            // only send if it has any packets in it
            if (TimeSinceOrderedAllSent >= TimeSinceOrderedAllSentMax &&
                CurrentOrderedAllSent.Length > Sent.EmptySizeWithAck) 
            {
                TimeSinceOrderedAllSent = 0;
                // we can only send it immediately if there are no other ordered packets
                // waiting, for ordered packets we only send one at a time
                if (OrderedCount == 0)
                    SendReliableAll(CurrentOrderedAllSent);
                // normally, we get mid assigned from sending. But we need it either way
                // so if we don't send, assign a mid now
                else if (!CurrentOrderedAllSent.HasMessageId)
                    AssignMessageId(CurrentOrderedAllSent);

                // mark the packet as ordered
                CurrentOrderedAllSent.IsOrdered = true;
                OrderedMids[OrderedCount] = CurrentOrderedAllSent.MessageId;
                // now get a new sent for the next ordered packet
                CurrentOrderedAllSent = BeginNewSend(SpecialNormal);
            }
            
            TimeSinceFastOrderedSent += elapsedms;
            // only send if it has any packets in it
            if (TimeSinceFastOrderedSent >= TimeSinceFastOrderedSentMax)
            {
                TimeSinceFastOrderedSent = 0;
                if (CurrentFastOrderedAllSent != null && CurrentFastOrderedAllSent.Length > Sent.EmptySizeWithOrderValue)
                {
                    SendFastOrderedAll(CurrentFastOrderedAllSent);
                    CurrentFastOrderedAllSent = null;
                }

                Sent psent;
                for (int i = 0; i < CurrentFastOrderedPlayerSent.Length; i++)
                {
                    psent = CurrentFastOrderedPlayerSent[i];
                    if (psent == null)
                        continue;
                    // only send if it has any packets in it
                    if (psent.Length > Sent.EmptySizeWithOrderValue)
                    {
                        SendReliable(psent, PlayerInfos.Values[PlayerInfos.IdsToIndices[i]]);
                        CurrentFastOrderedPlayerSent[i] = null;
                    }
                }
            }

            // send challenge request if we're requesting
            if (InChallengeRequest)
            {
                TimeSinceChallengeAttempt += elapsedms;
                TimeSinceChallengeStarted += elapsedms;
                if (TimeSinceChallengeStarted >= TimeSinceChallengeStartedMax)
                {
                    HandleChallengeResponse(EChallengeResponse.TIMEOUT);
                }
                else if (TimeSinceChallengeAttempt >= TimeSinceChallengeAttemptMax)
                {
                    TimeSinceChallengeAttempt = 0;
                    SendChallengeRequest();
                }
            }

            // send heartbeat regularly
            TimeSinceHeartbeat += elapsedms;
            if (TimeSinceHeartbeat >= TimeSinceHeartbeatMax)
            {
                TimeSinceHeartbeat = 0;
                SendHeartbeat();
            }

            // handle posttick for each executor
            for (int i = 0; i < ExecutorCount; i++)
                Executors[i].PostTick(elapsedms);
        }

        private void Process()
        {
            Receipt receipt;
            int c;
            PlayerInfo pinfo;
            byte specialid;
            ushort lastreceived;
            ushort ackmid;
            ushort nextunreceived;
            byte ackbyte;
            ushort eventid;
            ushort lasteventid = 0;
            int addedcount;

            bool foundExec = false;

            int skips = 0;
            while (Socket.CanRead(skips))
            {
                receipt = Socket.Read(skips);

                // handle the receipt
                c = 0;
                
                // to allow for easy congestion control
                // every packet begins with playerid, messageid, and a bool (is immediate)
                // then the ack, if it exists
                // then all proceeded data is repeated units of eventid, packetdata
                // this allows messages to be batched by default

                // 1. read the playerid
                receipt.PlayerId = receipt.Data[c]; c++;
                receipt.TargetPlayerId = receipt.Data[c]; c++;
                
                // target player id checking
                // if we are a client & have been assigned a playerid, inc messages must target
                // our playerid. we skip messages that aren't for us.
                // this should allow multiple players behind the same IP
                if ((ClientConnected || IsHost) && receipt.TargetPlayerId != OurPlayerId)
                {
                    if (NetLogger.On)
                        NetLogger.Log("Rejecting packet: addressed to player id " + receipt.TargetPlayerId + ", we are " + OurPlayerId + ".");

                    Socket.EndRead(skips);
                    continue;
                }

                // 2. read the eventid & MessageId
                receipt.MessageId = Bytes.ReadUShort(receipt.Data, c); c += 2;
                specialid = receipt.Data[c]; c++;
                receipt.IsImmediate = (specialid == SpecialIsImmediate);
                bool ordered = (specialid == SpecialOrdered);

                // this needs to occur without playerid checking
                if (specialid == SpecialChallengeRequest)
                {
                    c = ReceiveChallengeRequest(receipt, c);
                    Socket.EndRead(skips);
                    continue;
                }
                else if (specialid == SpecialChallengeResponse)
                {
                    c = ReceiveChallengeResponse(receipt, c);
                    Socket.EndRead(skips);
                    continue;
                }

                // now do playerid checking (compare playerid against acceptable ips)
                if (PlayerInfos.IdsToIndices.Length <= receipt.PlayerId)
                {
                    // this playerid does not exist
                    if (NetLogger.On)
                        NetLogger.Log("Rejecting packet: sender player id " + receipt.PlayerId + " out of bounds.");

                    Socket.EndRead(skips);
                    continue;
                }

                pinfo = PlayerInfos.Values[PlayerInfos.IdsToIndices[receipt.PlayerId]];
                if (!pinfo.EndPoint.Address.Equals(receipt.EndPoint.Address)
                    || pinfo.EndPoint.Port != receipt.EndPoint.Port)
                {
                    // this connection is not allowed to speak for this player
                    if (NetLogger.On)
                        NetLogger.Log("Rejecting packet: connection '" + receipt.EndPoint.Address + ":" + receipt.EndPoint.Port
                            + "' cannot speak for player id " + receipt.PlayerId + " (expected '" +
                            pinfo.EndPoint.Address + ":" + pinfo.EndPoint.Port + "')");

                    Socket.EndRead(skips);
                    continue;
                }

                // now we can treat the receipt as genuine

                if (!receipt.Processed)
                {
                    // mark this packet as received
                    MarkReceived(receipt.PlayerId, receipt.MessageId);

                    // reset their timer
                    TimeSinceReceivedPlayer[receipt.PlayerId] = 0;

                    // check for an ack
                    if (!receipt.IsImmediate)
                    {
                        // if it's not an immediate, there's an ack to be read
                        lastreceived = Bytes.ReadUShort(receipt.Data, c); c += 2;
                        ackmid = lastreceived;
                        // the 8 proceeding bytes are bitarrays
                        for (int i = 0; i < 8; i++)
                        {
                            ackbyte = receipt.Data[c]; c++;
                            if (ackbyte == 0)
                                continue; // no receipts
                            if (ackbyte == byte.MaxValue)
                            {
                                // all receipts
                                for (int o = 0; o < 8; o++)
                                {
                                    if (ackmid == 0)
                                        ackmid = ushort.MaxValue;
                                    else
                                        ackmid--;
                                    OurReceipts[receipt.PlayerId][ackmid] = true;
                                }
                                continue;
                            }
                            // neither all or none, must check every bit
                            for (int o = 0; o < 8; o++)
                            {
                                if (ackmid == 0)
                                    ackmid = ushort.MaxValue;
                                else
                                    ackmid--;

                                if (Bits.CheckBit(ackbyte, o))
                                    OurReceipts[receipt.PlayerId][ackmid] = true;
                            }
                        }
                        // the last 2 bytes are the next unreceived mid
                        nextunreceived = Bytes.ReadUShort(receipt.Data, c); c += 2;
                        if (nextunreceived != 0 || lastreceived != 0)
                        {
                            // in the case where both are 0, no messages have been received
                            // so we check that before confirming anything
                            OurReceipts[receipt.PlayerId][lastreceived] = true;
                            // now loop from the last ackmid to nextunreceived, confirming
                            while (ackmid != nextunreceived)
                            {
                                if (ackmid == 0)
                                    ackmid = ushort.MaxValue;
                                else
                                    ackmid--;
                                OurReceipts[receipt.PlayerId][ackmid] = true;
                            }
                        }
                    }
                }
                else if (!receipt.IsImmediate)
                {
                    // even if it was processed, we still need to bypass the ack
                    // if it exists, so we don't accidentally read it
                    c += 12;
                }

                // now that the handshake is done (or isn't, if we're an immediate),
                // then the rest of the data must be packet data

                // with one expection: ordered packets
                if (ordered)
                {
                    // read the order index 
                    // and *abort processing this packet if it is not 1+our last index
                    // from this player.
                    ushort orderindex = Bytes.ReadUShort(receipt.Data, c); c += 2;
                    ushort previndex = orderindex;
                    if (previndex == 0)
                        previndex = ushort.MaxValue;
                    else
                        previndex--;

                    if (FastOrderPlayerReceiptIndex[receipt.PlayerId] != previndex)
                    {
                        // we should ensure that we don't process the ack over and over
                        // and we don't reset player receipt timer over and over
                        // mark in the packet that it has been processed
                        receipt.Processed = true;
                        // do not endread here, and add one to our skips so we skip this packet
                        // the next time we read
                        skips++;
                        continue;
                    }

                    // if we got here, it means that we're reading this packet
                    // so update our receipt index
                    FastOrderPlayerReceiptIndex[receipt.PlayerId] = orderindex;
                }

                // read each packet within the receipt
                lasteventid = 0;
                eventid = 0;
                while (c < receipt.Length)
                {
                    // each packet begins with an eventid
                    lasteventid = eventid;
                    eventid = Bytes.ReadUShort(receipt.Data, c); c += 2;

                    // check for standard messages
                    if (eventid == EventNewClient)
                    {
                        if (!IsHost && pinfo.PlayerId == 0)
                        {
                            // the host does not accept these messages, only they can send them
                            // add the new client as active = false, to indicate that we won't
                            // try to send messages to them.
                            byte newpid = receipt.Data[c]; c++;
                            string name = Bytes.ReadString(receipt.Data, c, out addedcount);
                            c += addedcount;
                            AddPlayer(null, name, 0, true, newpid, false);
                        }
                        continue;
                    }
                    else if (eventid == EventLostClient)
                    {
                        if (!IsHost && pinfo.PlayerId == 0)
                        {
                            // the host does not accept these messages, only they can send them
                            byte lostpid = receipt.Data[c]; c++;
                            string reason = Bytes.ReadString(receipt.Data, c, out addedcount);
                            c += addedcount;
                            RemovePlayerClient(lostpid, reason);
                        }
                        continue;
                    }
                    else if (eventid == EventDisconnect)
                    {
                        if (IsHost)
                        {
                            // if host gets a disconnect, means a client is leaving
                            // all we need to do is remove the client
                            // this method will handle sending the "EventLostClient" msg
                            string reason = Bytes.ReadString(receipt.Data, c, out addedcount);
                            c += addedcount;
                            RemovePlayer(receipt.PlayerId, reason);
                        }
                        else if (pinfo.PlayerId == 0)
                        {
                            // if client gets a disconnect, means the host is booting them
                            // all we need to do is abandon, since we've been kicked
                            string reason = Bytes.ReadString(receipt.Data, c, out addedcount);
                            c += addedcount;
                            Abandon(reason);
                        }
                        continue;
                    }
                    else if (eventid == EventHeartbeat)
                    {
                        // the heartbeat contains a longack which is a list of the last
                        // 100 missing mids. when we receive this, we will loop over those
                        // mids and mark all the mids *between* those as being received.

                        ushort mid;
                        ushort tmid;
                        ushort lastmid = Bytes.ReadUShort(receipt.Data, c);
                        c += 2;
                        for (int i = 0; i < 100; i++)
                        {
                            mid = Bytes.ReadUShort(receipt.Data, c);
                            c += 2;

                            // compare mid to lastmid
                            tmid = mid;
                            if (tmid == ushort.MaxValue)
                                tmid = 0;
                            else
                                tmid++;

                            while (tmid != lastmid)
                            {
                                // mark this as received
                                OurReceipts[pinfo.PlayerId][tmid] = true;

                                if (tmid == ushort.MaxValue)
                                    tmid = 0;
                                else
                                    tmid++;
                            }

                            lastmid = mid;
                        }
                        continue;
                    }
                    else if (eventid == EventIgnore)
                    {
                        ushort ignoreMid = Bytes.ReadUShort(receipt.Data, c); c += 2;
                        MarkReceived(pinfo.PlayerId, ignoreMid);
                        continue;
                    }
                    else if (eventid == EventChangeMaxPlayers)
                    {
                        if (IsHost || (!IsHost && receipt.PlayerId != 0))
                        {
                            c++;
                            continue;
                        }

                        byte newmax = receipt.Data[c]; c++;
                        MaxPlayers = newmax;
                        if (MaxPlayersChangedCallback != null)
                            MaxPlayersChangedCallback(newmax);
                    }
                    else if (eventid >= ExecutorEventCount)
                    {
                        //throw new Exception("Unknown event received '" + eventid + "' from player '" + pinfo.Name + "'");
                        if (NetLogger.On)
                            NetLogger.Log("Unknown event received '" + eventid + "' from player '" + pinfo.Name + "'  id '" + pinfo.PlayerId + "'. Last Eventid was '" + lasteventid + "'");
                        c = receipt.Length + 1;
                        continue; // can't read the rest of the packet, skip it
                    }

                    foundExec = false;
                    for (int i = 0; i < ExecutorCount - 1; i++)
                    {
                        if (eventid >= ExecutorEventStart[i] && eventid < ExecutorEventStart[i + 1])
                        {
                            c = Executors[i].Receive(receipt, pinfo, eventid, c);
                            foundExec = true;
                            break;
                        }
                    }

                    if (eventid == 0)
                    {
                        //throw new Exception("Event zero received (zero is reserved and should never be sent)");
                        if (NetLogger.On)
                            NetLogger.Log("Event zero received (zero is reserved and should never be sent) from player '" + pinfo.Name + "' id '" + pinfo.PlayerId + "'. Last Eventid was '" + lasteventid + "'");
                        c = receipt.Length + 1;
                        continue; // can't read the rest of the packet, skip it
                    }

                    // if it's not one of the first 0 ... n-2, then it must be the n-1th executor
                    if (!foundExec)
                        c = Executors[ExecutorCount - 1].Receive(receipt, pinfo, eventid, c);
                }

                Socket.EndRead(skips);
            }
        }

        private void ProcessResends(float elapsedms)
        {
            ushort orderedMostRecent = OrderedMids[0];

            for (int i = 0; i < Sents.Count; i++)
            {
                Sent sent = Sents.Values[i];
                if (!sent.Finalized)
                    continue; // if the sent hasn't been finalized yet, don't bother sending it.

                if (sent.IsOrdered && sent.MessageId != orderedMostRecent)
                {
                    // if it's an ordered packet, and it's not the most recent,
                    // don't send it yet
                    continue;
                }

                if (sent.Retries > 0)
                    sent.Retries--;

                for (int o = 0; o < sent.AwaitingLength; o++)
                {
                    byte pid = sent.TargetPids[o];
                    if (PlayerInfos.IdsToIndices.Length <= pid)
                    {
                        sent.RemoveTargetIndex(o, skiphandshake: true);
                        o--;
                        continue;
                    }

                    // check if player has marked this message received
                    // if so, remove it
                    if (OurReceipts[pid][sent.MessageId])
                    {
                        sent.RemoveTargetIndex(o, skiphandshake: false);
                        o--;
                        continue;
                    }

                    // add the ack for this player to the message
                    sent.Data[Sent.TargetPidPosition] = pid;
                    sent.LoadAck(pid);
                    if (sent.IsFastOrdered)
                    {
                        sent.LoadFastOrderValue(pid);
                    }
                    Socket.Send(sent.Data, sent.Length, PlayerInfos.Values[PlayerInfos.IdsToIndices[pid]].EndPoint);

                    // increment the timer
                    sent.TargetWaits[o] += elapsedms;
                    // check if we have waited too long for confirmation
                    if (sent.TargetWaits[o] >= TimeSinceMissingReliableMax)
                    {
                        // disconnect this client
                        if (IsHost)
                        {
                            RemovePlayer(pid, "Connection unreliability");
                        }
                        else
                        {
                            Abandon("Server did not confirm one of our reliable messages for over " + TimeSinceMissingReliableMax + " ms");
                        }
                    }
                }

                // if everyone has the message, or we're done retrying, return it to the pool
                if (sent.Retries == 0
                    || sent.AwaitingLength == 0)
                {
                    if (sent.IsOrdered)
                    {
                        // if it was an ordered message, update the order list
                        OrderedCount--;
                        for (int o = 0; o < OrderedCount; o++)
                            OrderedMids[o] = OrderedMids[o + 1];
                    }

                    Sents.ReturnIndex(i);
                    i--;
                }
            }
        }

        private void ProcessFinalize()
        {
            // check each sent
            // if it is retry or reliable, we need to finalize it
            // what this does is prevent more players from being added to it
            // and (this is the important part) we tell each player who
            // is not a target of that sent, that they do not need that mid
            // we send EventIgnore with the mid attached. 

            // this is important because it avoids lock states where
            // we have sent many mids that don't belong to this player
            // and they can no longer easily ack/ follow the mid cycle

            Sent sent;
            Sent psent;
            bool found = false;
            for (int i = 0; i < Sents.Count; i++)
            {
                sent = Sents.Values[i];

                if (sent.IsImmediate || sent.Finalized || !sent.HasMessageId)
                    continue; // immediates don't count, as they have no mid

                sent.Finalized = true;

                // we need to find each player who is *not* a target of this sent
                for (int o = 0; o < PlayerInfos.Length; o++)
                {
                    byte pid = PlayerInfos.Values[o].PlayerId;

                    found = false;
                    // TODO: could there be a better way to do this search?
                    for (int p = 0; p < sent.AwaitingLength; p++)
                    {
                        if (sent.TargetPids[p] == pid)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        continue; // this player is a target, don't ignore

                    // this player is not a target, add ignore
                    psent = GetReliablePlayerSend(pid, 4);
                    Bytes.WriteUShort(psent.Data, EventIgnore, psent.Length); psent.Length += 2;
                    Bytes.WriteUShort(psent.Data, sent.MessageId, psent.Length); psent.Length += 2;
                }
            }
        }


        // Challenge Request
        #region Challenge Request
        public enum EChallengeResponse : byte
        {
            ACCEPT = 0,
            REJECT_FULL = 1,
            REJECT_WRONG_PASSWORD = 2,
            TIMEOUT = 3,
        }

        public void BeginChallengeRequest(Action<EChallengeResponse> callback, string name, string password)
        {
            if (InChallengeRequest)
                throw new Exception("Already sending challenge request, cannot open a new one");
            InChallengeRequest = true;
            TimeSinceChallengeAttempt = 0;
            TimeSinceChallengeStarted = 0;
            ChallengeRequestCallback = callback;

            ChallengeKey = NonSyncRandom.Next();
            ChallengeName = name;
            ChallengePassword = password;

            SendChallengeRequest();
        }

        private void SendChallengeRequest()
        {
            // for the client, trying to connect to server
            Sent send = BeginNewSend(SpecialChallengeRequest);
            // write our challenge key, password, and name
            Bytes.WriteInt(send.Data, ChallengeKey, send.Length); send.Length += 4;
            send.Length += Bytes.WriteString(send.Data, ChallengePassword, send.Length);
            send.Length += Bytes.WriteString(send.Data, ChallengeName, send.Length);

            SendImmediateAll(send);
        }

        private int ReceiveChallengeRequest(Receipt receipt, int c)
        {
            // in a challenge request, a client would like to connect to the server
            
            // clients hoping to connect generate a random challenge key
            // they send this key each time they ask to connect
            // this way, if we have already accepted the client, we know
            // not to accept them a second time.
            int challengeKey = Bytes.ReadInt(receipt.Data, c); c += 4;
            // read the password
            string password = Bytes.ReadString(receipt.Data, c);
            c += Bytes.GetStringLength(password);
            // read the name
            string name = Bytes.ReadString(receipt.Data, c);
            c += Bytes.GetStringLength(name);

            for (int i = 0; i < PlayerInfos.Count; i++)
            {
                PlayerInfo pinfo = PlayerInfos.Values[i];
                if (pinfo.ChallengeKey == challengeKey &&
                    pinfo.EndPoint.Address.Equals(receipt.EndPoint.Address))
                {
                    // already serviced this client
                    // send the accept again, in case they missed it
                    SendChallengeResponse(receipt, EChallengeResponse.ACCEPT, pinfo.PlayerId, pinfo.Name);
                    return c;
                }
            }

            if (PlayerInfos.Count == PlayerInfos.MaxLength
                || PlayerInfos.Count >= MaxPlayers)
            {
                SendChallengeResponse(receipt, EChallengeResponse.REJECT_FULL);

                // log the action
                if (NetLogger.On)
                    NetLogger.Log("Rejecting challenge from '" + name + "': server is full.");

                return c;
            }
            
            if (Password != string.Empty
                || Password != password)
            {
                SendChallengeResponse(receipt, EChallengeResponse.REJECT_WRONG_PASSWORD);

                // log the action
                if (NetLogger.On)
                    NetLogger.Log("Rejecting challenge from '" + name + "': password was incorrect.");

                return c;
            }
            
            string origname = name;

            // if we already have a player with this name, give them an addendum
            bool found = true;
            int timesfound = 2;
            while (found)
            {
                found = false;
                for (int i = 0; i < PlayerInfos.Count; i++)
                {
                    if (PlayerInfos.Values[i].Name == name)
                    {
                        name = origname + " " + timesfound;
                        timesfound++;
                        found = true;
                    }
                }
            }

            // add the player
            byte newpid = AddPlayer(receipt.EndPoint, name, challengeKey);

            // we need to send everyone a message that this client was added
            Sent sent = GetFastOrderedAllSend(Bytes.GetStringLength(name) + 1);
            // this message looks like the following:
            // [byte - pid] [string - name]
            Bytes.WriteUShort(sent.Data, EventNewClient, sent.Length); sent.Length += 2;
            sent.Data[sent.Length] = newpid; sent.Length++;
            sent.Length += Bytes.WriteString(sent.Data, name, sent.Length);

            // we need to send the new client a message for each client we have currently
            for (int i = 0; i < PlayerInfos.Count; i++)
            {
                PlayerInfo pinfo = PlayerInfos.Values[i];
                if (pinfo.PlayerId == newpid)
                    continue; // just sent this one

                sent = GetFastOrderedPlayerSend(newpid, Bytes.GetStringLength(pinfo.Name) + 1);
                Bytes.WriteUShort(sent.Data, EventNewClient, sent.Length); sent.Length += 2;
                sent.Data[sent.Length] = pinfo.PlayerId; sent.Length++;
                sent.Length += Bytes.WriteString(sent.Data, pinfo.Name, sent.Length);
            }

            // finally we can send the acceptance
            SendChallengeResponse(receipt, EChallengeResponse.ACCEPT, newpid, name);

            // log the action
            if (NetLogger.On)
                NetLogger.Log("Accepting challenge from '" + name + "'.");

            return c; // must return the bytes read from the receipt
        }

        private void SendChallengeResponse(Receipt receipt, EChallengeResponse responseType, byte pid = 0, string name = "")
        {
            Sent sent = BeginNewSend(SpecialChallengeResponse);

            sent.Data[sent.Length] = (byte)responseType;
            sent.Length++;

            if (responseType == EChallengeResponse.ACCEPT)
            {
                sent.Data[sent.Length] = pid;
                sent.Length++;
                sent.Length += Bytes.WriteString(sent.Data, name, sent.Length);
            }

            SendImmediate(sent, receipt.EndPoint, pid);
        }

        private int ReceiveChallengeResponse(Receipt receipt, int c)
        {
            // if this is accept, we need to read our playerid from the response
            // and our new name
            EChallengeResponse response = (EChallengeResponse)receipt.Data[c]; c++;
            if (response == EChallengeResponse.ACCEPT)
            {
                byte pid = receipt.Data[c]; c++;
                string name = Bytes.ReadString(receipt.Data, c, out int count); c += count;
                HandleChallengeResponse(response, pid, name);
                return c;
            }

            // in the latter case, we have a failure
            HandleChallengeResponse(response);

            return c;
        }

        private void HandleChallengeResponse(EChallengeResponse responseType, byte pid = 0, string name = "")
        {
            InChallengeRequest = false;
            ClientConnected = (responseType == EChallengeResponse.ACCEPT);
            
            if(ClientConnected)
            {
                // in an acceptance, we need to read our playerid & name from the response
                OurPlayerId = pid;
                OurName = name;
                
                // log the action
                if (NetLogger.On)
                    NetLogger.Log("Server accepted our challenge: '" + name + "' Id " + pid);

                // now that we know our playerid, update our convenience messages
                CurrentReliableAllSent.Data[0] = OurPlayerId;
                CurrentRetryAllSent.Data[0] = OurPlayerId;
                CurrentOrderedAllSent.Data[0] = OurPlayerId;
                for (int i = 0; i < CurrentReliablePlayerSent.Length; i++)
                    if (CurrentReliablePlayerSent[i] != null)
                        CurrentReliablePlayerSent[i].Data[0] = OurPlayerId;
            }

            if (responseType == EChallengeResponse.REJECT_FULL)
                Abandon("Server rejected our challenge: server is full.");
            else if (responseType == EChallengeResponse.REJECT_WRONG_PASSWORD)
                Abandon("Server rejected our challenge: incorrect password.");
            else if (responseType == EChallengeResponse.TIMEOUT)
                Abandon("Server did not respond: timeout.");
            else if (!ClientConnected)
                Abandon("Server rejected our challenge: unknown reason.");

            if (ChallengeRequestCallback != null)
                ChallengeRequestCallback(responseType);
        }
        #endregion Challenge Request

        #region Add-Remove Players
        public void ServerChangeMaxPlayers(int newmax)
        {
            if (!IsHost)
                return;

            if (newmax > byte.MaxValue || newmax < byte.MinValue)
                return;

            MaxPlayers = newmax;

            if (MaxPlayersChangedCallback != null)
                MaxPlayersChangedCallback(newmax);

            // inform clients
            Sent send = GetReliableAllSend(3);
            Bytes.WriteUShort(send.Data, EventChangeMaxPlayers, send.Length); send.Length += 2;
            send.Data[send.Length] = (byte)newmax; send.Length++;
        }

        // for the client, sending to the server if they leave
        // or the server, sending to the clients if they are booted
        private void SendDisconnect(string reason)
        {
            // if we're a client, and we're not connected, don't bother sending a disconnect
            if (!IsHost && !ClientConnected)
                return;

            Sent sent = BeginNewSend(SpecialIsImmediate);
            Bytes.WriteUShort(sent.Data, EventDisconnect, sent.Length); sent.Length += 2;
            sent.Length += Bytes.WriteString(sent.Data, reason, sent.Length);
            ClientConnected = false;

            SendImmediateAll(sent);
        }

        private void SendDisconnectSingle(IPEndPoint endpoint, string reason, byte targetpid)
        {
            // if we're a client, and we're not connected, don't bother sending a disconnect
            if (!IsHost && !ClientConnected)
                return;

            Sent sent = BeginNewSend(SpecialIsImmediate);
            Bytes.WriteUShort(sent.Data, EventDisconnect, sent.Length); sent.Length += 2;
            sent.Length += Bytes.WriteString(sent.Data, reason, sent.Length);
            ClientConnected = false;

            SendImmediate(sent, endpoint, targetpid);
        }

        public void RemovePlayer(byte playerid, string reason)
        {
            // send kicked message
            // note that this is ordered! we cannot have these messages get mixed up
            // because otherwise a remove could arrive before the join did
            // and clients might think another player is still connected
            Sent kicksent = GetFastOrderedAllSend(Bytes.GetStringLength(reason) + 3);
            Bytes.WriteUShort(kicksent.Data, EventLostClient, kicksent.Length); kicksent.Length += 2;
            kicksent.WriteByte(playerid);
            kicksent.Length += Bytes.WriteString(kicksent.Data, reason, kicksent.Length);

            // remove the player from our storage
            PlayerInfo pinfo = PlayerInfos.Values[PlayerInfos.IdsToIndices[playerid]];
            if (pinfo.Removed)
                return; // already removed this player, don't repeat it
            
            // log the action
            if (NetLogger.On)
                NetLogger.Log("Removing Player '" + pinfo.Name + "' Id " + pinfo.PlayerId + ": " + reason);

            SendDisconnectSingle(pinfo.EndPoint, reason, pinfo.PlayerId);
            PlayerInfos.ReturnId(playerid);

            // clear out any sents
            for (int i = 0; i < Sents.Count; i++)
            {
                Sent sent = Sents.Values[i];
                for (int o = 0; o < sent.AwaitingLength; o++)
                {
                    if (sent.TargetPids[o] == playerid)
                    {
                        sent.RemoveTargetIndex(o, skiphandshake: true);
                        o--;
                    }
                }
            }
        }

        private void RemovePlayerClient(byte playerid, string reason)
        {
            if (playerid == OurPlayerId)
            {
                // we're being evicted
                Abandon("Removed from server -- " + reason);
                return;
            }

            // find the player in our list
            if (playerid >= PlayerInfos.Length)
                return; // never added this player
            int index = PlayerInfos.IdsToIndices[playerid];
            if (index >= PlayerInfos.Count)
                return; // not active
            
            PlayerInfo pinfo = PlayerInfos.Values[index];

            // log the action
            if (NetLogger.On)
                NetLogger.Log("Host is removing Player '" + pinfo.Name + "' Id " + pinfo.PlayerId + ": " + reason);

            PlayerInfos.ReturnId(playerid);

            if (PlayerRemovedCallback != null)
                PlayerRemovedCallback(pinfo);
            
            for (int i = 0; i < ExecutorCount; i++)
                Executors[i].PlayerRemoved(pinfo);
        }

        private byte AddPlayer(IPEndPoint endpoint, string name, int challengeKey,
            bool forcepid = false, byte forcedpid = 0, bool active = true)
        {
            int pinfoindex = -1;
            if (forcepid)
            {
                pinfoindex = forcedpid;
                // when a specific playerid is requested,
                // first check if it is already checked out of the pool
                if (PlayerInfos.Length > forcedpid)
                {
                    int index = PlayerInfos.IdsToIndices[forcedpid];
                    if (index >= PlayerInfos.Count)
                    {
                        // this id hasn't been requested yet
                        // move the desired id into the next spot, then request
                        int moveid = PlayerInfos.IndicesToIds[PlayerInfos.Count];
                        int fidindex = PlayerInfos.IdsToIndices[forcedpid];

                        PlayerInfos.IdsToIndices[forcedpid] = PlayerInfos.Count;
                        PlayerInfos.IndicesToIds[PlayerInfos.Count] = forcedpid;
                        PlayerInfos.IdsToIndices[moveid] = fidindex;
                        PlayerInfos.IndicesToIds[fidindex] = moveid;

                        pinfoindex = PlayerInfos.Request();
                    }
                    else
                    {
                        pinfoindex = forcedpid; // this pid already exists, so use it as is
                    }
                }
                else
                {
                    PlayerInfos.Resize(forcedpid + 1);
                    // move the desired id into the next spot, then request
                    int moveid = PlayerInfos.IndicesToIds[PlayerInfos.Count];
                    int fidindex = PlayerInfos.IdsToIndices[forcedpid];

                    PlayerInfos.IdsToIndices[forcedpid] = PlayerInfos.Count;
                    PlayerInfos.IndicesToIds[PlayerInfos.Count] = forcedpid;
                    PlayerInfos.IdsToIndices[moveid] = fidindex;
                    PlayerInfos.IndicesToIds[fidindex] = moveid;

                    pinfoindex = PlayerInfos.Request();
                }
            }
            else
            {
                // first get a new player id
                pinfoindex = PlayerInfos.Request();
            }
            if (pinfoindex > byte.MaxValue)
                throw new Exception("Too many players added!");
            byte pid = (byte)pinfoindex;

            if (forcepid)
            {
                PlayerInfo pinfoOld = PlayerInfos.Values[PlayerInfos.IdsToIndices[pinfoindex]];
                if (pinfoOld.Name == name && pinfoOld.PlayerId == pid)
                    return pid; // this player has already been added
            }
            
            // log the action
            if (NetLogger.On)
                NetLogger.Log("Adding Player '" + name + "' Id " + pid);

            // set up our player info
            PlayerInfos.Values[PlayerInfos.IdsToIndices[pinfoindex]].Setup(pid, endpoint, name, active, challengeKey);

            // now, make sure we have our receipt structures in order
            while (PlayerLastReceivedMessageId.Length <= pinfoindex)
            {
                // not enough, need to expand
                int[] newlasts = new int[PlayerLastReceivedMessageId.Length * 2];
                for (int i = 0; i < PlayerLastReceivedMessageId.Length; i++)
                    newlasts[i] = PlayerLastReceivedMessageId[i];
                for (int i = PlayerLastReceivedMessageId.Length; i < newlasts.Length; i++)
                    newlasts[i] = -1;
                PlayerLastReceivedMessageId = newlasts;
            }
            PlayerLastReceivedMessageId[pinfoindex] = -1;

            while (PlayerReceipts.Length <= pinfoindex)
            {
                // not enough, need to expand
                ushort[][] newprecs = new ushort[PlayerReceipts.Length * 2][];
                for (int i = 0; i < PlayerReceipts.Length; i++)
                    newprecs[i] = PlayerReceipts[i];
                for (int i = PlayerReceipts.Length; i < newprecs.Length; i++)
                {
                    newprecs[i] = new ushort[ushort.MaxValue + 1];
                    for (int o = 0; o < newprecs[i].Length; o++)
                        newprecs[i][o] = (ushort)o;
                }
                PlayerReceipts = newprecs;
            }
            for (int i = 0; i < PlayerReceipts[pinfoindex].Length; i++)
                PlayerReceipts[pinfoindex][i] = (ushort)i;

            while (TimeSinceReceivedPlayer.Length <= pinfoindex)
            {
                // must expand
                float[] ntime = new float[TimeSinceReceivedPlayer.Length * 2];
                for (int i = 0; i < TimeSinceReceivedPlayer.Length; i++)
                    ntime[i] = TimeSinceReceivedPlayer[i];
                TimeSinceReceivedPlayer = ntime;
            }
            TimeSinceReceivedPlayer[pinfoindex] = 0;

            while  (CurrentReliablePlayerSent.Length <= pinfoindex)
            {
                // must expand
                Sent[] ncrps = new Sent[CurrentReliablePlayerSent.Length * 2];
                for (int i = 0; i < CurrentReliablePlayerSent.Length; i++)
                    ncrps[i] = CurrentReliablePlayerSent[i];
                CurrentReliablePlayerSent = ncrps;
            }
            if (CurrentReliablePlayerSent[pinfoindex] != null)
                Sents.Return(CurrentReliablePlayerSent[pinfoindex]);
            CurrentReliablePlayerSent[pinfoindex] = BeginNewSend(SpecialNormal);

            while (FastOrderPlayerIndex.Length <= pinfoindex)
            {
                ushort[] nfopi = new ushort[FastOrderPlayerIndex.Length * 2];
                for (ushort i = 0; i < FastOrderPlayerIndex.Length; i++)
                    nfopi[i] = FastOrderPlayerIndex[i];
                FastOrderPlayerIndex = nfopi;
            }
            FastOrderPlayerIndex[pinfoindex] = ushort.MaxValue;

            while (FastOrderPlayerReceiptIndex.Length <= pinfoindex)
            {
                ushort[] nfopi = new ushort[FastOrderPlayerReceiptIndex.Length * 2];
                for (ushort i = 0; i < FastOrderPlayerReceiptIndex.Length; i++)
                    nfopi[i] = FastOrderPlayerReceiptIndex[i];
                FastOrderPlayerReceiptIndex = nfopi;
            }
            FastOrderPlayerReceiptIndex[pinfoindex] = ushort.MaxValue;

            while (CurrentFastOrderedPlayerSent.Length <= pinfoindex)
            {
                // must expand
                Sent[] ncrps = new Sent[CurrentFastOrderedPlayerSent.Length * 2];
                for (int i = 0; i < CurrentFastOrderedPlayerSent.Length; i++)
                    ncrps[i] = CurrentFastOrderedPlayerSent[i];
                CurrentFastOrderedPlayerSent = ncrps;
            }
            if (CurrentFastOrderedPlayerSent[pinfoindex] != null)
            {
                Sents.Return(CurrentFastOrderedPlayerSent[pinfoindex]);
                CurrentFastOrderedPlayerSent[pinfoindex] = null;
            }

            while (OurReceipts.Length <= pinfoindex)
            {
                // must expand
                BitArray[] nbas = new BitArray[OurReceipts.Length * 2];
                for (int i = 0; i < OurReceipts.Length; i++)
                    nbas[i] = OurReceipts[i];
                for (int i = OurReceipts.Length; i < nbas.Length; i++)
                    nbas[i] = new BitArray(ushort.MaxValue + 1);
                OurReceipts = nbas;
            }
            OurReceipts[pinfoindex].SetAll(false); // clear our receipts

            // when a new player joins, we must begin a new FastOrderedAllSent
            // (because otherwise the new player won't be on the current buffer for it...)
            if (CurrentFastOrderedAllSent != null)
            {
                if (CurrentFastOrderedAllSent.Length > Sent.EmptySizeWithOrderValue)
                {
                    // send the current send
                    SendFastOrderedAll(CurrentFastOrderedAllSent);

                    // set to null so it is regenerated next time it is needed
                    CurrentFastOrderedAllSent = null;
                }
                else
                {
                    // if the message was empty, we only need to return it
                    Sents.Return(CurrentFastOrderedAllSent);
                    CurrentFastOrderedAllSent = null;
                }
            }

            PlayerInfo pinfo = PlayerInfos.Values[PlayerInfos.IdsToIndices[pinfoindex]];

            if (PlayerAddedCallback != null)
                PlayerAddedCallback(pinfo);

            for (int i = 0; i < ExecutorCount; i++)
                Executors[i].PlayerAdded(pinfo);

            return pid;
        }
        #endregion Add-Remove Players


        // Send Messages
        #region Send Messages
        public Sent BeginNewSend(byte specialid)
        {
            Sent sent = Sents.Request();

            int c = 0;
            sent.Data[c] = OurPlayerId;
            c++;

            // leave an empty for the target player id
            sent.Data[c] = 0; 
            c++;

            // leave an empty for the messageid
            Bytes.WriteUShort(sent.Data, 0, c); 
            c += 2;
            // write the specialid
            sent.Data[c] = specialid;
            c++;

            if (specialid == SpecialOrdered)
            {
                sent.IsImmediate = false;
                // reserve the next 12 bytes for the handshake
                // and the next 2 after that for the orderid
                c += 14;
            }
            else if (specialid != SpecialNormal)
            {
                sent.IsImmediate = true;
            }
            else
            {
                sent.IsImmediate = false;
                // reserve the next 12 bytes for the handshake
                c += 12;
            }

            sent.Length = c;
            return sent;
        }

        public Sent GetReliableAllSend(int requestedLength)
        {
            if (requestedLength > UdpClientExtensions.MaxUdpSize)
                throw new Exception("Packet splitting not supported yet");
            if (CurrentReliableAllSent.Length + requestedLength <= UdpClientExtensions.MaxUdpSize)
                return CurrentReliableAllSent;
            // send the current send
            SendReliableAll(CurrentReliableAllSent);
            // create a new send
            CurrentReliableAllSent = BeginNewSend(SpecialNormal);
            return CurrentReliableAllSent;
        }

        public Sent GetRetryAllSend(int requestedLength)
        {
            if (requestedLength > UdpClientExtensions.MaxUdpSize)
                throw new Exception("Packet splitting not supported yet");
            if (CurrentRetryAllSent.Length + requestedLength <= UdpClientExtensions.MaxUdpSize)
                return CurrentRetryAllSent;
            // send the current send
            SendRetryAll(CurrentRetryAllSent, RetryAllDefault);
            // create a new send
            CurrentRetryAllSent = BeginNewSend(SpecialNormal);
            return CurrentRetryAllSent;
        }

        public Sent GetReliablePlayerSend(byte pid, int requestedLength)
        {
            if (requestedLength > UdpClientExtensions.MaxUdpSize)
                throw new Exception("Packet splitting not supported yet");
            Sent send = CurrentReliablePlayerSent[pid];
            if (send.Length + requestedLength <= UdpClientExtensions.MaxUdpSize)
                return send;
            // send the current send

            SendReliable(send, PlayerInfos.Values[PlayerInfos.IdsToIndices[pid]]);
            // create a new send
            send = BeginNewSend(SpecialNormal);
            CurrentReliablePlayerSent[pid] = send;
            return send;
        }

        // DEPRECIATED:
        // Use FastOrderedAllSend, it is superior in every way
        private Sent GetOrderedAllSend(int requestedLength)
        {
            if (requestedLength > UdpClientExtensions.MaxUdpSize)
                throw new Exception("Packet splitting not supported yet");
            if (CurrentOrderedAllSent.Length + requestedLength <= UdpClientExtensions.MaxUdpSize)
                return CurrentOrderedAllSent;

            // we can only send it immediately if there are no other ordered packets
            // waiting, for ordered packets we only send one at a time
            if (OrderedCount == 0)
                SendReliableAll(CurrentOrderedAllSent);
            // normally, we get mid assigned from sending. But we need it either way
            // so if we don't send, assign a mid now
            else if (!CurrentOrderedAllSent.HasMessageId)
                AssignMessageId(CurrentOrderedAllSent);

            // mark the packet as ordered
            CurrentOrderedAllSent.IsOrdered = true;
            OrderedMids[OrderedCount] = CurrentOrderedAllSent.MessageId;
            // create a new send
            CurrentOrderedAllSent = BeginNewSend(SpecialNormal);
            return CurrentOrderedAllSent;
        }

        public Sent GetFastOrderedAllSend(int requestedLength)
        {
            // note that fast ordered packets return to being null when they are not in use
            // because that way they don't get assigned fastorderids until we're sure something
            // will be written in them
            // so we need to be prepared to check for null

            // whenever we request a new all send, if any player send is not
            // empty, we need to end it and start a new player send
            FastOrderAccessedAll = true;
            if (FastOrderAccessedPlayer)
            {
                FastOrderAccessedPlayer = false;
                Sent psend;
                for (int i = 0; i < CurrentFastOrderedPlayerSent.Length; i++)
                {
                    psend = CurrentFastOrderedPlayerSent[i];
                    if (psend != null && psend.Length > Sent.EmptySizeWithOrderValue)
                    {
                        SendReliable(psend, PlayerInfos.Values[PlayerInfos.IdsToIndices[i]]);
                        // set to null so it is regenerated when needed
                        CurrentFastOrderedPlayerSent[i] = null;
                    }
                }
            }

            if (requestedLength > UdpClientExtensions.MaxUdpSize)
                throw new Exception("Packet splitting not supported yet");
            if (CurrentFastOrderedAllSent != null)
            {
                if (CurrentFastOrderedAllSent.Length + requestedLength <= UdpClientExtensions.MaxUdpSize)
                    return CurrentFastOrderedAllSent;
                // send the current send
                SendFastOrderedAll(CurrentFastOrderedAllSent);
            }

            // create a new send
            CurrentFastOrderedAllSent = BeginNewSend(SpecialOrdered);
            SetupFastOrderedAllSent();
            return CurrentFastOrderedAllSent;
        }

        private void SetupFastOrderedAllSent()
        {
            // important note: sents that are sent to multiple players and are fastorder
            // must be marked this way (IsFastOrdered).
            // for sents that are only to one player, this is unncessary, as the player's
            // fastordervalue can be written directly into the packet
            CurrentFastOrderedAllSent.IsFastOrdered = true;
            // fill in the order values for all players
            byte pid;
            ushort nextvalue;
            for (int i = 0; i < PlayerInfos.Count; i++)
            {
                pid = PlayerInfos.Values[i].PlayerId;
                nextvalue = FastOrderPlayerIndex[pid];
                if (nextvalue == ushort.MaxValue)
                    nextvalue = 0;
                else
                    nextvalue++;
                FastOrderPlayerIndex[pid] = nextvalue;
                CurrentFastOrderedAllSent.AddFastOrderValue(pid, nextvalue);
            }
        }

        public Sent GetFastOrderedPlayerSend(byte pid, int requestedLength)
        {
            // whenever we request a new player send, if the all send is not
            // empty, we need to end it and start a new all send
            FastOrderAccessedPlayer = true;
            if (FastOrderAccessedAll)
            {
                FastOrderAccessedAll = false;
                if (CurrentFastOrderedAllSent != null)
                {
                    if (CurrentFastOrderedAllSent.Length > Sent.EmptySizeWithOrderValue)
                    {
                        // send the current send
                        SendFastOrderedAll(CurrentFastOrderedAllSent);

                        // set to null so it is regenerated next time it is needed
                        CurrentFastOrderedAllSent = null;
                    }
                    else
                    {
                        // if the message was empty, we only need to return it
                        Sents.Return(CurrentFastOrderedAllSent);
                        CurrentFastOrderedAllSent = null;
                    }
                }
            }

            if (requestedLength > UdpClientExtensions.MaxUdpSize)
                throw new Exception("Packet splitting not supported yet");
            Sent send = CurrentFastOrderedPlayerSent[pid];
            if (send != null)
            {
                if (send.Length + requestedLength <= UdpClientExtensions.MaxUdpSize)
                    return send;
                // send the current send

                SendReliable(send, PlayerInfos.Values[PlayerInfos.IdsToIndices[pid]]);
            }
            // create a new send
            send = BeginNewSend(SpecialOrdered);
            SetupFastOrderedPlayerSent(send, pid);
            CurrentFastOrderedPlayerSent[pid] = send;
            return send;
        }

        private void SetupFastOrderedPlayerSent(Sent send, byte pid)
        {
            // we can put in the special order index now, since this only goes
            // to one player and not multiple players.
            // (this is also why we don't need to mark this sent as IsFastOrdered)
            ushort nextvalue = FastOrderPlayerIndex[pid];
            if (nextvalue == ushort.MaxValue)
                nextvalue = 0;
            else
                nextvalue++;
            FastOrderPlayerIndex[pid] = nextvalue;
            Bytes.WriteUShort(send.Data, nextvalue, Sent.OrderValuePosition);
        }

        // stores the handshake within the sent structure
        private void AddHandshakeToSend(Sent sent, byte pid)
        {
            int ackIndex = sent.AddAck(pid);
            byte[] ack = sent.Acks[ackIndex];

            // structure of an Ack:
            // 2 bytes - ushort id of latest message received
            // 8 bytes - bit array, receipt of 64 messages before latest
            //           NOTE: 1 / true bits = missing in this case, false bits = received
            // 2 bytes - ushort id of the next unreceived message after the 64th
            //           ( the implication is that the client may deduce everything
            //             between mid-64 and this value is received)

            // 1. lookup latest message
            int latest = PlayerLastReceivedMessageId[pid];
            if (latest == -1)
            {
                // special case:
                // we have received no messages
                // in this case, we send a special code: all 0s
                for (int i = 0; i < ack.Length; i++)
                    ack[i] = 0;
                return;
            }

            // we have messages, so we can proceed
            byte nack;
            ushort mid = (ushort)latest;
            ushort[] precs = PlayerReceipts[pid];
            ushort nextunused = precs[mid];
            ushort currentmid = mid;
            if (currentmid == 0)
                currentmid = ushort.MaxValue;
            else
                currentmid--;

            // write our latest message
            Bytes.WriteUShort(ack, mid, 0);

            // now start writing the last 64 ack
            for (int i = 0; i < 8; i++)
            {
                nack = 0;
                for (int o = 0; o < 8; o++)
                {
                    if (currentmid == nextunused)
                    {
                        // add a missing bit
                        Bits.AddTrueBit(nack, o);
                        // find the next unused
                        nextunused = precs[currentmid];
                    }

                    if (currentmid == 0)
                        currentmid = ushort.MaxValue;
                    else
                        currentmid--;
                }

                ack[i + 2] = nack;
            }

            // finally write the remaining unused ushort
            Bytes.WriteUShort(ack, nextunused, 10);

            // nothing left to do here, the rest of these todos are for
            // other features:

            // idea for reception storage
            // per player keep a ushort[65535] Receipts[]
            // each index corresponds to a message id
            // each value is the id of the previous unreceived message
            // if a Receipts[id] == id, that means it is unreceived

            // this would be memory inefficient, as for 256 players it'd be 36 mb
            // but it would be highly cpu efficient to iterate over constantly
            // in fact, the ack could simply be:
            // 2 bytes - ushort id of latest message received
            // 10 bytes - 5 ushorts of the previous 5 unreceived messages
            // e.g. 100 | 99, 95, 94, 93, 90
            //      would mean that we just got 100
            //      we're missing 99, we have 98-96, we're missing 95, 94, 93, we have 92-91
            // and this would generate very quickly
            // but in certain cases would contain less info than the bitarray (and incertain, more)
            // we should evaluate what is faster

            // done: see EventIgnore
            // important note: not all clients will confirm all messages, because the MIDs
            // are for all messages and not all messages are for each client.
            // the server must periodically send a message which enumerates
            // the MIDs a client may ignore. (clients do not need to do such since
            // they only communicate with the server, thus all their sents will
            // be accounted for) 

            // done: see heartbeat
            // to ensure that old messages get acknowledged, clients + server should send a longer
            // handshake periodically, e.g. 50 bytes could cover the last 400 messages and be sent
            // every second rather than constantly

            // done: see MarkReceived
            // when we receive a new message from this client
            // we should perform a check:
            // the goal is to reset the received so that we don't think we have
            // already received messages when the client loops around
            //
            // if this is the new latest message we have received 
            //  (that is, it isn't within the circular range (ushort.maxvalue / 2) behind 
            //   the current latest message id)
            // and moving from the old latest to the new latest crosses a 512 message boundary 
            //  (e.g. going from 5630 to 5633 crosses 5632, which % 512 == 0)
            // then look ahead for the next 512 messages and reset their PlayerReceipts to be
            // self-equal, this way we don't think we already have those messages as we approach them.
        }

        private void MarkReceived(byte pid, ushort mid)
        {
            ushort[] precs = PlayerReceipts[pid];
            if (precs[mid] != mid)
                return; // already marked received

            // when a message is received, set Receipts[id] = Receipts[id - 1]
            // and then scroll forward Receipts[id + 1], Receipts[id + 2] etc
            // if they are equal to id, set them equal to Receipts[id - 1]
            // if they are not equal to id, stop scrolling

            ushort midminus = mid;
            if (midminus == 0)
                midminus = ushort.MaxValue;
            else
                midminus--;

            
            ushort midminusvalue = precs[midminus];
            precs[mid] = midminusvalue;

            ushort midnext = mid;
            while(true)
            {
                if (midnext == ushort.MaxValue)
                    midnext = 0;
                else
                    midnext++;

                if (precs[midnext] == mid)
                    precs[midnext] = midminusvalue;
                else
                    break;
            }

            // now check if this is the latest received
            int oldLatest = PlayerLastReceivedMessageId[pid];
            if (oldLatest == -1)
            {
                PlayerLastReceivedMessageId[pid] = mid;
                return; // first message received
            }

            ushort oldLatestMid = (ushort)oldLatest;
            if (oldLatestMid == mid)
                return; // same mid...

            // the mid can only move up if it is within 512 of the latest mid
            // normally the math is very simple here, UNLESS we are in one specific region

            if (oldLatestMid >= ushort.MaxValue - 512)
            {
                if (mid > oldLatestMid || mid < 512)
                {
                    PlayerLastReceivedMessageId[pid] = mid;
                }
                else return;
            }
            else
            {
                if (mid > oldLatestMid && mid < oldLatestMid + 512)
                {
                    PlayerLastReceivedMessageId[pid] = mid;
                }
                else return;
            }

            // if we get down here, it means we updated our last received
            // (note the 'else return;'s)

            // when we update latest mid, there's a particular case we need to handle
            // whenever we cross a 512 boundary, we do some cleanup
            // specifically, we mark the 512 block *ahead* of the one we just
            // moved into as unreceived, ensuring that it is freed up by the time
            // we arrive to it.

            if (mid / 512 != oldLatestMid / 512)
            {
                ushort startpoint = (ushort)(((oldLatestMid / 512) + 1) * 512);
                for (int i = 0; i < 512; i++)
                {
                    precs[startpoint] = startpoint;
                    startpoint++;
                }
            }
        }
            
        public void SendImmediate(Sent sent, PlayerInfo pinfo)
        {
            // sends without any intention of resending
            if (!sent.IsImmediate)
                Abandon("Tried to send non-immediate as immediate");
            sent.Data[Sent.TargetPidPosition] = pinfo.PlayerId;
            Socket.Send(sent.Data, sent.Length, pinfo.EndPoint);
            Sents.Return(sent);
        }

        public void SendImmediate(Sent sent, IPEndPoint endpoint, byte pid)
        {
            // sends without any intention of resending
            if (!sent.IsImmediate)
                Abandon("Tried to send non-immediate as immediate");
            sent.Data[Sent.TargetPidPosition] = pid;
            Socket.Send(sent.Data, sent.Length, endpoint);
            Sents.Return(sent);
        }

        public void SendImmediateAll(Sent sent)
        {
            if (!sent.IsImmediate)
                Abandon("Tried to send non-immediate as immediate");
            for (int i = 0; i < PlayerInfos.Count; i++)
            {
                PlayerInfo pinfo = PlayerInfos.Values[i];
                if (pinfo.Active)
                {
                    sent.Data[Sent.TargetPidPosition] = pinfo.PlayerId;
                    Socket.Send(sent.Data, sent.Length, pinfo.EndPoint);
                    Sents.Return(sent);
                    if (!IsHost)
                        break; // if we're a client, there's only one endpoint to talk to
                }
            }
        }

        public void SendRetry(Sent sent, PlayerInfo pinfo, int maxRetries = 3)
        {
            // sends and retries until handshook or MaxRetries has passed
            if (sent.IsImmediate)
                Abandon("Tried to send immediate as retry");
            if (!pinfo.Active)
                return;
            sent.Retries = maxRetries;
            if (!sent.HasMessageId)
                AssignMessageId(sent);
            sent.AddTarget(pinfo.PlayerId);
            // build ack for player
            AddHandshakeToSend(sent, pinfo.PlayerId);
            sent.LoadAck(pinfo.PlayerId);
            sent.Data[Sent.TargetPidPosition] = pinfo.PlayerId;
            Socket.Send(sent.Data, sent.Length, pinfo.EndPoint);
        }

        public void SendRetryAll(Sent sent, int maxRetries = 3)
        {
            if (sent.IsImmediate)
                Abandon("Tried to send immediate as retry");
            sent.Retries = maxRetries;
            if (!sent.HasMessageId)
                AssignMessageId(sent);
            for (int i = 0; i < PlayerInfos.Count; i++)
            {
                PlayerInfo pinfo = PlayerInfos.Values[i];
                if (pinfo.Active)
                {
                    sent.AddTarget(pinfo.PlayerId);
                    // build ack for player
                    AddHandshakeToSend(sent, pinfo.PlayerId);
                    sent.LoadAck(pinfo.PlayerId);
                    sent.Data[Sent.TargetPidPosition] = pinfo.PlayerId;
                    Socket.Send(sent.Data, sent.Length, pinfo.EndPoint);
                    if (!IsHost)
                        break; // if we're a client, there's only one endpoint to talk to
                }
            }
        }

        public void SendReliable(Sent sent, PlayerInfo pinfo)
        {
            // guarantees arrival eventually, sends until handshook
            if (sent.IsImmediate)
                Abandon("Tried to send immediate as reliable");
            if (!pinfo.Active)
                return;
            sent.Retries = -1;
            if (!sent.HasMessageId)
                AssignMessageId(sent);
            sent.AddTarget(pinfo.PlayerId);
            // build ack for player
            AddHandshakeToSend(sent, pinfo.PlayerId);
            sent.LoadAck(pinfo.PlayerId);
            sent.Data[Sent.TargetPidPosition] = pinfo.PlayerId;
            Socket.Send(sent.Data, sent.Length, pinfo.EndPoint);
        }

        public void SendReliableAll(Sent sent)
        {
            if (sent.IsImmediate)
                Abandon("Tried to send immediate as reliable");
            sent.Retries = -1;
            if (!sent.HasMessageId)
                AssignMessageId(sent);
            for (int i = 0; i < PlayerInfos.Count; i++)
            {
                PlayerInfo pinfo = PlayerInfos.Values[i];
                if (pinfo.Active)
                {
                    sent.AddTarget(pinfo.PlayerId);
                    // build ack for player
                    AddHandshakeToSend(sent, pinfo.PlayerId);
                    sent.LoadAck(pinfo.PlayerId);
                    sent.Data[Sent.TargetPidPosition] = pinfo.PlayerId;
                    Socket.Send(sent.Data, sent.Length, pinfo.EndPoint);
                    if (!IsHost)
                        break; // if we're a client, there's only one endpoint to talk to
                }
            }
        }

        public void SendFastOrderedAll(Sent sent)
        {
            if (sent.IsImmediate)
                Abandon("Tried to send immediate as reliable");
            sent.Retries = -1;
            if (!sent.HasMessageId)
                AssignMessageId(sent);
            for (int i = 0; i < PlayerInfos.Count; i++)
            {
                PlayerInfo pinfo = PlayerInfos.Values[i];
                // only send if we have a valid set fastorder value
                if (pinfo.Active && sent.FastOrderSets[pinfo.PlayerId])
                {
                    sent.AddTarget(pinfo.PlayerId);
                    // build ack for player
                    AddHandshakeToSend(sent, pinfo.PlayerId);
                    sent.LoadAck(pinfo.PlayerId);
                    // put in the orderid for each player
                    sent.LoadFastOrderValue(pinfo.PlayerId);
                    sent.Data[Sent.TargetPidPosition] = pinfo.PlayerId;
                    Socket.Send(sent.Data, sent.Length, pinfo.EndPoint);
                    if (!IsHost)
                        break; // if we're a client, there's only one endpoint to talk to
                }
            }
        }

        private void AssignMessageId(Sent sent)
        {
            sent.HasMessageId = true;
            sent.MessageId = NextMessageId;
            MessageIdToPoolIdMap[NextMessageId] = sent.PoolIndex;

            // clear our receipts
            for (int i = 0; i < OurReceipts.Length; i++)
                OurReceipts[i][NextMessageId] = false;

            // put the mid into the message itself
            Bytes.WriteUShort(sent.Data, NextMessageId, Sent.MidPosition);

            // now find the next mid value
            if (NextMessageId == ushort.MaxValue)
                NextMessageId = 0;
            else
                NextMessageId++;
            while (MessageIdToPoolIdMap[NextMessageId] != -1)
            {
                // if, when creating a new messageid, we discover that a next messageid 
                // is already taken, it means this: a client has yet to
                // confirm a reliable message, for over a whole cycle. 
                // to be safe, we could forcibly disconnect any clients we discover
                // in this situation. However, is it necessary?
                // we will skip that messageid, so the client will only confirm it now
                // if they actually receive it.
                // in a way, things should work out.

                if (NextMessageId == ushort.MaxValue)
                    NextMessageId = 0;
                else
                    NextMessageId++;
                if (NextMessageId == sent.MessageId) // if we loop back around
                    throw new Exception("No more available message ids!");
            }
        }

        private void SendHeartbeat()
        {
            // if we're not connected, don't send heartbeat (unless we're the host)
            if (!IsHost && !ClientConnected)
                return;

            // periodically, we send a heartbeat message.
            // this contains a long ack
            // and potentially in the future could contain
            // other useful diagnostic information (latency or such)

            // the heartbeat is a unique immediate per player
            for (int i = 0; i < PlayerInfos.Count; i++)
            {
                PlayerInfo pinfo = PlayerInfos.Values[i];

                if (!pinfo.Active)
                    continue;

                int latest = PlayerLastReceivedMessageId[pinfo.PlayerId];
                if (latest == -1)
                    continue; // no received messages from this player yet

                Sent send = BeginNewSend(SpecialIsImmediate);

                Bytes.WriteUShort(send.Data, EventHeartbeat, send.Length); send.Length += 2;
                
                ushort[] precs = PlayerReceipts[pinfo.PlayerId];
                ushort mid = precs[(ushort)latest];
                // for the heartbeat, just write the last 100 unreceived message ids
                for (int o = 0; o < 100; o++)
                {
                    Bytes.WriteUShort(send.Data, mid, send.Length);
                    send.Length += 2;
                    // quick explanation of what we're doing here:
                    // the player receipts stores, at each index, the mid of the
                    // next missing message. but, because [mid] is missing,
                    // precs[mid] = mid, so we can't scroll back through the list
                    // simply by taking precs[mid]. Therefore, subtract one from
                    // mid and then search precs to find the next missing message
                    if (mid == 0)
                        mid = ushort.MaxValue;
                    else
                        mid--;
                    mid = precs[mid];
                }

                SendImmediate(send, pinfo);
            }
        }

        #endregion Send Messages

        
    }
}
