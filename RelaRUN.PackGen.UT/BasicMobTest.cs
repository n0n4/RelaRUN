using Microsoft.VisualStudio.TestTools.UnitTesting;
using RelaRUN.Messages;
using System;
using System.Reflection;
using System.Text;

namespace RelaRUN.PackGen.UT
{
    public class BoolMob
    {
        public string Name = "Mob";
        public bool OnFire = true;
        public bool Jumping = false;
        public bool Poisoned = true;

        public bool CompareTo(BoolMob target)
        {
            return Name == target.Name
                && OnFire == target.OnFire
                && Jumping == target.Jumping
                && Poisoned == target.Poisoned;
        }
    }

    public class BoolHoarder
    {
        public string Name = "Hoarder";
        public bool OnFire = true;
        public bool Jumping = false;
        public bool Poisoned = true;
        public bool Underwater = true;
        public bool Electrified = false;
        public bool Crouching = false;
        public bool Dazed = true;
        public bool Confused = false;

        public bool IsEnemy = false;
        public bool IsFriendly = false;
        public bool Sleeping = true;
        public bool Sprinting = true;

        public bool CompareTo(BoolHoarder target)
        {
            return Name == target.Name
                && OnFire == target.OnFire
                && Jumping == target.Jumping
                && Poisoned == target.Poisoned
                && Underwater == target.Underwater
                && Electrified == target.Electrified
                && Crouching == target.Crouching
                && Dazed == target.Dazed
                && Confused == target.Confused

                && IsEnemy == target.IsEnemy
                && IsFriendly == target.IsFriendly
                && Sleeping == target.Sleeping
                && Sprinting == target.Sprinting;
        }
    }

    [TestClass]
    public class BoolCollectionTest
    {
        [TestMethod]
        public void HoarderGenTest()
        {
            // parse
            GenInfo info = GenInfo.Read(typeof(BoolHoarder));

            // render
            StringBuilder sb = new StringBuilder();
            info.WriteClass(sb, "RelaRUN.PackGen.UT", false);
            string code = sb.ToString();

            // compile
            Assembly asm = CompilerHelper.Compile(code, "BoolHoarderGenTest");

            // test the assembly
            Type packerType = asm.GetType("RelaRUN.PackGen.UT.BoolHoarderPacker");

            BoolHoarder bm = new BoolHoarder();
            int writelen = (int)packerType.GetMethod("GetWriteLength").Invoke(null, new object?[] { bm });
            Assert.AreEqual(10, writelen);

            Sent sent = new Sent();
            packerType.GetMethod("Pack").Invoke(null, new object?[] { bm, sent });
            Assert.AreEqual(writelen, sent.Length);

            BoolHoarder bm2 = new BoolHoarder()
            {
                Name = "",
                OnFire = false,
                Jumping = false,
                Poisoned = false,
                Underwater = false,
                Electrified = false,
                Crouching = false,
                Dazed = false,
                Confused = false,

                IsEnemy = false,
                IsFriendly = false,
                Sleeping = false,
                Sprinting = false
            };
            Receipt receipt = new Receipt(null);
            receipt.Data = sent.Data;
            receipt.Length = sent.Length;

            packerType.GetMethod("Unpack").Invoke(null, new object?[] { bm2, receipt, 0 });
            Assert.IsTrue(bm.CompareTo(bm2));
        }

        [TestMethod]
        public void BoolGenTest()
        {
            // parse
            GenInfo info = GenInfo.Read(typeof(BoolMob));

            // render
            StringBuilder sb = new StringBuilder();
            info.WriteClass(sb, "RelaRUN.PackGen.UT", false);
            string code = sb.ToString();

            // compile
            Assembly asm = CompilerHelper.Compile(code, "BoolMobGenTest");

            // test the assembly
            Type packerType = asm.GetType("RelaRUN.PackGen.UT.BoolMobPacker");

            BoolMob bm = new BoolMob();
            int writelen = (int)packerType.GetMethod("GetWriteLength").Invoke(null, new object?[] { bm });
            Assert.AreEqual(5, writelen);

            Sent sent = new Sent();
            packerType.GetMethod("Pack").Invoke(null, new object?[] { bm, sent });
            Assert.AreEqual(writelen, sent.Length);

            BoolMob bm2 = new BoolMob()
            {
                Name = "",
                OnFire = false,
                Jumping = false,
                Poisoned = false
            };
            Receipt receipt = new Receipt(null);
            receipt.Data = sent.Data;
            receipt.Length = sent.Length;

            packerType.GetMethod("Unpack").Invoke(null, new object?[] { bm2, receipt, 0 });
            Assert.IsTrue(bm.CompareTo(bm2));
        }
    }
}
