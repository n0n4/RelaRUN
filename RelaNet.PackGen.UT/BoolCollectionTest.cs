using Microsoft.VisualStudio.TestTools.UnitTesting;
using RelaNet.Messages;
using System;
using System.Reflection;
using System.Text;

namespace RelaNet.PackGen.UT
{
    public class BasicMob
    {
        public string Name = "Mob";
        public float X = 2;
        public float Y = -3.3f;
        public int Health = 100;
        public byte ArmorType = 1;
        public ushort Armor = 50;

        public bool CompareTo(BasicMob target)
        {
            return Name == target.Name
                && X == target.X
                && Y == target.Y
                && Health == target.Health
                && ArmorType == target.ArmorType
                && Armor == target.Armor;
        }
    }

    [TestClass]
    public class BasicMobTest
    {
        [TestMethod]
        public void GenTest()
        {
            // parse
            GenInfo info = GenInfo.Read(typeof(BasicMob));

            // render
            StringBuilder sb = new StringBuilder();
            info.WriteClass(sb, "RelaNet.PackGen.UT", false);
            string code = sb.ToString();

            // compile
            Assembly asm = CompilerHelper.Compile(code, "BasicMobGenTest");

            // test the assembly
            Type packerType = asm.GetType("RelaNet.PackGen.UT.BasicMobPacker");

            BasicMob bm = new BasicMob();
            int writelen = (int)packerType.GetMethod("GetWriteLength").Invoke(null, new object?[] { bm });
            Assert.AreEqual(19, writelen);

            Sent sent = new Sent();
            packerType.GetMethod("Pack").Invoke(null, new object?[] { bm, sent });
            Assert.AreEqual(writelen, sent.Length);

            BasicMob bm2 = new BasicMob()
            {
                Name = "",
                X = 0,
                Y = 0,
                Health = 0,
                ArmorType = 0,
                Armor = 0
            };
            Receipt receipt = new Receipt(null);
            receipt.Data = sent.Data;
            receipt.Length = sent.Length;

            packerType.GetMethod("Unpack").Invoke(null, new object?[] { bm2, receipt, 0 });
            Assert.IsTrue(bm.CompareTo(bm2));
        }
    }
}
