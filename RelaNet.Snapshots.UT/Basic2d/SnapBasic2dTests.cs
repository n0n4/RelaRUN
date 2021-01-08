using Microsoft.VisualStudio.TestTools.UnitTesting;
using RelaNet.Snapshots.Basic2d;
using RelaNet.UT;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelaNet.Snapshots.UT.Basic2d
{
    [TestClass]
    public class SnapBasic2dTests
    {
        private void SimpleEntitiesTestLogic(int clients, int amount)
        {
            SnapBasic2dEnvironment senv = new SnapBasic2dEnvironment(clients);

            senv.Activate();
            senv.FastTick();

            // have the server ghost some new entities
            List<byte> eids = new List<byte>();
            for (int i = 0; i < amount; i++)
            {
                senv.AddEntityFirst(new NentBasic2d()
                {
                    X = 10f + i,
                    Y = 5f + i,
                    XVel = 3f
                }, out byte eid);

                eids.Add(eid);
            }

            ushort timeAdded = senv.NetSnappers[0].CurrentTime;

            senv.FastTick();

            ushort timeChecked = senv.NetSnappers[0].CurrentTime;
            int diff = timeChecked - timeAdded;
            Snapper<NentBasic2d, PackerBasic2d, PackInfoBasic2d> checkNent =
                senv.Nents[0];

            senv.FastTick(); // allow extra time to be sure the clients
                                  // have these timestamps

            // verify that the client has the entities
            for (int c = 1; c < senv.NetSnappers.Count; c++)
            {
                NetExecutorSnapper ns = senv.NetSnappers[c];
                Snapper<NentBasic2d, PackerBasic2d, PackInfoBasic2d> nent = senv.Nents[c];

                for (int i = 0; i < eids.Count; i++)
                {
                    byte eid = eids[i];

                    // pull the entity from the source so we can compare
                    SnapHistory<NentBasic2d> checkH = checkNent.GetFirstEntity(eid);
                    Assert.AreNotEqual(null, checkH);

                    // does this ent exist?
                    SnapHistory<NentBasic2d> h = nent.GetFirstEntity(eid);
                    Assert.AreNotEqual(null, h);

                    // do we have snapshots for the original timestamp?
                    int origIndex = h.FindIndex(timeAdded);
                    Assert.IsTrue(origIndex != -1);

                    // are the values in it correct?
                    Assert.AreEqual(10f + i, h.Shots[origIndex].X);
                    Assert.AreEqual(5f + i, h.Shots[origIndex].Y);
                    Assert.AreEqual(3f, h.Shots[origIndex].XVel);

                    // do we have snapshots for the latest timestamp?
                    int checkIndex = h.FindIndex(timeChecked);
                    Assert.IsTrue(checkIndex != -1);
                    int checkAgainstIndex = checkH.FindIndex(timeChecked);
                    Assert.IsTrue(checkAgainstIndex != -1);

                    // are the values in it correct?
                    Assert.AreEqual(checkH.Shots[checkAgainstIndex].X, h.Shots[checkIndex].X);
                    Assert.AreEqual(5f + i, h.Shots[checkIndex].Y);
                    Assert.AreEqual(3f, h.Shots[checkIndex].XVel);
                }
            }
        }

        [TestMethod]
        public void SimpleSnapTest()
        {
            SimpleEntitiesTestLogic(1, 10);
        }

        [TestMethod]
        public void Simple255SnapTest()
        {
            SimpleEntitiesTestLogic(1, 255);
        }

        [TestMethod]
        public void Simple255x16SnapTest()
        {
            SimpleEntitiesTestLogic(16, 255);
        }
    }
}
