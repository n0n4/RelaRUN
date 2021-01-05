using RelaNet.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots
{
    public interface ISnapSceneChanger
    {
        void ClientNewScene(byte changeType, ushort customId, string texta, string textb);
    }
}
