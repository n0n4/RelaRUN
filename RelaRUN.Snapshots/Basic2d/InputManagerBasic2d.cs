using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.Snapshots.Basic2d
{
    public static class InputManagerBasic2d
    {
        public static SnapInputManager<InputBasic2d, InputPackerBasic2d> Make()
        {
            return new SnapInputManager<InputBasic2d, InputPackerBasic2d>();
        }
    }
}
