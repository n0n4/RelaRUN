using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Utilities.UT
{
    [TestClass]
    public class BytesTests
    {
        [TestMethod]
        public void UShortTest()
        {
            void innerTest(ushort[] us)
            {
                byte[] bs = new byte[us.Length * 2];
                for (int i = 0; i < us.Length; i++)
                    Bytes.WriteUShort(bs, us[i], i * 2);

                for (int i = 0; i < us.Length; i++)
                    Assert.AreEqual(us[i], Bytes.ReadUShort(bs, i * 2));
            }

            innerTest(new ushort[] { 0 });
            innerTest(new ushort[] { 1 });
            innerTest(new ushort[] { 0, 1 });
            innerTest(new ushort[] { 10, 333, 51, 51 , 6 });
            innerTest(new ushort[] { 1000, 4000, 5000, 77 });
            innerTest(new ushort[] { 0, 0, 0, 0, 5, 6, 8 });
        }

        [TestMethod]
        public void IntTest()
        {
            void innerTest(int[] us)
            {
                byte[] bs = new byte[us.Length * 4];
                for (int i = 0; i < us.Length; i++)
                    Bytes.WriteInt(bs, us[i], i * 4);

                for (int i = 0; i < us.Length; i++)
                    Assert.AreEqual(us[i], Bytes.ReadInt(bs, i * 4));
            }

            innerTest(new int[] { 0 });
            innerTest(new int[] { 1 });
            innerTest(new int[] { 0, 1 });
            innerTest(new int[] { 10, 333, 51, 51, 6 });
            innerTest(new int[] { 1000, 4000, 5000, 77 });
            innerTest(new int[] { 0, 0, 0, 0, 5, 6, 8 });
            innerTest(new int[] { 0, 0, 0, -777780, 5, 6, -8 });
            innerTest(new int[] { 91000, 94000, 95000, -977 });
        }

        [TestMethod]
        public void FloatTest()
        {
            void innerTest(float[] us)
            {
                byte[] bs = new byte[us.Length * 4];
                for (int i = 0; i < us.Length; i++)
                    Bytes.WriteFloat(bs, us[i], i * 4);

                for (int i = 0; i < us.Length; i++)
                    Assert.AreEqual(us[i], Bytes.ReadFloat(bs, i * 4));
            }

            innerTest(new float[] { 0 });
            innerTest(new float[] { 1 });
            innerTest(new float[] { 0, 1 });
            innerTest(new float[] { 10, 333, 51, 51, 6 });
            innerTest(new float[] { 1000, 4000, 5000, 77 });
            innerTest(new float[] { 0, 0, 0, 0, 5, 6, 8 });
            innerTest(new float[] { 0, 0, 0, -777780, 5, 6, -8 });
            innerTest(new float[] { 91000, 94000, 95000, -977 });
            innerTest(new float[] { 0.075f });
            innerTest(new float[] { 10, 33.33f });
            innerTest(new float[] { 0, -0.005f, 13.35f, 100029.3f });
            innerTest(new float[] { 51.1f, 0, 5.5f, 5.125f, 51.01f, 6 });
        }

        [TestMethod]
        public void DoubleTest()
        {
            void innerTest(double[] us)
            {
                byte[] bs = new byte[us.Length * 8];
                for (int i = 0; i < us.Length; i++)
                    Bytes.WriteDouble(bs, us[i], i * 8);

                for (int i = 0; i < us.Length; i++)
                    Assert.AreEqual(us[i], Bytes.ReadDouble(bs, i * 8));
            }

            innerTest(new double[] { 0 });
            innerTest(new double[] { 1 });
            innerTest(new double[] { 0, 1 });
            innerTest(new double[] { 10, 333, 51, 51, 6 });
            innerTest(new double[] { 1000, 4000, 5000, 77 });
            innerTest(new double[] { 0, 0, 0, 0, 5, 6, 8 });
            innerTest(new double[] { 0, 0, 0, -777780, 5, 6, -8 });
            innerTest(new double[] { 91000, 94000, 95000, -977 });
            innerTest(new double[] { 0.075 });
            innerTest(new double[] { 10, 33.33 });
            innerTest(new double[] { 0, -0.005, 13.35, 100029.3 });
            innerTest(new double[] { 51.1, 0, 5.5, 5.125, 51.01, 6 });
        }

        [TestMethod]
        public void StringTest()
        {
            void innerTest(string[] us)
            {
                int len = 0;
                for (int i = 0; i < us.Length; i++)
                    len += Bytes.GetStringLength(us[i]);

                byte[] bs = new byte[len];
                int c = 0;
                for (int i = 0; i < us.Length; i++)
                {
                    Bytes.WriteString(bs, us[i], c);
                    c += Bytes.GetStringLength(us[i]);
                }

                c = 0;
                for (int i = 0; i < us.Length; i++)
                {
                    Assert.AreEqual(us[i], Bytes.ReadString(bs, c));
                    c += Bytes.GetStringLength(us[i]);
                }
            }

            innerTest(new string[] { "" });
            innerTest(new string[] { " " });
            innerTest(new string[] { "impediment" });
            innerTest(new string[] { "cake", "cookies" });
            innerTest(new string[] { "and down we went", "where were you" });
            innerTest(new string[] { "that's my name" });
            innerTest(new string[] { "who's there\r\n who's here" });
            innerTest(new string[] { "a", "b", "c" });
            innerTest(new string[] { ".;'/[]\\1234567890-=qwertyuiopasdfghjkl",
                ";'zxcvbnm,./~!@#$%^&*()_+QWERTYUIOP{}ASDFGHJKL:ZXCVBNM<>?|",
                "you're welcome"});
        }
    }
}
