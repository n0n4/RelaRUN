using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RelaRUN.DynamicDatas.UT
{
    [TestClass]
    public class TypeTests
    {
        private byte[] Data = new byte[500];

        private void TestType(DynamicDataType type)
        {
            DynamicDataType secondType = new DynamicDataType("UNLOADED");

            // test the header
            int hlen = type.GetHeaderLength();
            int hwritten = type.WriteHeader(Data, 0);
            Assert.AreEqual(hlen, hwritten);

            int hread = secondType.LoadHeader(Data, 0);
            Assert.AreEqual(hlen, hread);

            // affirm the results
            Assert.AreEqual(type.TypeName, secondType.TypeName);
            Assert.AreEqual(type.Bools, secondType.Bools);
            Assert.AreEqual(type.Bytes, secondType.Bytes);
            Assert.AreEqual(type.UShorts, secondType.UShorts);
            Assert.AreEqual(type.Ints, secondType.Ints);
            Assert.AreEqual(type.Floats, secondType.Floats);
            Assert.AreEqual(type.Doubles, secondType.Doubles);
            Assert.AreEqual(type.Strings, secondType.Strings);
            Assert.AreEqual(type.TotalCount, secondType.TotalCount);

            // test the names
            type.BeginNamesWrite();
            int npackets = type.GetNamesCount();
            for (int i = 0; i < npackets; i++)
            {
                int nlen = type.GetNextNamesLength();
                int nwritten = type.WriteNextNames(Data, 0);
                Assert.AreEqual(nlen, nwritten);

                int nread = secondType.LoadNames(Data, 0);
                Assert.AreEqual(nlen, nread);
            }

            // affirm the results
            for (int i = 0; i < type.TotalCount; i++)
            {
                Assert.AreEqual(type.Names[i], secondType.Names[i]);
            }
        }

        [TestMethod]
        public void PlayingCardReadWriteTest()
        {
            TestType(new DynamicDataType("Playing Card",
                bools: new string[] { "Face Card" },
                bytes: new string[] { "Value", "Suit" }));
        }

        [TestMethod]
        public void TCGCardReadWriteTest()
        {
            TestType(new DynamicDataType("TCG Card",
                bools: new string[] { "Instant" },
                bytes: new string[] { "Fire Cost", "Water Cost", "Rock Cost", "Wind Cost" },
                ints: new string[] { "Health", "Attack", "Armor" },
                strings: new string[] { "Description" }));
        }

        [TestMethod]
        public void EverythingBagelReadWriteTest()
        {
            TestType(new DynamicDataType("Everything Bagel",
                bools: new string[] { "Selectable", "Flying", "Invulnerable" },
                bytes: new string[] { "PlayerId", "Lives", "Cards", "Mining Skill" },
                ushorts: new string[] { "Year", "Coins" },
                ints: new string[] { "Health", "Wins" },
                floats: new string[] { "X", "Y", "Speediness", "Acceleration" },
                doubles: new string[] { "Interest Rate", "K", "Jerk", "Troublemaking Quotient" },
                strings: new string[] { "Town", "Street", "County", "State",
                    "Description", "Statement of Purpose", "Beliefs" }));
        }
    }
}
