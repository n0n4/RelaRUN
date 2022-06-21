using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaRUN.DynamicDatas.UT
{
    [TestClass]
    public class EntryTests
    {
        private byte[] Data = new byte[500];

        private void TestEntry(DynamicDataEntry entry)
        {
            DynamicDataEntry secondEntry = new DynamicDataEntry(entry.DataType);
            
            entry.BeginWrite();
            for (int i = 0; !entry.IsDoneWriting(); i++)
            {
                int nlen = entry.GetNextPacketLength();
                int written = entry.WriteNextPacket(Data, 0);
                Assert.AreEqual(nlen, written);

                int read = secondEntry.ReadPacket(Data, 0);
                Assert.AreEqual(nlen, read);
            }

            // now affirm that values match
            for (int i = 0; i < entry.DataType.Bools; i++)
                Assert.AreEqual(entry.Bools[i], secondEntry.Bools[i]);

            for (int i = 0; i < entry.DataType.Bytes; i++)
                Assert.AreEqual(entry.Bytes[i], secondEntry.Bytes[i]);

            for (int i = 0; i < entry.DataType.UShorts; i++)
                Assert.AreEqual(entry.UShorts[i], secondEntry.UShorts[i]);

            for (int i = 0; i < entry.DataType.Ints; i++)
                Assert.AreEqual(entry.Ints[i], secondEntry.Ints[i]);

            for (int i = 0; i < entry.DataType.Floats; i++)
                Assert.AreEqual(entry.Floats[i], secondEntry.Floats[i]);

            for (int i = 0; i < entry.DataType.Doubles; i++)
                Assert.AreEqual(entry.Doubles[i], secondEntry.Doubles[i]);

            for (int i = 0; i < entry.DataType.Strings; i++)
                Assert.AreEqual(entry.GetStringAtIndex(i), secondEntry.GetStringAtIndex(i));
        }

        [TestMethod]
        public void PlayingCardReadWriteTest()
        {
            DynamicDataType playingCardType = new DynamicDataType("Playing Card",
                bools: new string[] { "Face Card" },
                bytes: new string[] { "Value", "Suit" });

            DynamicDataEntry card1 = new DynamicDataEntry(playingCardType);
            card1.SetBool("Face Card", true);
            card1.SetByte("Value", 12);
            card1.SetByte("Suit", 3);

            TestEntry(card1);

            DynamicDataEntry card2 = new DynamicDataEntry(playingCardType);
            card2.SetBool("Face Card", false);
            card2.SetByte("Value", 3);
            card2.SetByte("Suit", 1);

            TestEntry(card2);

            DynamicDataEntry card3 = new DynamicDataEntry(playingCardType);
            card3.SetBool("Face Card", false);
            card3.SetByte("Value", 8);
            card3.SetByte("Suit", 2);

            TestEntry(card3);
        }

        [TestMethod]
        public void TCGCardReadWriteTest()
        {
            DynamicDataType tcgCardType = new DynamicDataType("TCG Card",
                bools: new string[] { "Instant" },
                bytes: new string[] { "Fire Cost", "Water Cost", "Rock Cost", "Wind Cost" },
                ints: new string[] { "Health", "Attack", "Armor" },
                strings: new string[] { "Name", "Description" });

            {
                DynamicDataEntry card = new DynamicDataEntry(tcgCardType);

                card.SetBool("Instant", false);

                card.SetByte("Fire Cost", 2);
                card.SetByte("Water Cost", 0);
                card.SetByte("Rock Cost", 1);
                card.SetByte("Wind Cost", 7);

                card.SetInt("Health", 100);
                card.SetInt("Attack", -7);
                card.SetInt("Armor", 8);

                card.SetString("Name", "Silver Spear");
                card.SetString("Description", "A very straightforward card");

                TestEntry(card);
            }

            {
                DynamicDataEntry card = new DynamicDataEntry(tcgCardType);

                card.SetBool("Instant", false);

                card.SetByte("Fire Cost", 0);
                card.SetByte("Water Cost", 35);
                card.SetByte("Rock Cost", 1);
                card.SetByte("Wind Cost", 0);

                card.SetInt("Health", 0);
                card.SetInt("Attack", 99999);
                card.SetInt("Armor", -1);

                card.SetString("Name", "Guard Tower");
                card.SetString("Description", "This structure does a modest and reasonable amount of damage with its " +
                    "balanced attack level. This description is reasonably long as well and " +
                    "definitely isn't too long, certainly not too long in an attempt to test " +
                    "how the system handles splitting very long strings. No difficulties will" +
                    " be undertaken in the effort to reconstruct this string. Living on easy" +
                    " street is all we do here.");

                TestEntry(card);
            }
        }

        [TestMethod]
        public void ManyIntsReadWriteTest()
        {
            string[] ints = new string[100];
            for (int i = 0; i < ints.Length; i++)
                ints[i] = i.ToString();

            DynamicDataType manyIntsType = new DynamicDataType("Many Ints",
                bools: new string[] { "Face Card" },
                bytes: new string[] { "Value", "Suit" },
                ints: ints);

            DynamicDataEntry card1 = new DynamicDataEntry(manyIntsType);
            card1.SetBool("Face Card", true);
            card1.SetByte("Value", 12);
            card1.SetByte("Suit", 3);
            for (int i = 0; i < ints.Length; i++)
                card1.SetInt(ints[i], i);

            TestEntry(card1);

            DynamicDataEntry card2 = new DynamicDataEntry(manyIntsType);
            card2.SetBool("Face Card", false);
            card2.SetByte("Value", 3);
            card2.SetByte("Suit", 1);
            for (int i = 0; i < ints.Length; i++)
                card1.SetInt(ints[i], i * 2);

            TestEntry(card2);

            DynamicDataEntry card3 = new DynamicDataEntry(manyIntsType);
            card3.SetBool("Face Card", false);
            card3.SetByte("Value", 8);
            card3.SetByte("Suit", 2);

            TestEntry(card3);
        }
    }
}
