using RelaRUN.Messages;
using RelaRUN.Sockets;
using RelaRUN.Utilities;
using System;

namespace RelaRUN.FlatSnap
{
    public class NetExecutorFlatSnap : INetExecutor
    {
        public IFlatSnapSimulator Simulator;

        public int PreviousDataIndex;
        public int CurrentDataIndex = 0;
        public FlatSnapData[] Data;
        public int WindowSize;
        public int MaxEntities;
        public int FloatsPerEntity;
        public int BytesPerEntity;
        public int UShortsPerEntity;
        public int IntsPerEntity;

        public int NonNetFloatsPerEntity;
        public int NonNetBytesPerEntity;
        public int NonNetUShortsPerEntity;
        public int NonNetIntsPerEntity;

        public bool[] EntityInUse;
        public uint[] EntitySpawn;
        public uint[] EntityDeath;
        public EntityInfo EntityInfo;

        // client data for predicting the future
        public FlatSnapData ClientPredictedData;

        public int HighestEntityId = 0;
        public uint CurrentTime = 0;
        public float TickRate = 30;
        public float SimulationRate = 10;
        public float PropagationRate = 30; // rate of nonnet simulation
        public float AccumulatedTime = 0;
        // client data for keeping track of where the server is at
        // vs where they are at
        public uint ClientTime = 0;
        public float ClientTickOffset = 100;

        private uint LastTimestampReceipt = uint.MaxValue;
        private FlatSnapData LastReceiptData = null;
        private bool GotFirstTimestamp = false;

        public NetServer Server;
        public FlatSnapInputManager InputManager;

        public ushort EventPacketHeader = 0;
        public ushort EventFloatPacket = 1;
        public ushort EventBytePacket = 2;
        public ushort EventUShortPacket = 3;
        public ushort EventIntPacket = 4;
        public ushort EventSetup = 5;
        public ushort EventInput = 6;
        public ushort EventEntitySpawn = 7;
        public ushort EventEntityDeath = 8;
        public ushort EventEntityReminder = 9;

        public const int PacketHeaderLength = 4;
        public const int SentSizeWithHeader = Sent.EmptySizeWithAck + PacketHeaderLength;

        public int SentMaxRetries = 3;
        public float SentRetryTimer = 30;

        // the entity reminder is a packet that gets sent every tick
        // informing clients about the spawn/death times of current
        // entities. This is a fail safe incase all instances of an
        // entity spawn/death message fail to arrive for the client
        // this will eventually ensure parity and resync
        // the higher EntityRemindersPerTick is, the faster resync will
        // occur, but this comes at a cost of sending more data per tick
        // recommendation: increase EntityRemindersPerTick when you have
        // a higher entity count
        private int EntityReminderIndex = 0;
        public int EntityRemindersPerTick = 1;

        private int FloatReminderIndex = 0;
        private int ByteReminderIndex = 0;
        private int UShortReminderIndex = 0;
        private int IntReminderIndex = 0;

        private Sent DataSent;
        private Sent ClientInputSent;

        public NetExecutorFlatSnap(IFlatSnapSimulator simulator, int windowSize,
            int maxEntities, int floats, int bytes, int ushorts, int ints,
            int nonNetFloats, int nonNetBytes, int nonNetUShorts, int nonNetInts,
            FlatSnapInputManager inputManager)
        {
            Simulator = simulator;
            InputManager = inputManager;

            WindowSize = windowSize;
            MaxEntities = maxEntities;
            FloatsPerEntity = floats;
            BytesPerEntity = bytes;
            UShortsPerEntity = ushorts;
            IntsPerEntity = ints;
            NonNetFloatsPerEntity = nonNetFloats;
            NonNetBytesPerEntity = nonNetBytes;
            NonNetUShortsPerEntity = nonNetUShorts;
            NonNetIntsPerEntity = nonNetInts;

            EntityInUse = new bool[MaxEntities]; 
            EntitySpawn = new uint[MaxEntities];
            EntityDeath = new uint[MaxEntities];
            EntityInfo.InUse = EntityInUse;
            EntityInfo.Spawn = EntitySpawn;
            EntityInfo.Death = EntityDeath;

            Data = new FlatSnapData[windowSize * 2];
            for (int i = 0; i < Data.Length; i++)
                Data[i] = new FlatSnapData(i, (uint)i, maxEntities, floats, bytes, ushorts, ints,
                    nonNetFloats, nonNetBytes, nonNetUShorts, nonNetInts);
            
            ClientPredictedData = new FlatSnapData(0, 0, maxEntities, floats, bytes, ushorts, ints,
                    nonNetFloats, nonNetBytes, nonNetUShorts, nonNetInts);

            PreviousDataIndex = (windowSize * 2) - 1;
        }

