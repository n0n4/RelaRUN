using System;
using System.Collections;
using RelaNet.Messages;
using RelaNet.Utilities;

namespace RelaNet.Basics
{
    public class NetExecutorChat : INetExecutor
    {
        public ushort EventChatMessage = 0;


        public NetServer Server;
        public Action<PlayerInfo, string> ChatCallback;
        public Func<string, string> FilterFunc;

        private byte SendKey = 0;
        private BitArray[] ReceiveKey = new BitArray[2];

        public NetExecutorChat(Action<PlayerInfo, string> chatCallback,
            Func<string, string> filterFunc)
        {
            ChatCallback = chatCallback;
            FilterFunc = filterFunc;

            for (int i = 0; i < ReceiveKey.Length; i++)
                ReceiveKey[i] = new BitArray(256);
        }

        public void PostTick(float elapsedMS)
        {
            // nothing doing
        }

        public void PreTick(float elapsedMS)
        {
            // nothing doing
        }

        public int Receive(Receipt receipt, PlayerInfo pinfo, ushort eventid, int c)
        {
            if (eventid == EventChatMessage)
            {
                if (Server.IsHost)
                {
                    byte recKey = receipt.Data[c]; c++;
                    // if we're the host, our job is to send chat messages to other players
                    // read the string the client sent
                    string msg = Bytes.ReadString(receipt.Data, c, out int len); c += len;

                    // before we go any further, check the recKey to see if this is a duplicate
                    // if our ReceiveKey is already true for this id, we already got the message
                    if (ReceiveKey[pinfo.PlayerId][recKey])
                        return c;
                    // otherwise, we need to fill in our receivekey now
                    ReceiveKey[pinfo.PlayerId][recKey] = true;
                    // and, when we do this, we free up the receive key that is 128 spaces from this one
                    // to ensure future messages can be received
                    byte nextKey = recKey;
                    if (nextKey < byte.MaxValue - 128)
                        nextKey += 128;
                    else
                        nextKey = (byte)(128 - (byte.MaxValue - recKey));
                    ReceiveKey[pinfo.PlayerId][nextKey] = false;

                    // allow server to filter msg if desired
                    msg = FilterFunc(msg);

                    // bubble the message up to our controller
                    if (ChatCallback != null)
                        ChatCallback(pinfo, msg);

                    // relay the msg
                    Sent send = Server.GetReliableAllSend(4 + len);
                    Bytes.WriteUShort(send.Data, EventChatMessage, send.Length); send.Length += 2;
                    send.Data[send.Length] = recKey; send.Length++;
                    send.Data[send.Length] = pinfo.PlayerId; send.Length++;
                    Bytes.WriteString(send.Data, msg, send.Length); send.Length += Bytes.GetStringLength(msg);

                    return c;
                }
                else
                {
                    byte recKey = receipt.Data[c]; c++;
                    // if we're a client, we're receiving someone else's chat message
                    // read the playerid we're receiving from
                    byte pid = receipt.Data[c]; c++;
                    string msg = Bytes.ReadString(receipt.Data, c, out int len); c += len;

                    // before we go any further, check the recKey to see if this is a duplicate
                    // if our ReceiveKey is already true for this id, we already got the message
                    // (we also return here if the player telling us is not the host...)
                    if (ReceiveKey[pid][recKey] || pinfo.PlayerId != 0)
                        return c;
                    // otherwise, we need to fill in our receivekey now
                    ReceiveKey[pid][recKey] = true;
                    // and, when we do this, we free up the receive key that is 128 spaces from this one
                    // to ensure future messages can be received
                    byte nextKey = recKey;
                    if (nextKey < byte.MaxValue - 128)
                        nextKey += 128;
                    else
                        nextKey = (byte)(128 - (byte.MaxValue - recKey));
                    ReceiveKey[pid][nextKey] = false;

                    // find the playerinfo
                    PlayerInfo sentpinfo = Server.PlayerInfos.Values[Server.PlayerInfos.IdsToIndices[pid]];

                    // bubble the message up to our controller
                    if (ChatCallback != null)
                        ChatCallback(sentpinfo, msg);

                    return c;
                }
            }
            throw new Exception("NetExecutorChat was passed eventid '" + eventid + "' which does not belong to it.");
        }

        public ushort Register(NetServer server, ushort startIndex)
        {
            Server = server;

            EventChatMessage += startIndex;
            return 1;
        }

        public void ClientSendChat(string msg)
        {
            if (Server.IsHost)
                throw new Exception("Tried to send client chat as host");

            // increment sendkey
            if (SendKey == byte.MaxValue)
                SendKey = 0;
            else
                SendKey++;

            int len = Bytes.GetStringLength(msg);
            Sent send = Server.GetReliableAllSend(3 + len);
            Bytes.WriteUShort(send.Data, EventChatMessage, send.Length); send.Length += 2;
            send.Data[send.Length] = SendKey; send.Length++;
            Bytes.WriteString(send.Data, msg, send.Length); send.Length += len;
        }

        public void ServerSendChat(string msg)
        {
            if (!Server.IsHost)
                throw new Exception("Tried to send host chat as client");

            // increment sendkey
            if (SendKey == byte.MaxValue)
                SendKey = 0;
            else
                SendKey++;

            int len = Bytes.GetStringLength(msg);
            Sent send = Server.GetReliableAllSend(4 + len);
            Bytes.WriteUShort(send.Data, EventChatMessage, send.Length); send.Length += 2;
            send.Data[send.Length] = SendKey; send.Length++;
            send.Data[send.Length] = Server.OurPlayerId; send.Length++;
            Bytes.WriteString(send.Data, msg, send.Length); send.Length += len;

            // bubble the message up to our controller
            if (ChatCallback != null)
                ChatCallback(Server.PlayerInfos.Values[Server.PlayerInfos.IdsToIndices[Server.OurPlayerId]], msg);
        }

        public void PlayerAdded(PlayerInfo pinfo)
        {
            // ensure we have enough receivekeys
            while (ReceiveKey.Length <= pinfo.PlayerId)
            {
                BitArray[] nbas = new BitArray[ReceiveKey.Length * 2];
                for (int i = 0; i < ReceiveKey.Length; i++)
                    nbas[i] = ReceiveKey[i];
                for (int i = ReceiveKey.Length; i < nbas.Length; i++)
                    nbas[i] = new BitArray(256);
                ReceiveKey = nbas;
            }

            // clear the existing receivekey
            ReceiveKey[pinfo.PlayerId].SetAll(false);
        }

        public void PlayerRemoved(PlayerInfo pinfo)
        {
            // nothing doing
        }

        public void ClientConnected()
        {
            // nothing doing
        }
    }
}
