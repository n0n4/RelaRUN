using RelaRUN.Messages;
using RelaRUN.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.FlatSnap
{
    public class FlatSnapInputManager
    {
        public NetServer Server;
        public NetExecutorFlatSnap FlatSnap;

        public int FloatCount = 0;
        public int ByteCount = 0;
        public int WindowSize = 0;
        public int MaxInputsPerTime = 0;

        private FlatSnapInput[] AllPlayerInputs;
        private FlatSnapInput[] AllPlayerPrevInputs;

        // clientside
        public FlatSnapInput CurrentClientInput;
        private int CurrentClientInputIndex = 0;
        private byte ClientInputCounter = 0;
        public FlatSnapInput[] ClientInputs;

        // serverside
        public FlatSnapInput BlankInput;
        public FlatSnapInput[][] ServerInputs;

        public FlatSnapInputManager(int windowSize, int maxInputsPerTime, int floatCount, int byteCount)
        {
            FloatCount = floatCount;
            ByteCount = byteCount;
            MaxInputsPerTime = maxInputsPerTime;
            WindowSize = windowSize * maxInputsPerTime;

            BlankInput = new FlatSnapInput(floatCount, byteCount);

            AllPlayerInputs = new FlatSnapInput[4];
            for (int i = 0; i < AllPlayerInputs.Length; i++)
                AllPlayerInputs[i] = BlankInput;

            AllPlayerPrevInputs = new FlatSnapInput[4];
            for (int i = 0; i < AllPlayerPrevInputs.Length; i++)
                AllPlayerPrevInputs[i] = BlankInput;
        }

        public void Loaded(NetServer server, NetExecutorFlatSnap flatSnap)
        {
            Server = server;
            FlatSnap = flatSnap;

            if (!Server.IsHost)
            {
                ClientInputs = new FlatSnapInput[WindowSize];
                FillArray(ClientInputs);
                CurrentClientInput = ClientInputs[0];
            }
            else
            {
                ServerInputs = new FlatSnapInput[4][];
                for (int i = 0; i < ServerInputs.Length; i++)
                {
                    ServerInputs[i] = new FlatSnapInput[WindowSize];
                    FillArray(ServerInputs[i]);
                }
            }
        }

        private void FillArray(FlatSnapInput[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new FlatSnapInput(FloatCount, ByteCount);
            }
        }

        public void Resize(byte pid)
        {
            if (Server.IsHost)
                ResizeServerInputs(pid);

            if (pid >= AllPlayerInputs.Length)
            {
                int newlen = AllPlayerInputs.Length * 2;
                while (newlen < pid)
                    newlen *= 2;

                FlatSnapInput[] old = AllPlayerInputs;
                AllPlayerInputs = new FlatSnapInput[newlen];
                for (int i = 0; i < old.Length; i++)
                    AllPlayerInputs[i] = old[i];

                for (int i = old.Length; i < AllPlayerInputs.Length; i++)
                    AllPlayerInputs[i] = BlankInput;
            }

            if (pid >= AllPlayerPrevInputs.Length)
            {
                int newlen = AllPlayerPrevInputs.Length * 2;
                while (newlen < pid)
                    newlen *= 2;

                FlatSnapInput[] old = AllPlayerPrevInputs;
                AllPlayerPrevInputs = new FlatSnapInput[newlen];
                for (int i = 0; i < old.Length; i++)
                    AllPlayerPrevInputs[i] = old[i];

                for (int i = old.Length; i < AllPlayerPrevInputs.Length; i++)
                    AllPlayerPrevInputs[i] = BlankInput;
            }
        }

        public void ResizeServerInputs(byte pid)
        {
            if (pid < ServerInputs.Length)
                return;

            int newlen = ServerInputs.Length * 2;
            while (newlen < pid)
                newlen *= 2;

            FlatSnapInput[][] old = ServerInputs;
            ServerInputs = new FlatSnapInput[newlen][];
            for (int i = 0; i < old.Length; i++)
                ServerInputs[i] = old[i];

            for (int i = old.Length; i < ServerInputs.Length; i++)
            {
                ServerInputs[i] = new FlatSnapInput[WindowSize];
                FillArray(ServerInputs[i]);
            }
        }

        public void PreTick()
        {
            if (!Server.IsHost)
            {
                ClientInputCounter = 0;
            }
        }

        public void FinishClientInput()
        {
            WriteClientInput();

            CurrentClientInputIndex++;
            if (CurrentClientInputIndex > ClientInputs.Length)
            {
                CurrentClientInputIndex = 0;
            }
            ClientInputCounter++;

            CurrentClientInput = ClientInputs[CurrentClientInputIndex];
        }

        private void WriteClientInput()
        {
            int len = 2 + 4 + 1 + (4 * FloatCount) + ByteCount;
            Sent send = FlatSnap.GetClientInputSent(len);

            send.WriteUShort(FlatSnap.EventInput);
            send.WriteUInt(FlatSnap.ClientTime);
            send.WriteByte(ClientInputCounter);
            for (int i = 0; i < FloatCount; i++)
                send.WriteFloat(CurrentClientInput.Floats[i]);
            for (int i = 0; i < ByteCount; i++)
                send.WriteByte(CurrentClientInput.Bytes[i]);
        }

        public FlatSnapInput GetServerInput(byte pid, int dataIndex, byte slot)
        {
            int index = dataIndex * MaxInputsPerTime;
            FlatSnapInput input = ServerInputs[pid][index];
            byte c = 0;

            if (c == slot)
                return input;
            while (c != slot)
            {
                index++;
                c++;
                if (c >= MaxInputsPerTime)
                {
                    input = null;
                    break;
                }

                input = ServerInputs[pid][index];
            }

            return input;
        }

        public int ReadClientInput(Receipt receipt, int c)
        {
            // todo: needing to do this lookup every time we receive
            // a client input packet could become cumbersome
            // perhaps we could implement some kind of caching or otherwise
            // make this more intelligent
            uint time = Bytes.ReadUInt(receipt.Data, c); c += 4;
            byte slot = receipt.Data[c]; c++;

            // find dataIndex from time
            int dataIndex = -1;
            for (int i = 0; i < FlatSnap.Data.Length; i++)
            {
                if (FlatSnap.Data[i].Time == time)
                {
                    dataIndex = i;
                    break;
                }
            }

            if (dataIndex == -1)
            {
                // read without saving, we don't have this time yet
                c += 4 * FloatCount;
                c += ByteCount;
                return c;
            }

            FlatSnapInput input = GetServerInput(receipt.PlayerId, dataIndex, slot);
            // read into input
            for (int i = 0; i < FloatCount; i++)
            {
                input.Floats[i] = Bytes.ReadFloat(receipt.Data, c); c += 4;
            }

            for (int i = 0; i < ByteCount; i++)
            {
                input.Bytes[i] = receipt.Data[i]; c++;
            }

            return c;
        }

        public FlatSnapInput GetInput(byte pid, int currentDataIndex, float offset, out FlatSnapInput prevInput)
        {
            if (!Server.IsHost)
            {
                // client behavior is straightforward:
                // - we will only have inputs for ourselves, so if we get
                //   inputs for another player, return blanks
                // - otherwise, pull out our corresponding input
                if (pid != Server.OurPlayerId)
                {
                    prevInput = BlankInput;
                    return BlankInput;
                }

                int index = currentDataIndex * MaxInputsPerTime;
                while (offset >= FlatSnap.SimulationRate)
                {
                    index++;
                    if (index > ClientInputs.Length)
                        index = 0;
                    offset -= FlatSnap.SimulationRate;
                }
                int prevIndex = index - 1;
                if (prevIndex < 0)
                    prevIndex = ClientInputs.Length - 1;

                prevInput = ClientInputs[prevIndex];
                return ClientInputs[index];
            }
            else
            {
                // server behavior is only slightly more complex
                // do the same logic as above, but pull from serverinputs based on playerid
                FlatSnapInput[] inputs = ServerInputs[pid];
                int index = currentDataIndex * MaxInputsPerTime;
                while (offset >= FlatSnap.SimulationRate)
                {
                    index++;
                    if (index > inputs.Length)
                        index = 0;
                    offset -= FlatSnap.SimulationRate;
                }
                int prevIndex = index - 1;
                if (prevIndex < 0)
                    prevIndex = inputs.Length - 1;

                prevInput = inputs[prevIndex];
                return inputs[index];
            }
        }

        public FlatSnapInput[] GetInputsForAllPlayers(int currentDataIndex, float offset, out FlatSnapInput[] prevInputs)
        {
            for (byte i = 0; i < AllPlayerInputs.Length; i++)
            {
                FlatSnapInput input = GetInput(i, currentDataIndex, offset, out FlatSnapInput prevInput);
                AllPlayerInputs[i] = input;
                AllPlayerPrevInputs[i] = prevInput;
            }

            prevInputs = AllPlayerPrevInputs;
            return AllPlayerInputs;
        }
    }
}