        public void Resize(int maxEntities)
        {
            MaxEntities = maxEntities;

            for (int i = 0; i < Data.Length; i++)
                Data[i].Resize(maxEntities);

            ClientPredictedData.Resize(maxEntities);
        }

        public void Ascend()
        {
            PreviousDataIndex = CurrentDataIndex;
            CurrentDataIndex++;
            if (CurrentDataIndex >= Data.Length)
                CurrentDataIndex = 0;

            CurrentTime++;
            Data[CurrentDataIndex].Time = CurrentTime;
            Data[PreviousDataIndex].Copy(Data[CurrentDataIndex], HighestEntityId);

            // setup the next few datas so we can receive client input properly
            float delta = ClientTickOffset * 2;
            int index = CurrentDataIndex;
            uint addTime = CurrentTime;
            while (delta > 0)
            {
                index++;
                addTime++;
                Data[index].Time = addTime;
                delta -= TickRate;
            }

            if (Server.IsHost)
            {
                // simulate
                float time = TickRate;
                float totalOffset = 0;
                while (time >= SimulationRate)
                {
                    time -= SimulationRate;
                    Simulate(Data[CurrentDataIndex], SimulationRate, totalOffset);
                    totalOffset += SimulationRate;
                }
                if (time > 0)
                    Simulate(Data[CurrentDataIndex], time, totalOffset);

                // pack our data
                for (int i = 0; i < EntityRemindersPerTick; i++)
                    PackEntityReminder();
                PackBytes();
                PackInts();
                PackUShorts();
                PackFloats();

                FloatReminderIndex++;
                if (FloatReminderIndex * 8 > FloatsPerEntity * HighestEntityId)
                    FloatReminderIndex = 0;

                IntReminderIndex++;
                if (IntReminderIndex * 8 > IntsPerEntity * HighestEntityId)
                    IntReminderIndex = 0;

                UShortReminderIndex++;
                if (UShortReminderIndex * 8 > UShortsPerEntity * HighestEntityId)
                    UShortReminderIndex = 0;

                ByteReminderIndex++;
                if (ByteReminderIndex * 8 > BytesPerEntity * HighestEntityId)
                    ByteReminderIndex = 0;

                // only send the packet if it's not empty
                if (DataSent.Length > SentSizeWithHeader)
                {
                    Server.SendRetryAll(DataSent, SentMaxRetries, SentRetryTimer);
                } 
                else
                {
                    // we're not going to use this sent, so just return it
                    Server.ReturnUnusedSent(DataSent);
                }

                DataSent = BeginNewSend();
            }
            else
            {
                ClientTime = GetClientTime();
            }
        }

        public void ClientPredict()
        {
            // scroll backwards according to the client offset
            int index = CurrentDataIndex;
            float backtime = ClientTickOffset - AccumulatedTime;
            while (backtime > 0)
            {
                index--;
                if (index < 0)
                    index = Data.Length - 1;
                backtime -= TickRate;
            }
            // now backtime should be 0 or negative
            // if it's negative, multiply by -1 and add it to the time to scroll forward
            float time = ClientTickOffset + AccumulatedTime;
            if (backtime < 0)
                time += backtime * -1;

            // copy data into our client prediction data
            FlatSnapData baseData = Data[index];
            if (baseData != null)
                baseData.Copy(ClientPredictedData, HighestEntityId);

            // simulate from server time to our time
            float totalOffset = 0;
            while (time >= TickRate)
            {
                // rewriting here
            }

            while (time >= SimulationRate)
            {
                time -= SimulationRate;
                Simulate(ClientPredictedData, SimulationRate, totalOffset);
                todo; // copy nonnet data back??
                // no, that won't work...
                // theory: do this simulation, tracking the ticks
                // e.g. simulate as if we were doing it by tick
                // this will also make it more consistent with the server
                // then, if we have a data for that tick, use the non-net arrays 
                // from that data (DONT do copying! it'd be slow!)
                // instead just give the clientpredicteddata those arrays
                // temporarily
                totalOffset += SimulationRate;
            }
            if (time > 0)
                Simulate(ClientPredictedData, time, totalOffset);
        }

