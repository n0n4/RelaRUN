using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots.Basic2d
{
    public static class SnapInputManagerBasic2d
    {
        public static SnapInputManager<InputBasic2d, SnapInputPackerBasic2d> Make()
        {
            return new SnapInputManager<InputBasic2d, SnapInputPackerBasic2d>();
        }
    }
}
