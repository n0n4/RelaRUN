using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots.Basic2d
{
    public class AdvancerConfigBasic2d
    {
        public const ushort NENT_PLAYEROBJ = 1;

        // note: choosing to hardcode this value. If you wanted e.g.
        // each player to have a different speed, you would need to
        // add a speed float to the NentBasic2d struct or NentStaticBasic2d
        // and reference that instead.
        public float PlayerSpeed = 0.3f; // per ms
        // when a player gives input A, they begin a dash that lasts
        // this long. We're using [Free1] to hold the timer value
        // in this example.
        public float DashTimerMax = 500f; // ms
        // afterwards, [Free1] is set to -[DashCooldownMax] (note the
        // negative sign) and it ticks back up to 0. When it hits 0 
        // the dash is available again.
        public float DashCooldownMax = 1000f; // ms
        public float DashSpeed = 0.9f; // per ms
    }
}
