using RelaNet.Utilities;
using RelaStructures;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots.Basic2d
{
    public class SimulatorBasic2d : ISnapSimulator
    {
        public NetServer Server;
        public SnapInputManager<InputBasic2d, InputPackerBasic2d> Input;
        public Snapper<NentBasic2d, NentStaticBasic2d, PackerBasic2d, PackInfoBasic2d> Nents;

        public SnapInLogician<NentBasic2d, NentStaticBasic2d, PackerBasic2d, PackInfoBasic2d,
            InputBasic2d, InputPackerBasic2d,
            InAdvancerBasic2d, AdvancerConfigBasic2d> Logician;

        private NetExecutorSnapper NetSnapper;

        public SimulatorBasic2d(
            SnapInputManager<InputBasic2d, InputPackerBasic2d> input,
            Snapper<NentBasic2d, NentStaticBasic2d, PackerBasic2d, PackInfoBasic2d> nents,
            AdvancerConfigBasic2d config)
        {
            Input = input;
            Nents = nents;

            Logician = new SnapInLogician<NentBasic2d, NentStaticBasic2d, PackerBasic2d, PackInfoBasic2d,
                InputBasic2d, InputPackerBasic2d,
                InAdvancerBasic2d, AdvancerConfigBasic2d>
                (input, nents, config);
        }

        public void Loaded(NetExecutorSnapper snapper)
        {
            NetSnapper = snapper;
            Server = NetSnapper.Server;

            // pass the loaded event to our logicians
            Logician.Loaded(snapper);
        }


        // Advance Methods
        public void ClientAdvance(int times, float tickms)
        {
            ushort pretime = NetSnapper.ClientTime;
            if (pretime == 0)
                pretime = ushort.MaxValue;
            else
                pretime--;

            bool lastHadMissing = false;
            for (int i = 0; i < times; i++)
            {
                ushort confTimestamp = NetSnapper.GetConfTimestamp();
                bool noMissingImpulse = Logician.ClientPredictTick(confTimestamp);

                if (noMissingImpulse && !lastHadMissing
                    && NetSnapper.ClientImpulseTime != NetSnapper.ClientTime
                    && NetSnapper.ClientImpulseTime != pretime)
                {
                    // move up impulse on the netsnapper
                    NetSnapper.MoveImpulseTimeUp();
                }
                else
                {
                    // can't move the impulse up
                    lastHadMissing = true;
                }

                NetSnapper.AdvanceSimulateTimestamp();
            }

            Logician.ClientPredictMS(tickms);
        }

        public void ServerPreAdvance()
        {
            // Do nothing.
        }

        public void ServerAdvance()
        {
            // run advance on each logician
            Logician.ServerAdvance();
        }

        public void ServerPostAdvance()
        {
            // Do nothing.
        }
    }
}
