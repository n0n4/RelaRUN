using System;
using System.Collections.Generic;
using System.Text;
using RelaRUN.Messages;

namespace RelaRUN.Snapshots
{
    public interface ISnapInputManager
    {
        void Loaded(NetExecutorSnapper snapper, byte inputIndex);

        int ReadInput(Receipt receipt, int c, ushort timestamp, float tickms);
        void FinalizeInputs();

        void PlayerAdded(byte pid);

        void ClientReleaseInputs(ushort timestamp);
        void ServerReleaseInputs(ushort timestamp);
    }
}
