using System;
using System.Collections.Generic;
using System.Text;
using RelaNet.Messages;
using RelaNet.Utilities;

namespace RelaNet.Basics
{
    public class NetExecutorLobby : INetExecutor
    {
        public ushort EventCrowned = 0;
        public ushort EventKick = 1;
        public ushort EventChangeMaxPlayers = 2;

        public NetServer Server;
        public byte LobbyCrownId = 0;

        public Action<PlayerInfo> CrownedCallback;
        public string KickMessage = "Kicked by lobby crown.";
        

        public void ServerCrown(byte pid)
        {
            if (!Server.IsHost)
                return;

            PlayerInfo pinfo = Server.PlayerInfos.Values[Server.PlayerInfos.IdsToIndices[pid]];
            // verify this pid exists
            if (Server.PlayerInfos.Length <= pid)
                return; // can't exist
            if (!pinfo.Active)
                return; // not active

            LobbyCrownId = pid;

            // send a message to all players
            Sent send = Server.GetReliableAllSend(3);
            Bytes.WriteUShort(send.Data, EventCrowned, send.Length); send.Length += 2;
            send.Data[send.Length] = pid; send.Length++;

            if (CrownedCallback != null)
                CrownedCallback(pinfo);
        }

        public void ClientPassCrown(byte targetpid)
        {
            if (LobbyCrownId != Server.OurPlayerId || Server.IsHost)
                return; // we are not the crown...

            // send a message to the host
            Sent send = Server.GetReliableAllSend(3);
            Bytes.WriteUShort(send.Data, EventCrowned, send.Length); send.Length += 2;
            send.Data[send.Length] = targetpid; send.Length++;
        }

        public void ClientChangeMaxPlayers(int newmax)
        {
            if (LobbyCrownId != Server.OurPlayerId || Server.IsHost)
                return;

            if (newmax < byte.MinValue || newmax > byte.MaxValue)
                return;

            Sent send = Server.GetReliableAllSend(3);
            Bytes.WriteUShort(send.Data, EventCrowned, send.Length); send.Length += 2;
            send.Data[send.Length] = (byte)newmax; send.Length++;
        }

        public void ClientKickPlayer(byte targetid)
        {
            if (LobbyCrownId != Server.OurPlayerId || Server.IsHost)
                return;

            Sent send = Server.GetReliableAllSend(3);
            Bytes.WriteUShort(send.Data, EventKick, send.Length); send.Length += 2;
            send.Data[send.Length] = targetid; send.Length++;
        }

        public void PlayerAdded(PlayerInfo pinfo)
        {
            if (Server.IsHost && pinfo.PlayerId != 0 && LobbyCrownId == 0)
            {
                // if no one is crowned, crown the first player who enters
                ServerCrown(pinfo.PlayerId);
            }
            else
            {
                // we need to tell the new client who the crown is, so recrown
                // the current crowned player
                ServerCrown(LobbyCrownId);
            }
        }

        public void PlayerRemoved(PlayerInfo pinfo)
        {
            if (Server.IsHost && LobbyCrownId == pinfo.PlayerId)
            {
                // crown has left, pick a new crown (or revert to 0 if none exist)
                for (int i = 0; i < Server.PlayerInfos.Count; i++)
                {
                    PlayerInfo newpinfo = Server.PlayerInfos.Values[i];
                    if (newpinfo.Active && newpinfo.PlayerId != pinfo.PlayerId)
                    {
                        ServerCrown(newpinfo.PlayerId);
                        return;
                    }
                }

                // if we got here, no players were found, so crown the default
                ServerCrown(0);
            }
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
            if (eventid == EventCrowned)
            {
                byte newpid = receipt.Data[c]; c++;
                if (Server.IsHost && pinfo.PlayerId == LobbyCrownId)
                {
                    // if we're the host, we only accept messages from the current crown for this
                    ServerCrown(newpid);
                }
                else if (!Server.IsHost && pinfo.PlayerId == 0)
                {
                    // if we're a client, we only accept messages from the server

                    // note that we do no validity checks here: we assume the server knows
                    // what it is doing. And, if the crown is a player we have not received
                    // the join for yet, we still need to store them.
                    LobbyCrownId = newpid;
                    if (CrownedCallback != null)
                        CrownedCallback(Server.PlayerInfos.Values[Server.PlayerInfos.IdsToIndices[newpid]]);
                }
                return c;
            }
            else if (eventid == EventKick)
            {
                byte targetpid = receipt.Data[c]; c++;
                if (Server.IsHost && pinfo.PlayerId == LobbyCrownId)
                {
                    // if we're the host, we only accept messages from the current crown for this
                    PlayerInfo targetpinfo = Server.PlayerInfos.Values[Server.PlayerInfos.IdsToIndices[targetpid]];
                    // verify this pid exists
                    if (Server.PlayerInfos.Length <= targetpid)
                        return c; // can't exist
                    if (!targetpinfo.Active)
                        return c; // not active
                    Server.RemovePlayer(targetpid, KickMessage);
                }
                return c;
            }
            else if (eventid == EventChangeMaxPlayers)
            {
                byte newmax = receipt.Data[c]; c++;
                if (Server.IsHost && pinfo.PlayerId == LobbyCrownId)
                {
                    Server.ServerChangeMaxPlayers(newmax);
                }
                return c;
            }
            throw new Exception("NetExecutorLobby was passed eventid '" + eventid + "' which does not belong to it.");
        }

        public ushort Register(NetServer server, ushort startIndex)
        {
            Server = server;

            EventCrowned += startIndex;
            EventKick += startIndex;
            EventChangeMaxPlayers += startIndex;
            return 3;
        }
    }
}
