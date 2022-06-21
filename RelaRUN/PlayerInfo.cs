using RelaRUN.Sockets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace RelaRUN
{
    public struct PlayerInfo
    {
        public byte PlayerId;
        public IPEndPoint EndPoint;
        public string Name;
        public bool Active;
        public int ChallengeKey;
        public bool Removed;

        public PlayerInfo(byte pid, IPEndPoint endpoint, string name, bool active, int challengeKey)
        {
            PlayerId = pid;
            EndPoint = endpoint;
            Name = name;
            Active = active;
            ChallengeKey = challengeKey;
            Removed = true;
        }

        public void Clear()
        {
            PlayerId = 0;
            Name = string.Empty;
            Active = false;
            ChallengeKey = 0;
            Removed = true;
        }

        public void Move(ref PlayerInfo target)
        {
            target.PlayerId = PlayerId;
            target.EndPoint = EndPoint;
            target.Name = Name;
            target.Active = Active;
            target.ChallengeKey = ChallengeKey;
            target.Removed = Removed;
        }

        public void Setup(byte pid, IPEndPoint endpoint, string name, bool active, int challengeKey)
        {
            PlayerId = pid;
            EndPoint = endpoint;
            Name = name;
            Active = active;
            ChallengeKey = challengeKey;
            Removed = false;
        }
    }
}