        public uint GetClientTime()
        {
            uint time = CurrentTime;
            float delta = ClientTickOffset;
            while (delta > TickRate)
            {
                time++;
                delta -= TickRate;
            }
            return time;
        }

        public void Simulate(FlatSnapData data, float delta, float totalOffset)
        {
            FlatSnapInput[] inputs = InputManager.GetInputsForAllPlayers(CurrentDataIndex, totalOffset, out FlatSnapInput[] prevInputs);
            Simulator.Simulate(data, delta, HighestEntityId, EntityInfo, inputs, prevInputs);
        }

        public Sent GetDataSent(int requestedLength)
        {
            if (requestedLength > UdpClientExtensions.MaxUdpSize)
                throw new Exception("Packet splitting not supported yet");

            Sent send = DataSent;
            if (send.Length + requestedLength <= UdpClientExtensions.MaxUdpSize)
                return send;
            // send the current send
            Server.SendRetryAll(send, SentMaxRetries, SentRetryTimer);

            // create a new send
            send = BeginNewSend();
            DataSent = send;
            return send;
        }

        public Sent GetClientInputSent(int requestedLength)
        {
            if (requestedLength > UdpClientExtensions.MaxUdpSize)
                throw new Exception("Packet splitting not supported yet");

            Sent send = ClientInputSent;
            if (send.Length + requestedLength <= UdpClientExtensions.MaxUdpSize)
                return send;
            // send the current send
            Server.SendRetryAll(send, SentMaxRetries, SentRetryTimer);

            // create a new send
            send = Server.BeginNewSend(NetServer.SpecialNormal);
            ClientInputSent = send;
            return send;
        }

        private Sent BeginNewSend()
        {
            Sent send = Server.BeginNewSend(NetServer.SpecialNormal);

            // when we form a new packet, append the packet header automatically
            send.WriteUShort(EventPacketHeader);
            send.WriteUInt(CurrentTime);

            return send;
        }

        private void SendSetup(byte pid)
        {
            Sent send = Server.GetReliablePlayerSend(pid, 14);
            send.WriteUShort(EventSetup);
            send.WriteUInt(CurrentTime);
            send.WriteFloat(TickRate);
            send.WriteFloat(SimulationRate);
        }

        public int GetNextEntityId()
        {
            for (int i = 0; i < MaxEntities; i++)
            {
                if (EntityInUse[i] == false)
                    return i;
            }
            return -1;
        }

        public void SpawnEntity(int id)
        {
            EntityInUse[id] = true;
            EntitySpawn[id] = CurrentTime;
            EntityDeath[id] = uint.MaxValue;

            if (id >= HighestEntityId)
                HighestEntityId = id + 1;

            Sent sent = GetDataSent(2 + 4 + (FloatsPerEntity * 4) + (UShortsPerEntity * 2) + (IntsPerEntity * 4) + BytesPerEntity);
            sent.WriteUShort(EventEntitySpawn);
            sent.WriteInt(id);

            FlatSnapData data = Data[CurrentDataIndex];
            for (int i = 0; i < FloatsPerEntity; i++)
                sent.WriteFloat(data.Floats[(id * FloatsPerEntity) + i]);

            for (int i = 0; i < UShortsPerEntity; i++)
                sent.WriteUShort(data.UShorts[(id * UShortsPerEntity) + i]);

            for (int i = 0; i < IntsPerEntity; i++)
                sent.WriteInt(data.Ints[(id * IntsPerEntity) + i]);

            for (int i = 0; i < BytesPerEntity; i++)
                sent.WriteByte(data.Bytes[(id * BytesPerEntity) + i]);
        }

