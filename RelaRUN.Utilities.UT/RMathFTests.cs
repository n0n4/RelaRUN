using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.Utilities.UT
{
    [TestClass]
    public class RMathFTests
    {
        public void AssertFloatEquals(float expected, float actual,
            float tolerancePercent = 0.02f)
        {
            if (actual < expected * (1f - tolerancePercent)
                || actual > expected * (1f + tolerancePercent))
            {
                // failsafe: both really close to 0
                if (Math.Abs(actual) < 0.00001f && Math.Abs(expected) < 0.00001f)
                    return;

                Assert.Fail(actual + " did not fall within "
                    + (tolerancePercent * 100f) + "% of " + expected
                    + " (" + (expected * (1f - tolerancePercent)) + " - "
                    + (expected * (1f + tolerancePercent)) + ")");
            }
        }

        public void AssertFloatEqualsRadians(float expected, float actual,
            float tolerancePercent = 0.02f)
        {
            if (actual < expected * (1f - tolerancePercent)
                || actual > expected * (1f + tolerancePercent))
            {
                // failsafe: 2PI = 0
                if (expected > RMathF.TWOPI * (1f - tolerancePercent)
                    && actual < (RMathF.TWOPI * (1f + tolerancePercent)) - RMathF.TWOPI)
                    return;

                if (actual > RMathF.TWOPI * (1f - tolerancePercent)
                    && expected < (RMathF.TWOPI * (1f + tolerancePercent)) - RMathF.TWOPI)
                    return;

                // failsafe: both really close to 0
                if (Math.Abs(actual) < 0.00001f && Math.Abs(expected) < 0.00001f)
                    return;

                Assert.Fail(actual + " did not fall within "
                    + (tolerancePercent * 100f) + "% of " + expected
                    + " (" + (expected * (1f - tolerancePercent)) + " - "
                    + (expected * (1f + tolerancePercent)) + ")");
            }
        }

        [TestMethod]
        public void AngleMidpointTest()
        {
            void test(float dega, float degb, float degresult)
            {
                AssertFloatEqualsRadians(RMathF.ToRad(degresult),
                    RMathF.AngleMidpoint(RMathF.ToRad(dega), RMathF.ToRad(degb)));
                AssertFloatEqualsRadians(RMathF.ToRad(degresult),
                    RMathF.AngleMidpoint(RMathF.ToRad(degb), RMathF.ToRad(dega)));
            }

            test(45f, 135f, 90f);
            test(30f, 340f, 5f);
            test(200f, 100f, 150f);
            test(10f, 200f, 285f);
        }

        [TestMethod]
        public void AngleBlendTest()
        {
            void test(float dega, float degb, float degresult, float percent)
            {
                // check extrema
                AssertFloatEqualsRadians(RMathF.ToRad(dega),
                    RMathF.AngleBlend(RMathF.ToRad(dega), RMathF.ToRad(degb),
                    0));
                AssertFloatEqualsRadians(RMathF.ToRad(degb),
                    RMathF.AngleBlend(RMathF.ToRad(dega), RMathF.ToRad(degb),
                    1f));

                // check midpoint
                AssertFloatEqualsRadians(
                    RMathF.AngleMidpoint(RMathF.ToRad(dega), RMathF.ToRad(degb)),
                    RMathF.AngleBlend(RMathF.ToRad(dega), RMathF.ToRad(degb),
                    0.5f));

                AssertFloatEqualsRadians(RMathF.ToRad(degresult),
                    RMathF.AngleBlend(RMathF.ToRad(dega), RMathF.ToRad(degb),
                    percent));
            }

            test(100f, 200f, 125f, 0.25f);
            test(200f, 100f, 125f, 0.75f);
            test(100f, 200f, 175f, 0.75f);
            test(200f, 100f, 175f, 0.25f);

            test(350f, 10f, 355f, 0.25f);
            test(10f, 350f, 355f, 0.75f);
            test(350f, 10f, 5f, 0.75f);
            test(10f, 350f, 5f, 0.25f);
        }

        [TestMethod]
        public void ClampTest()
        { 
            Assert.AreEqual(30f, RMathF.Clamp(20f, 30f, 50f));
            Assert.AreEqual(50f, RMathF.Clamp(90f, 30f, 50f));
            Assert.AreEqual(40f, RMathF.Clamp(40f, 30f, 50f));
        }
    }
}
