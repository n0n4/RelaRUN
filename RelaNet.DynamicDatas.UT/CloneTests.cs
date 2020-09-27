using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.DynamicDatas.UT
{
    [TestClass]
    public class CloneTests
    {
        // full update tests and stale update tests
        private byte[] Data = new byte[500];

        private DynamicDataClone TestCloneFull(DynamicDataClone clone)
        {
            DynamicDataClone secondClone = new DynamicDataClone(clone.Entry);

            clone.BeginWrite();
            for (int i = 0; !clone.IsDoneWriting(); i++)
            {
                int nlen = clone.GetNextPacketLength(false);
                int written = clone.WriteNextPacket(Data, 0, false);
                Assert.AreEqual(nlen, written);

                int read = secondClone.ReadPacket(Data, 0);
                Assert.AreEqual(nlen, read);
            }

            clone.ClearStales();

            // now affirm that values match
            for (int i = 0; i < clone.Entry.DataType.Bools; i++)
                Assert.AreEqual(clone.Bools[i], secondClone.Bools[i]);

            for (int i = 0; i < clone.Entry.DataType.Bytes; i++)
                Assert.AreEqual(clone.Bytes[i], secondClone.Bytes[i]);

            for (int i = 0; i < clone.Entry.DataType.UShorts; i++)
                Assert.AreEqual(clone.UShorts[i], secondClone.UShorts[i]);

            for (int i = 0; i < clone.Entry.DataType.Ints; i++)
                Assert.AreEqual(clone.Ints[i], secondClone.Ints[i]);

            for (int i = 0; i < clone.Entry.DataType.Floats; i++)
                Assert.AreEqual(clone.Floats[i], secondClone.Floats[i]);

            for (int i = 0; i < clone.Entry.DataType.Doubles; i++)
                Assert.AreEqual(clone.Doubles[i], secondClone.Doubles[i]);

            for (int i = 0; i < clone.Entry.DataType.Strings; i++)
                Assert.AreEqual(clone.GetStringAtIndex(i), secondClone.GetStringAtIndex(i));

            return secondClone;
        }

        private void TestCloneStale(DynamicDataClone clone, DynamicDataClone secondClone)
        {
            clone.BeginWrite();
            for (int i = 0; !clone.IsDoneWriting(); i++)
            {
                int nlen = clone.GetNextPacketLength(true);
                int written = clone.WriteNextPacket(Data, 0, true);
                Assert.AreEqual(nlen, written);

                int read = secondClone.ReadPacket(Data, 0);
                Assert.AreEqual(nlen, read);
            }

            clone.ClearStales();

            // now affirm that values match
            for (int i = 0; i < clone.Entry.DataType.Bools; i++)
                Assert.AreEqual(clone.Bools[i], secondClone.Bools[i]);

            for (int i = 0; i < clone.Entry.DataType.Bytes; i++)
                Assert.AreEqual(clone.Bytes[i], secondClone.Bytes[i]);

            for (int i = 0; i < clone.Entry.DataType.UShorts; i++)
                Assert.AreEqual(clone.UShorts[i], secondClone.UShorts[i]);

            for (int i = 0; i < clone.Entry.DataType.Ints; i++)
                Assert.AreEqual(clone.Ints[i], secondClone.Ints[i]);

            for (int i = 0; i < clone.Entry.DataType.Floats; i++)
                Assert.AreEqual(clone.Floats[i], secondClone.Floats[i]);

            for (int i = 0; i < clone.Entry.DataType.Doubles; i++)
                Assert.AreEqual(clone.Doubles[i], secondClone.Doubles[i]);

            for (int i = 0; i < clone.Entry.DataType.Strings; i++)
                Assert.AreEqual(clone.GetStringAtIndex(i), secondClone.GetStringAtIndex(i));
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

                DynamicDataClone clone1 = new DynamicDataClone(card);
                clone1.SetBool("Instant", true);
                clone1.SetByte("Water Cost", 2);
                clone1.SetByte("Rock Cost", 0);
                clone1.SetInt("Health", 99);
                clone1.SetString("Name", "Silver Spear Copy");

                DynamicDataClone clone2 = TestCloneFull(clone1);

                clone1.SetInt("Health", 50);

                TestCloneStale(clone1, clone2);

                clone1.SetInt("Attack", 8);
                clone1.SetByte("Water Cost", 3);

                TestCloneStale(clone1, clone2);
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

                DynamicDataClone clone1 = new DynamicDataClone(card);
                clone1.SetBool("Instant", true);
                clone1.SetByte("Water Cost", 2);
                clone1.SetByte("Rock Cost", 0);
                clone1.SetInt("Health", 99);
                clone1.SetString("Description", "changed This structure does a modest and reasonable amount of damage with its " +
                    "balanced attack level. This description is reasonably long as well and " +
                    "definitely isn't too long, certainly not too long in an attempt to test " +
                    "how the system handles splitting very long strings. No difficulties will" +
                    " be undertaken in the effort to reconstruct this string. Living on easy" +
                    " street is all we do here.");

                DynamicDataClone clone2 = TestCloneFull(clone1);

                clone1.SetInt("Health", 50);

                TestCloneStale(clone1, clone2);

                clone1.SetInt("Attack", 8);
                clone1.SetByte("Water Cost", 3);

                clone1.SetString("Description", "changed again and again This structure does a modest and reasonable amount of damage with its " +
                    "balanced attack level. This description is reasonably long as well and " +
                    "definitely isn't too long, certainly not too long in an attempt to test " +
                    "how the system handles splitting very long strings. No difficulties will" +
                    " be undertaken in the effort to reconstruct this string. Living on easy" +
                    " street is all we do here.");

                TestCloneStale(clone1, clone2);
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

                DynamicDataClone clone1 = new DynamicDataClone(card);
                clone1.SetBool("Instant", true);
                clone1.SetByte("Water Cost", 2);
                clone1.SetByte("Rock Cost", 0);
                clone1.SetInt("Health", 99);
                clone1.SetString("Name", "Guard Tower Copy");

                DynamicDataClone clone2 = TestCloneFull(clone1);

                clone1.SetInt("Health", 50);

                TestCloneStale(clone1, clone2);

                clone1.SetInt("Attack", 8);
                clone1.SetByte("Water Cost", 3);

                TestCloneStale(clone1, clone2);
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

            DynamicDataClone clone1 = new DynamicDataClone(card1);
            clone1.SetInt(ints[10], 55);
            clone1.SetInt(ints[11], 1003);
            clone1.SetInt(ints[56], 990002);
            clone1.SetByte("Suit", 5);

            DynamicDataClone clone2 = TestCloneFull(clone1);

            clone1.SetInt(ints[33], 60);
            clone1.SetInt(ints[10], 5);

            TestCloneStale(clone1, clone2);

            clone1.SetByte("Suit", 7);

            TestCloneStale(clone1, clone2);
        }
    }
}
