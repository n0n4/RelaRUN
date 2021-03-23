using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots
{
    public interface ISnapInAdvancer<TSnap, TStatic, TInput, TInputPacker, TAdvancerConfig>
        : ISnapAdvancer<TSnap, TStatic, TAdvancerConfig>
        where TSnap : struct                                  // entity struct
        where TStatic : struct                                // static data struct
        where TInput : struct                                 // input struct
        where TInputPacker : struct, ISnapInputPacker<TInput> // input packer
    {
        // can this entity be affected by input? returns 0 if not, pid if it can
        byte GetInputPlayer(TSnap snap, TStatic stat);
        void InputLogic(TAdvancerConfig config, TInput action, SnapHistory<TSnap, TStatic> h, ref TSnap snap,
            byte pid, float delta);
    }
}