        public void DestroyEntity(int id)
        {
            EntityDeath[id] = CurrentTime;

            Sent sent = GetDataSent(2 + 4);
            sent.WriteUShort(EventEntityDeath);
            sent.WriteInt(id);
        }

        private void PropagateNonNetData(int id, int startingIndex)
        {
            FlatSnapData startingData = Data[startingIndex];
            uint time = startingData.Time;
            int index = startingIndex + 1;
            if (index > Data.Length)
                index = 0;
            FlatSnapData prevData = startingData;
            while (index != startingIndex)
            {
                FlatSnapData data = Data[index];
                if (data.Time < time)
                    break;

                // propagate
                prevData.CopyEntityNonNet(data, id);
                float delta = 0;
                while (delta < TickRate) 
                {
                    FlatSnapInput[] inputs = InputManager.GetInputsForAllPlayers(index, delta, out FlatSnapInput[] prevInputs);
                    Simulator.PropagateNonNet(data, PropagationRate, id, HighestEntityId, EntityInfo, inputs, prevInputs);
                    delta += PropagationRate;
                }
                prevData = data;
            }
        }
        


        // Interface Methods
        public ushort Register(NetServer server, ushort startIndex)
        {
            Server = server;

            EventPacketHeader += startIndex;
            EventFloatPacket += startIndex;
            EventBytePacket += startIndex;
            EventUShortPacket += startIndex;
            EventIntPacket += startIndex;
            EventSetup += startIndex;
            EventInput += startIndex;
            EventEntitySpawn += startIndex;
            EventEntityDeath += startIndex;
            EventEntityReminder += startIndex;

            InputManager.Loaded(server, this);

            if (Server.IsHost)
            {
                // do host setup
                DataSent = BeginNewSend();
            }
            else
            {
                ClientInputSent = Server.BeginNewSend(NetServer.SpecialNormal);
            }

            return 10;
        }

