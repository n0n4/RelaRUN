using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RelaRUN.Utilities.UT
{
    [TestClass]
    public class BitsTests
    {
        [TestMethod]
        public void BitFlagsTest()
        {
            void innerTest(bool[] truth)
            {
                byte b = 0;
                for (int i = 0; i < truth.Length; i++)
                    if (truth[i])
                        b = Bits.AddTrueBit(b, i);

                // now test
                for (int i = 0; i < truth.Length; i++)
                    Assert.AreEqual(truth[i], Bits.CheckBit(b, i));
            }

            bool[] make(string t)
            {
                bool[] bs = new bool[t.Length];
                for (int i = 0; i < t.Length; i++)
                    bs[i] = t[i] == '1';
                return bs;
            }

            innerTest(make("00000000"));
            innerTest(make("11111111"));

            innerTest(make("01010101"));
            innerTest(make("10101010"));

            innerTest(make("10000000"));
            innerTest(make("01000000"));
            innerTest(make("00100000"));
            innerTest(make("00010000"));
            innerTest(make("00001000"));
            innerTest(make("00000100"));
            innerTest(make("00000010"));
            innerTest(make("00000001"));

            innerTest(make("01111111"));
            innerTest(make("10111111"));
            innerTest(make("11011111"));
            innerTest(make("11101111"));
            innerTest(make("11110111"));
            innerTest(make("11111011"));
            innerTest(make("11111101"));
            innerTest(make("11111110"));

            innerTest(make("11100100"));
            innerTest(make("01111001"));
            innerTest(make("01100110"));
            innerTest(make("00010110"));
            innerTest(make("10000001"));
            innerTest(make("01010010"));
        }
    }
}
