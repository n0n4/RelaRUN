using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.Snapshots
{
    public interface ISnapAdvancer<TSnap, TStatic, TAdvancerConfig>
        where TSnap : struct   // entity struct
        where TStatic : struct // static data struct
    {
        void AdvanceLogic(TAdvancerConfig config, SnapHistory<TSnap, TStatic> h, ref TSnap cur, float delta);

        // interp a new snapshot between two existing snapshots (at currentIndex)
        void InterpTickLogic(SnapHistory<TSnap, TStatic> h);
        // interp between the current snapshot and the next snapshot (on ImpulseShot)
        void InterpMSLogic(SnapHistory<TSnap, TStatic> h, ref TSnap shot, float delta, float targetms);

        void BlendLogic(SnapHistory<TSnap, TStatic> h, ref TSnap shot, TSnap blendTarget, float factor);
    }
}
