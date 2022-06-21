using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.Snapshots.Basic2d
{
    public struct InputBasic2d
    {
        public float Vertical;
        public float Horizontal;
        public float Rotation;
        public byte Inputs;

        public const byte INPUT_NONE = 0;
        public const byte INPUT_A = 1;
        public const byte INPUT_B = 2;
        public const byte INPUT_C = 4;
        public const byte INPUT_D = 8;
        public const byte INPUT_E = 16;
        public const byte INPUT_F = 32;
        public const byte INPUT_G = 64;
        public const byte INPUT_H = 128;
    }
}