        public int Receive(Receipt receipt, PlayerInfo pinfo, ushort eventid, int c)
        {
            if (Server.IsHost)
            {
                if (eventid == EventInput)
                {
                    return InputManager.ReadClientInput(receipt, c);
                }
                // if it's unrecognized, skip the packet
                return receipt.Length + 1;
            }

            // client receipts
            if (eventid == EventSetup)
            {
                uint timestamp = Bytes.ReadUInt(receipt.Data, c); c += 4;
                // reset our data
                CurrentDataIndex = 0;
                CurrentTime = timestamp;
                TickRate = Bytes.ReadFloat(receipt.Data, c); c += 4;
                SimulationRate = Bytes.ReadFloat(receipt.Data, c); c += 4;
                Data[0].Time = timestamp;
                Data[0].Clear();
            }
            else if (eventid == EventPacketHeader)
            {
                uint oldTimestamp = LastTimestampReceipt;
                LastTimestampReceipt = Bytes.ReadUInt(receipt.Data, c); c += 4;
                
                if (oldTimestamp != LastTimestampReceipt)
                {
                    // if we haven't received any timestamps yet, accept the first one we see as baseline
                    if (!GotFirstTimestamp)
                    {
                        Data[CurrentDataIndex].Time = LastTimestampReceipt;
                        GotFirstTimestamp = true;
                    }

                    bool found = false;
                    for (int i = 0; i < Data.Length; i++)
                    {
                        if (Data[i].Time == LastTimestampReceipt)
                        {
                            LastReceiptData = Data[i];
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // we didn't find the next timestamp, so skip these packets
                        LastReceiptData = null;
                    }
                }
            }
            else if (eventid == EventFloatPacket)
            {
                c = ReadFloats(receipt, c);
            }
            else if (eventid == EventIntPacket)
            {
                c = ReadInts(receipt, c);
            }
            else if (eventid == EventUShortPacket)
            {
                c = ReadUShorts(receipt, c);
            }
            else if (eventid == EventBytePacket)
            {
                c = ReadBytes(receipt, c);
            }
            else if (eventid == EventEntitySpawn)
            {
                int id = Bytes.ReadInt(receipt.Data, c); c += 4;
                if (id >= HighestEntityId)
                    HighestEntityId = id + 1;
                EntitySpawn[id] = LastTimestampReceipt;

                FlatSnapData data = LastReceiptData;
                // fl, us, in, by
                for (int i = 0; i < FloatsPerEntity; i++)
                {
                    data.Floats[(id * FloatsPerEntity) + i] = Bytes.ReadFloat(receipt.Data, c); c += 4;
                }

                for (int i = 0; i < UShortsPerEntity; i++)
                {
                    data.UShorts[(id * UShortsPerEntity) + i] = Bytes.ReadUShort(receipt.Data, c); c += 2;
                }

                for (int i = 0; i < IntsPerEntity; i++)
                {
                    data.Ints[(id * IntsPerEntity) + i] = Bytes.ReadInt(receipt.Data, c); c += 4;
                }

                for (int i = 0; i < BytesPerEntity; i++)
                {
                    data.Bytes[(id * BytesPerEntity) + i] = receipt.Data[c]; c++;
                }

                if (!EntityInUse[id])
                {
                    Simulator.ClientSpawn(data, id);
                    EntityInUse[id] = true;

                    PropagateNonNetData(id, data.Index);
                }

                return c;
            }
            else if (eventid == EventEntityDeath)
            {
                int id = Bytes.ReadInt(receipt.Data, c); c += 4;
                EntityDeath[id] = LastTimestampReceipt;

                Simulator.ClientDeath(LastReceiptData, id);
                return c;
            }
            else if (eventid == EventEntityReminder)
            {
                int index = Bytes.ReadInt(receipt.Data, c); c += 4;
                byte bitmask = receipt.Data[c]; c++;

                for (int i = 0; i < 8; i++)
                {
                    if (Bits.CheckBit(bitmask, i))
                    {
                        uint oldSpawn = EntitySpawn[index + i];
                        uint oldDeath = EntityDeath[index + i];
                        EntitySpawn[index + i] = Bytes.ReadUInt(receipt.Data, c); c += 4;
                        EntityDeath[index + i] = Bytes.ReadUInt(receipt.Data, c); c += 4;

                        if (!EntityInUse[index + i] || oldSpawn != EntitySpawn[index + i])
                        {
                            EntityInUse[index + i] = true;
                            Simulator.ClientSpawn(LastReceiptData, index + i);
                        }

                        if (oldDeath != EntityDeath[index + i])
                            Simulator.ClientDeath(LastReceiptData, index + i);
                    }
                }

                return c;
            }
            else
            {
                // if it's unrecognized, skip the packet
                return receipt.Length + 1;
            }


            return c;
        }

        public void PreTick(float elapsedMS)
        {
            AccumulatedTime += elapsedMS;

            // clear up unused entities
            if (CurrentTime > WindowSize && HighestEntityId > 0) 
            {
                for (int i = HighestEntityId - 1; i >= 0; i--)
                {
                    if (!EntityInUse[i])
                        continue;

                    if (EntityDeath[i] < CurrentTime - WindowSize)
                    {
                        EntityInUse[i] = false;
                        if (HighestEntityId == i + 1)
                            HighestEntityId = i;
                    }
                }
            }

            InputManager.PreTick();

            if (Server.IsHost)
            {
                while (AccumulatedTime >= TickRate)
                {
                    AccumulatedTime -= TickRate;
                    Ascend();
                }
            }
            else
            {
                while (AccumulatedTime >= TickRate)
                    AccumulatedTime -= TickRate;

                ClientPredict();
            }
        }

        public void PostTick(float elapsedMS)
        {
            if (!Server.IsHost)
            {
                // send client input packets
                if (ClientInputSent.Length > Sent.EmptySizeWithAck)
                {
                    Server.SendRetryAll(ClientInputSent, SentMaxRetries, SentRetryTimer);
                    ClientInputSent = Server.BeginNewSend(NetServer.SpecialNormal);
                }
            }
        }

        public void PlayerAdded(PlayerInfo pinfo)
        {
            InputManager.Resize(pinfo.PlayerId);

            if (Server.IsHost)
            {
                // send setup
                SendSetup(pinfo.PlayerId);

                return;
            }
        }

        public void PlayerRemoved(PlayerInfo pinfo)
        {

        }

        public void ClientConnected()
        {

        }



        // Packing Methods
        public void PackEntityReminder()
        {
            int len = 7;
            byte bitmask = 0;
            for (int i = 0; i < 8; i++)
            {
                int index = i + EntityReminderIndex;
                if (index >= MaxEntities)
                    break;

                if (EntityInUse[index])
                {
                    bitmask = Bits.AddTrueBit(bitmask, i);
                    len += 8;
                }
            }

            Sent sent = GetDataSent(len);
            sent.WriteUShort(EventEntityReminder);
            sent.WriteInt(EntityReminderIndex);
            sent.WriteByte(bitmask);

            for (int i = 0; i < 8; i++)
            {
                int index = i + EntityReminderIndex;
                if (index >= MaxEntities)
                    break;

                if (EntityInUse[index])
                {
                    sent.WriteUInt(EntitySpawn[index]);
                    sent.WriteUInt(EntityDeath[index]);
                } 
            }

            EntityReminderIndex += 8;
            if (EntityReminderIndex >= MaxEntities)
                EntityReminderIndex = 0;
        }

        public void PackFloats()
        {
            // format:
            // [int - starting point]
            // [byte - 8xbitmask]
            // [byte - bitmask][floats depending on bitmask] repeated (up to 8 times)

            FlatSnapData data = Data[CurrentDataIndex];
            FlatSnapData oldData = Data[PreviousDataIndex];

            int index = 0;
            while (index < HighestEntityId * FloatsPerEntity)
            {
                // must have at least enough length for one group of 8
                int len = 2 + 1 + 4 + 1 + 32;

                Sent sent = GetDataSent(len);
                sent.WriteUShort(EventFloatPacket);

                sent.WriteInt(index);
                int majorBitmaskSpot = sent.Length;
                sent.WriteByte(0);
                byte major = 0;

                for (int i = 0; i < 8; i++)
                {
                    // check if we have enough space to write another group
                    if (sent.Length + 33 >= UdpClientExtensions.MaxUdpSize)
                    {
                        // if not, break out
                        break;
                    }

                    if (index >= HighestEntityId * FloatsPerEntity)
                    {
                        // also break if we're already done writing all entities
                        break;
                    }

                    int minorBitmaskSpot = sent.Length;
                    sent.WriteByte(0);
                    byte minor = 0;

                    bool written = false;
                    for (int o = 0; o < 8; o++)
                    {
                        float val = data.Floats[index];
                        if (val != oldData.Floats[index] || FloatReminderIndex == index/8)
                        {
                            written = true;
                            minor = Bits.AddTrueBit(minor, o);
                            sent.WriteFloat(val);
                        }
                        index++;
                    }

                    if (written)
                    {
                        major = Bits.AddTrueBit(major, i);
                        sent.Data[minorBitmaskSpot] = minor;
                    }
                    else
                    {
                        // undo the minor bitmask
                        sent.Length--;
                    }
                }

                sent.Data[majorBitmaskSpot] = major;
            }
        }

        public int ReadFloats(Receipt receipt, int c)
        {
            FlatSnapData data = LastReceiptData;

            // read the starting int
            int start = Bytes.ReadInt(receipt.Data, c); c += 4;

            // read the major bitmask
            byte major = receipt.Data[c]; c++;

            int index = start;
            for (int i = 0; i < 8; i++)
            {
                if (!Bits.CheckBit(major, i))
                {
                    index += 8;
                    continue;
                }

                byte minor = receipt.Data[c]; c++;
                for (int o = 0; o < 8; o++)
                {
                    if (!Bits.CheckBit(minor, o))
                    {
                        index++;
                        continue;
                    }

                    float val = Bytes.ReadFloat(receipt.Data, c); c += 4;
                    data.Floats[index] = val;
                    index++;
                }
            }

            return c;
        }

        public void PackInts()
        {
            // format:
            // [int - starting point]
            // [byte - 8xbitmask]
            // [byte - bitmask][ints depending on bitmask] repeated (up to 8 times)

            FlatSnapData data = Data[CurrentDataIndex];
            FlatSnapData oldData = Data[PreviousDataIndex];

            int index = 0;
            while (index < HighestEntityId * IntsPerEntity)
            {
                // must have at least enough length for one group of 8
                int len = 2 + 1 + 4 + 1 + 32;

                Sent sent = GetDataSent(len);
                sent.WriteUShort(EventIntPacket);

                sent.WriteInt(index);
                int majorBitmaskSpot = sent.Length;
                sent.WriteByte(0);
                byte major = 0;

                for (int i = 0; i < 8; i++)
                {
                    // check if we have enough space to write another group
                    if (sent.Length + 33 >= UdpClientExtensions.MaxUdpSize)
                    {
                        // if not, break out
                        break;
                    }

                    if (index >= HighestEntityId * IntsPerEntity)
                    {
                        // also break if we're already done writing all entities
                        break;
                    }

                    int minorBitmaskSpot = sent.Length;
                    sent.WriteByte(0);
                    byte minor = 0;

                    bool written = false;
                    for (int o = 0; o < 8; o++)
                    {
                        int val = data.Ints[index];
                        if (val != oldData.Ints[index] || IntReminderIndex == index / 8)
                        {
                            written = true;
                            minor = Bits.AddTrueBit(minor, o);
                            sent.WriteInt(val);
                        }
                        index++;
                    }

                    if (written)
                    {
                        major = Bits.AddTrueBit(major, i);
                        sent.Data[minorBitmaskSpot] = minor;
                    }
                    else
                    {
                        // undo the minor bitmask
                        sent.Length--;
                    }
                }

                sent.Data[majorBitmaskSpot] = major;
            }
        }

        public int ReadInts(Receipt receipt, int c)
        {
            FlatSnapData data = LastReceiptData;

            // read the starting int
            int start = Bytes.ReadInt(receipt.Data, c); c += 4;

            // read the major bitmask
            byte major = receipt.Data[c]; c++;

            int index = start;
            for (int i = 0; i < 8; i++)
            {
                if (!Bits.CheckBit(major, i))
                {
                    index += 8;
                    continue;
                }

                byte minor = receipt.Data[c]; c++;
                for (int o = 0; o < 8; o++)
                {
                    if (!Bits.CheckBit(minor, o))
                    {
                        index++;
                        continue;
                    }

                    int val = Bytes.ReadInt(receipt.Data, c); c += 4;
                    data.Ints[index] = val;
                    index++;
                }
            }

            return c;
        }

        public void PackUShorts()
        {
            // format:
            // [int - starting point]
            // [byte - 8xbitmask]
            // [byte - bitmask][ushorts depending on bitmask] repeated (up to 8 times)

            FlatSnapData data = Data[CurrentDataIndex];
            FlatSnapData oldData = Data[PreviousDataIndex];

            int index = 0;
            while (index < HighestEntityId * UShortsPerEntity)
            {
                // must have at least enough length for one group of 8
                int len = 2 + 1 + 4 + 1 + 16;

                Sent sent = GetDataSent(len);
                sent.WriteUShort(EventUShortPacket);

                sent.WriteInt(index);
                int majorBitmaskSpot = sent.Length;
                sent.WriteByte(0);
                byte major = 0;

                for (int i = 0; i < 8; i++)
                {
                    // check if we have enough space to write another group
                    if (sent.Length + 17 >= UdpClientExtensions.MaxUdpSize)
                    {
                        // if not, break out
                        break;
                    }

                    if (index >= HighestEntityId * UShortsPerEntity)
                    {
                        // also break if we're already done writing all entities
                        break;
                    }

                    int minorBitmaskSpot = sent.Length;
                    sent.WriteByte(0);
                    byte minor = 0;

                    bool written = false;
                    for (int o = 0; o < 8; o++)
                    {
                        ushort val = data.UShorts[index];
                        if (val != oldData.UShorts[index] || UShortReminderIndex == index / 8)
                        {
                            written = true;
                            minor = Bits.AddTrueBit(minor, o);
                            sent.WriteUShort(val);
                        }
                        index++;
                    }

                    if (written)
                    {
                        major = Bits.AddTrueBit(major, i);
                        sent.Data[minorBitmaskSpot] = minor;
                    }
                    else
                    {
                        // undo the minor bitmask
                        sent.Length--;
                    }
                }

                sent.Data[majorBitmaskSpot] = major;
            }
        }

        public int ReadUShorts(Receipt receipt, int c)
        {
            FlatSnapData data = LastReceiptData;

            // read the starting int
            int start = Bytes.ReadUShort(receipt.Data, c); c += 2;

            // read the major bitmask
            byte major = receipt.Data[c]; c++;

            int index = start;
            for (int i = 0; i < 8; i++)
            {
                if (!Bits.CheckBit(major, i))
                {
                    index += 8;
                    continue;
                }

                byte minor = receipt.Data[c]; c++;
                for (int o = 0; o < 8; o++)
                {
                    if (!Bits.CheckBit(minor, o))
                    {
                        index++;
                        continue;
                    }

                    ushort val = Bytes.ReadUShort(receipt.Data, c); c += 2;
                    data.UShorts[index] = val;
                    index++;
                }
            }

            return c;
        }

        public void PackBytes()
        {
            // format:
            // [int - starting point]
            // [byte - 8xbitmask]
            // [byte - bitmask][bytes depending on bitmask] repeated (up to 8 times)

            FlatSnapData data = Data[CurrentDataIndex];
            FlatSnapData oldData = Data[PreviousDataIndex];

            int index = 0;
            while (index < HighestEntityId * BytesPerEntity)
            {
                // must have at least enough length for one group of 8
                int len = 2 + 1 + 4 + 1 + 8;

                Sent sent = GetDataSent(len);
                sent.WriteUShort(EventBytePacket);

                sent.WriteInt(index);
                int majorBitmaskSpot = sent.Length;
                sent.WriteByte(0);
                byte major = 0;

                for (int i = 0; i < 8; i++)
                {
                    // check if we have enough space to write another group
                    if (sent.Length + 9 >= UdpClientExtensions.MaxUdpSize)
                    {
                        // if not, break out
                        break;
                    }

                    if (index >= HighestEntityId * BytesPerEntity)
                    {
                        // also break if we're already done writing all entities
                        break;
                    }

                    int minorBitmaskSpot = sent.Length;
                    sent.WriteByte(0);
                    byte minor = 0;

                    bool written = false;
                    for (int o = 0; o < 8; o++)
                    {
                        byte val = data.Bytes[index];
                        if (val != oldData.Bytes[index] || ByteReminderIndex == index / 8)
                        {
                            written = true;
                            minor = Bits.AddTrueBit(minor, o);
                            sent.WriteByte(val);
                        }
                        index++;
                    }

                    if (written)
                    {
                        major = Bits.AddTrueBit(major, i);
                        sent.Data[minorBitmaskSpot] = minor;
                    }
                    else
                    {
                        // undo the minor bitmask
                        sent.Length--;
                    }
                }

                sent.Data[majorBitmaskSpot] = major;
            }
        }

        public int ReadBytes(Receipt receipt, int c)
        {
            FlatSnapData data = LastReceiptData;

            // read the starting int
            int start = Bytes.ReadInt(receipt.Data, c); c += 4;

            // read the major bitmask
            byte major = receipt.Data[c]; c++;

            int index = start;
            for (int i = 0; i < 8; i++)
            {
                if (!Bits.CheckBit(major, i))
                {
                    index += 8;
                    continue;
                }

                byte minor = receipt.Data[c]; c++;
                for (int o = 0; o < 8; o++)
                {
                    if (!Bits.CheckBit(minor, o))
                    {
                        index++;
                        continue;
                    }

                    byte val = receipt.Data[c]; c++;
                    data.Bytes[index] = val;
                    index++;
                }
            }

            return c;
        }
    }
}
