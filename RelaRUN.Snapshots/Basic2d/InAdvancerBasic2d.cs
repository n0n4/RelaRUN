using RelaRUN.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.Snapshots.Basic2d
{
    public struct InAdvancerBasic2d 
        : ISnapInAdvancer<NentBasic2d, NentStaticBasic2d, InputBasic2d, InputPackerBasic2d, AdvancerConfigBasic2d>
    {
        // advance methods
        public void AdvanceLogic(AdvancerConfigBasic2d config, SnapHistory<NentBasic2d, NentStaticBasic2d> h, ref NentBasic2d cur, float delta)
        {
            if (h.StaticData.Id2 == AdvancerConfigBasic2d.NENT_PLAYEROBJ)
            {
                // some special considerations for the playerobject
                if (cur.Free1 > 0)
                {
                    // if Free1 is over 0, we're in the middle of a dash
                    // this means we're moving quickly in the direction
                    // of our rotation

                    // we're expecting the client to provide rotation in
                    // radians, for the record
                    cur.XVel = config.DashSpeed * RMathF.Cos(cur.Rot);
                    cur.YVel = config.DashSpeed * RMathF.Sin(cur.Rot);

                    // reduce the dash timer 
                    cur.Free1 -= delta;
                    // if we cross 0, set up the cooldown timer
                    if (cur.Free1 <= 0)
                        cur.Free1 = -config.DashCooldownMax;

                    // note: something we're not really handling here
                    // is that on the tick that the dash ends, we may 
                    // get a few ms of "extra" dash b/c if it has say
                    // 10ms remaining and we have 16ms in the tick, 
                    // you're dashing for the full 16ms even though
                    // you should only have 10ms of dash.

                    // you could probably fix this issue if precision
                    // matters in your use case. but for this demo
                    // I think it's outside the scope
                }
                else if (cur.Free1 < 0)
                {
                    // if we're negative, we're on cooldown
                    // count back up to 0, when we get to 0
                    // the dash is available again.
                    cur.Free1 += delta;
                    if (cur.Free1 >= 0)
                        cur.Free1 = 0;
                }
            }

            cur.X += cur.XVel * delta;
            cur.Y += cur.YVel * delta;
        }

        
        // interp methods
        public void InterpMSLogic(SnapHistory<NentBasic2d, NentStaticBasic2d> h,
            ref NentBasic2d shot, float delta, float targetms)
        {
            // interpolate from Current forward delta ms
            float tickpercent = delta / targetms;
            float invtickpercent = 1.0f - tickpercent;

            // we don't adjust IDs at all

            // pos/vel is simple, just blend
            shot.X = (shot.X * invtickpercent) + (h.Shots[h.NextIndex].X * tickpercent);
            shot.Y = (shot.Y * invtickpercent) + (h.Shots[h.NextIndex].Y * tickpercent);
            shot.XVel = (shot.XVel * invtickpercent) + (h.Shots[h.NextIndex].XVel * tickpercent);
            shot.YVel = (shot.YVel * invtickpercent) + (h.Shots[h.NextIndex].YVel * tickpercent);

            // it makes sense to blend Free1 as well since we just
            // use it as a timer, but in other cases this might not 
            // be appropriate
            shot.Free1 = (shot.Free1 * invtickpercent) + (h.Shots[h.NextIndex].Free1 * tickpercent);

            // rotation is more complicated to blend
            shot.Rot = RMathF.AngleBlend(shot.Rot, h.Shots[h.NextIndex].Rot, tickpercent);
        }

        public void InterpTickLogic(SnapHistory<NentBasic2d, NentStaticBasic2d> h)
        {
            // interpolates from Prev to Next to create Current
            h.Shots[h.CurrentIndex] = h.Shots[h.PrevIndex];

            // always inherit ids from previous
            //h.Prev.Id1 = h.Prev.Id1;
            //h.Prev.Id2 = h.Prev.Id2;

            // pos/vel is easy, just average them
            h.Shots[h.CurrentIndex].X = (h.Shots[h.NextIndex].X + h.Shots[h.CurrentIndex].X) / 2f;
            h.Shots[h.CurrentIndex].Y = (h.Shots[h.NextIndex].Y + h.Shots[h.CurrentIndex].Y) / 2f;
            h.Shots[h.CurrentIndex].XVel = (h.Shots[h.NextIndex].XVel + h.Shots[h.CurrentIndex].XVel) / 2f;
            h.Shots[h.CurrentIndex].YVel = (h.Shots[h.NextIndex].YVel + h.Shots[h.CurrentIndex].YVel) / 2f;

            // in our case, we use Free1 as a timer, so it makes 
            // sense to average this as well. May not be the case
            // if Free1 is used for a different kind of value
            h.Shots[h.CurrentIndex].Free1 = (h.Shots[h.NextIndex].Free1 + h.Shots[h.CurrentIndex].Free1) / 2f;

            // rotation is more complicated to find the midpoint
            h.Shots[h.CurrentIndex].Rot = RMathF.AngleMidpoint(h.Shots[h.CurrentIndex].Rot, h.Shots[h.NextIndex].Rot);
        }

        public void BlendLogic(SnapHistory<NentBasic2d, NentStaticBasic2d> h,
            ref NentBasic2d shot, NentBasic2d blendTarget, float factor)
        {
            // interpolate from Current forward delta ms
            float invfactor = 1.0f - factor;

            // we don't adjust IDs at all

            // pos/vel is simple, just blend
            shot.X = (shot.X * invfactor) + (blendTarget.X * factor);
            shot.Y = (shot.Y * invfactor) + (blendTarget.Y * factor);
            shot.XVel = (shot.XVel * invfactor) + (blendTarget.XVel * factor);
            shot.YVel = (shot.YVel * invfactor) + (blendTarget.YVel * factor);

            // it makes sense to blend Free1 as well since we just
            // use it as a timer, but in other cases this might not 
            // be appropriate
            shot.Free1 = (shot.Free1 * invfactor) + (blendTarget.Free1 * factor);

            // rotation is more complicated to blend
            shot.Rot = RMathF.AngleBlend(shot.Rot, blendTarget.Rot, factor);
        }


        // input methods
        public byte GetInputPlayer(NentBasic2d snap, NentStaticBasic2d stat)
        {
            return stat.Id2 == AdvancerConfigBasic2d.NENT_PLAYEROBJ ? stat.Id1 : (byte)0;
        }

        public void InputLogic(AdvancerConfigBasic2d config, InputBasic2d action, SnapHistory<NentBasic2d, NentStaticBasic2d> h, ref NentBasic2d snap, byte pid, float delta)
        {
            // process the inputs for this action 
            snap.XVel = RMathF.Clamp(action.Horizontal, -1f, 1f) * config.PlayerSpeed;
            snap.YVel = RMathF.Clamp(action.Vertical, -1f, 1f) * config.PlayerSpeed;

            // set our rotation, but only if we're not mid-dash
            if (snap.Free1 <= 0)
                snap.Rot = action.Rotation;

            // dash action
            if ((action.Inputs & InputBasic2d.INPUT_A) != 0
                && snap.Free1 == 0)
            {
                // if input A is pressed, dash forward according to rotation
                // we use Free1 to store the dash timer. We can only begin a
                // dash if Free1 is equal to 0 (e.g. dash is over).
                snap.Free1 = config.DashTimerMax;

                // we don't need to set XVel/YVel here because this is done
                // in AdvanceLogic
            }

            // finally, do AdvanceLogic over the delta window
            AdvanceLogic(config, h, ref snap, delta);
        }
    }
}
