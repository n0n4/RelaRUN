using RelaStructures;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.Snapshots
{
    public class SnapLogician<TSnap, TStatic, TPacker, TPackInfo, TAdvancer, TAdvancerConfig>
        where TSnap : struct                                           // entity struct
        where TStatic : struct                                         // static data struct
        where TPacker : struct, ISnapPacker<TSnap, TStatic, TPackInfo> // packer struct 
        where TPackInfo : struct                                       // packer info struct
        where TAdvancer : struct, ISnapAdvancer<TSnap, TStatic, TAdvancerConfig>
    {
        public NetServer Server;
        public Snapper<TSnap, TStatic, TPacker, TPackInfo> Nents;
        public ReArrayIdPool<SnapHistory<TSnap, TStatic>>[] NentDatas;

        private NetExecutorSnapper NetSnapper;

        private readonly TAdvancer Advancer = new TAdvancer();
        public TAdvancerConfig AdvancerConfig;

        public SnapLogician(Snapper<TSnap, TStatic, TPacker, TPackInfo> nents,
            TAdvancerConfig config)
        {
            Nents = nents;
            AdvancerConfig = config;

            NentDatas = new ReArrayIdPool<SnapHistory<TSnap, TStatic>>[2];
            NentDatas[0] = nents.FirstData;
            NentDatas[1] = nents.SecondData;
        }

        public void Loaded(NetExecutorSnapper snapper)
        {
            NetSnapper = snapper;
            Server = NetSnapper.Server;
        }


        public void ServerAdvance()
        {
            for (int d = 0; d < NentDatas.Length; d++)
            {
                ReArrayIdPool<SnapHistory<TSnap, TStatic>> data = NentDatas[d];
                for (int i = 0; i < data.Count; i++)
                {
                    SnapHistory<TSnap, TStatic> h = data.Values[i];

                    if (h.PrevFlag != SnapHistory<TSnap, TStatic>.FlagGold)
                        continue; // if the prevflag is not gold,
                                  // the entity does not exist at this timestamp
                                  // so we don't bother simulating it yet

                    // now we advance the snapshot forward to the current point in
                    // time, and then save it
                    h.Shots[h.CurrentIndex] = h.Shots[h.PrevIndex];
                    Advancer.AdvanceLogic(AdvancerConfig, h, ref h.Shots[h.CurrentIndex], NetSnapper.TickMSTarget);

                    Nents.ServerSaveSimIntoCurrent(h);
                }
            }
        }

        // predicts forward a whole tick
        // this returns true if all impulse entities have conf snapshots
        // indicating that we can turn up the impulseTimestamp
        public bool ClientPredictTick(ushort confTimestamp)
        {
            ushort prevConfTimestamp = confTimestamp;
            if (prevConfTimestamp == 0)
                prevConfTimestamp = ushort.MaxValue;
            else
                prevConfTimestamp--;

            bool noMissingImpulse = true;

            for (int d = 0; d < NentDatas.Length; d++)
            {
                ReArrayIdPool<SnapHistory<TSnap, TStatic>> data = NentDatas[d];
                for (int i = 0; i < data.Count; i++)
                {
                    SnapHistory<TSnap, TStatic> h = data.Values[i];

                    if (h.PrevFlag == SnapHistory<TSnap, TStatic>.FlagEmpty
                        || h.PrevFlag == SnapHistory<TSnap, TStatic>.FlagDeghosted)
                        continue; // if the prev flag is empty, this entity does 
                                  // not exist yet

                    // if the entity has impulse, we may need to predict
                    // if the current timestamp is after the impulse timestamp
                    if (h.HasImpulse
                        && ((NetSnapper.SimulateTimestamp >= h.ImpulseTimestamp
                            && !(NetSnapper.SimulateTimestamp >= ushort.MaxValue - 500
                                 && h.ImpulseTimestamp <= 500))
                            || (NetSnapper.SimulateTimestamp <= 500
                                && h.ImpulseTimestamp >= ushort.MaxValue - 500)))
                    {
                        // cases:
                        // 0. if SimulateTimestamp is before h.ImpulseTimestamp, don't
                        //    predict, do normal interp
                        // 1. if this entity has a gold snapshot in SimulateTimestamp + I,
                        //    then don't predict; use the server result
                        // 2. if not, advance a predicted result

                        // case 0 is accounted for by the if statement above

                        // case 1
                        int confIndex = h.FindIndex(confTimestamp);
                        if (confIndex != -1
                            && h.Flags[confIndex] == SnapHistory<TSnap, TStatic>.FlagGold
                            && h.ImpulseTimestamp == NetSnapper.SimulateTimestamp)
                        {
                            // in this case, our job is easy, we have a server confirmed snap
                            // so we just use that
                            h.ImpulseShot = h.Shots[confIndex];
                        }
                        else
                        {
                            // case 2
                            // in this case, we must predict based on the current ImpulseShot

                            // because we got here, we are missing a conf for this impulse
                            // so we can't move up the NetSnapper's ImpulseTimestamp
                            noMissingImpulse = false;

                            if (h.ImpulseTimestamp == NetSnapper.SimulateTimestamp)
                            {
                                // special consideration: if this is our first impulse,
                                // we must populate ImpulseShot

                                // if we have a confirmed shot for the previous timestamp,
                                // we should use that
                                // if we don't, this must be our first prediction, so we
                                // can simply use the previous snapshot
                                int prevConfIndex = h.FindIndex(prevConfTimestamp);
                                if (prevConfIndex != -1
                                    && h.Flags[prevConfIndex] == SnapHistory<TSnap, TStatic>.FlagGold)
                                {
                                    h.ImpulseShot = h.Shots[prevConfIndex];
                                }
                                else
                                {
                                    h.ImpulseShot = h.Shots[h.PrevIndex];
                                }
                            }
                            
                            // just do a normal advance
                            Advancer.AdvanceLogic(AdvancerConfig, h, ref h.ImpulseShot, NetSnapper.TickMSTarget);
                        }
                    }

                    // even if we did impulse above, we still want to interp
                    // because we may need to create silver snapshots to use for
                    // blending or impulse later

                    // if we have a gold from the server, we don't need to do anything
                    // because the current shot is already good
                    if (h.CurrentFlag == SnapHistory<TSnap, TStatic>.FlagGold)
                        continue;

                    // (you might notice from the following, that we resimulate silver
                    //  flags [client guesses] each tick; this way they continue to update
                    //  their guesses if new server info arrives)

                    // if not, we need to create a silver for the current shot
                    // we know the previous is gold or silver already, since we checked
                    // for empty or ghost way back above

                    // so we know the prev is good for interp, but is the next?
                    // if the next is also good for interp, we'll just interp
                    h.Shots[h.CurrentIndex] = h.Shots[h.PrevIndex];
                    if (h.NextFlag == SnapHistory<TSnap, TStatic>.FlagGold
                        || h.NextFlag == SnapHistory<TSnap, TStatic>.FlagSilver)
                    {
                        Advancer.InterpTickLogic(h);
                    }
                    else
                    {
                        // otherwise, do extrapolation 
                        Advancer.AdvanceLogic(AdvancerConfig, h, ref h.Shots[h.CurrentIndex], NetSnapper.TickMSTarget);
                    }

                    // save our guess snapshot as a new silver
                    Nents.ClientSaveSimIntoCurrent(h);
                }
            }

            return noMissingImpulse;
        }

        // predicts forward a set number of ms
        public void ClientPredictMS(float delta)
        {
            for (int d = 0; d < NentDatas.Length; d++)
            {
                ReArrayIdPool<SnapHistory<TSnap, TStatic>> data = NentDatas[d];
                for (int i = 0; i < data.Count; i++)
                {
                    SnapHistory<TSnap, TStatic> h = data.Values[i];

                    // if the impulse time has moved up, move up the entity's to match
                    if (h.HasImpulse &&
                        ((NetSnapper.ClientImpulseTime >= 500
                         && h.ImpulseTimestamp < NetSnapper.ClientImpulseTime
                         && h.ImpulseTimestamp >= NetSnapper.ClientImpulseTime - 500)
                        || (NetSnapper.ClientImpulseTime < 500
                         && h.ImpulseTimestamp < NetSnapper.ClientImpulseTime)
                        || (NetSnapper.ClientImpulseTime < 500
                         && h.ImpulseTimestamp >= ushort.MaxValue - 500)))
                        h.ImpulseTimestamp = NetSnapper.ClientImpulseTime;

                    if (h.PrevFlag == SnapHistory<TSnap, TStatic>.FlagEmpty
                        || h.PrevFlag == SnapHistory<TSnap, TStatic>.FlagDeghosted)
                        continue; // if the prev flag is empty, this entity does 
                                  // not exist yet

                    // if the entity has impulse, we may need to predict
                    // if the current timestamp is after the impulse timestamp
                    if (h.HasImpulse
                        && ((NetSnapper.SimulateTimestamp >= h.ImpulseTimestamp
                            && !(NetSnapper.SimulateTimestamp >= ushort.MaxValue - 500
                                 && h.ImpulseTimestamp <= 500))
                            || (NetSnapper.SimulateTimestamp <= 500
                                && h.ImpulseTimestamp >= ushort.MaxValue - 500)))
                    {
                        // just do a normal advance
                        Advancer.AdvanceLogic(AdvancerConfig, h, ref h.ImpulseShot, delta);

                        // now check if we need to blend
                        if (!h.PermanentImpulse
                            && ((NetSnapper.SimulateTimestamp >= h.BlendTimestamp
                                && !(NetSnapper.SimulateTimestamp >= ushort.MaxValue - 500
                                     && h.BlendTimestamp <= 500))
                                || (NetSnapper.SimulateTimestamp <= 500
                                    && h.BlendTimestamp >= ushort.MaxValue - 500)))
                        {
                            // we're in the Blend Zone
                            // find the snapshot that we would be displaying, if this were not
                            // an impulse.
                            TSnap blendTarget = h.Shots[h.CurrentIndex];
                            if (h.NextFlag == SnapHistory<TSnap, TStatic>.FlagGold
                                || h.NextFlag == SnapHistory<TSnap, TStatic>.FlagSilver)
                            {
                                Advancer.InterpMSLogic(h, ref blendTarget, delta, NetSnapper.TickMSTarget);
                            }
                            else
                            {
                                Advancer.AdvanceLogic(AdvancerConfig, h, ref blendTarget, delta);
                            }

                            // determine the blend factor
                            int blendTicks = NetSnapper.SimulateTimestamp;
                            if (NetSnapper.SimulateTimestamp <= 500
                                && h.BlendTimestamp >= ushort.MaxValue - 500)
                            {
                                blendTicks = (ushort.MaxValue - h.BlendTimestamp) + 1 + NetSnapper.SimulateTimestamp;
                            }
                            else
                            {
                                blendTicks -= h.BlendTimestamp;
                            }

                            h.ActiveBlendFactor = blendTicks * h.BlendFactor
                                + h.BlendFactor * (delta / NetSnapper.TickMSTarget);
                            if (h.ActiveBlendFactor > 1)
                            {
                                h.ActiveBlendFactor = 1;

                                // special consideration:
                                // if the impulse time equals the simulate time (in other words,
                                // this is our first timestamp of the predictive window), and
                                // our blend factor is already over 1, then we are already
                                // back to being in-sync with the server, so we can remove impulse
                                // from this entity.
                                if (NetSnapper.ClientImpulseTime == NetSnapper.SimulateTimestamp)
                                {
                                    h.HasImpulse = false;
                                }
                            }

                            // now blend the shots together
                            Advancer.BlendLogic(h, ref h.ImpulseShot, blendTarget, h.ActiveBlendFactor);
                        }

                        // finally, continue so we don't hit the interp logic below
                        continue;
                    }

                    // if not, we need to create a silver for the current shot
                    // we know the previous is gold or silver already, since we checked
                    // for empty or ghost way back above

                    // so we know the prev is good for interp, but is the next?
                    // if the next is also good for interp, we'll just interp
                    h.ImpulseShot = h.Shots[h.CurrentIndex];
                    if (h.NextFlag == SnapHistory<TSnap, TStatic>.FlagGold
                        || h.NextFlag == SnapHistory<TSnap, TStatic>.FlagSilver)
                    {
                        Advancer.InterpMSLogic(h, ref h.ImpulseShot, delta, NetSnapper.TickMSTarget);
                    }
                    else
                    {
                        // otherwise, do extrapolation 
                        Advancer.AdvanceLogic(AdvancerConfig, h, ref h.ImpulseShot, delta);
                    }

                    // note: we don't save this, it is just for rendering
                }
            }
        }
    }
}
