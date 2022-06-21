using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.Utilities
{
    public static class RMathF
    {
        public const float PI = 3.14159265f;
        public const float TWOPI = 6.2831853f;

        public static float Clamp(float a, float min, float max)
        {
            return (a < min ? min : (a > max ? max : a));
        }

        public static float ToRad(float deg)
        {
            return (deg / 360f) * TWOPI;
        }

        public static float ToDeg(float rad)
        {
            return (rad / TWOPI) * 360f;
        }

        public static float Sin(float rad)
        {
            return (float)Math.Sin(rad);
        }

        public static float Cos(float rad)
        {
            return (float)Math.Cos(rad);
        }

        public static float Atan2(float y, float x)
        {
            return (float)Math.Atan2(y, x);
        }
        
        public static float AngleMidpoint(float a, float b)
        {
            // order them by size, so a > b always
            if (b > a)
            {
                float t = a;
                a = b;
                b = t;
            }

            // determine which direction is closer: over or under
            if (a - b < PI)
            {
                // if the under distance is less than PI, it must be 
                // the shorter distance, so our answer is simply to
                // halve the distance
                return (a + b) / 2f;
            }
            else if (a != b)
            {
                // if the under distance is more than PI, it will be faster
                // to go over instead.
                a -= TWOPI;
                float t = (a + b) / 2f;
                if (t < 0)
                    t += TWOPI;
                return t;
            }

            // otherwise, the angles are the same
            return a;
        }

        public static float AngleBlend(float a, float b, float percent)
        {
            if (percent == 0)
                return a;
            if (percent == 1)
                return b;

            // determine which direction is closer: over or under
            float underDist = Math.Abs(a - b);
            if (underDist < PI)
            {
                // if the under distance is less than PI, it must be 
                // the shorter distance, so our answer is simply to
                // blend the two angles
                return (a * (1f - percent)) + (b * percent);
            }
            else if (a != b)
            {
                // if the under distance is more than PI, it will be faster
                // to go over instead.
                float overDist = TWOPI - underDist;
                if (a < b)
                {
                    a -= overDist * percent;
                    if (a < 0)
                        a += TWOPI;
                    return a;
                }
                else
                {
                    a += overDist * percent;
                    if (a > TWOPI)
                        a -= TWOPI;
                    return a;
                }
            }

            // otherwise, the angles are the same
            return a;
        }
    }
}
