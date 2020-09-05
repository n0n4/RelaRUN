using Microsoft.VisualStudio.TestTools.UnitTesting;
using RelaNet.UT;
using System.Collections.Generic;

namespace RelaNet.Basics.UT
{
    [TestClass]
    public class ChatTests
    {
        [TestMethod]
        public void SimpleChatTest()
        {
            List<List<string>> msgs = new List<List<string>>();
            for (int i = 0; i < 4; i++)
                msgs.Add(new List<string>());
            List<NetExecutorChat> chats = new List<NetExecutorChat>();

            TestEnvironment tenv = TestEnvironment.AutoConnected(3,
                (serv) =>
                {
                    NetExecutorChat chat = new NetExecutorChat(
                        (pinfo, msg) => { msgs[serv.OurPlayerId].Add(pinfo.Name + ": " + msg); },
                        (msg) => { return msg; });
                    chats.Add(chat);
                    serv.AddExecutor(chat);
                });


            // send some messages
            chats[0].ServerSendChat("welcome everyone");
            tenv.TickRepeat(11, 10);
            chats[1].ClientSendChat("I'm here");
            tenv.TickRepeat(11, 10);
            chats[2].ClientSendChat("I'm here as well");
            tenv.TickRepeat(11, 10);
            chats[3].ClientSendChat("don't forget about *me*");
            tenv.TickRepeat(11, 10);


            // now verify receipts
            void innerTest(List<string> strs)
            {
                Assert.AreEqual(4, strs.Count);
                Assert.AreEqual(tenv.ServerHost.OurName + ": " + "welcome everyone", strs[0]);
                Assert.AreEqual(tenv.Clients[0].OurName + ": " + "I'm here", strs[1]);
                Assert.AreEqual(tenv.Clients[1].OurName + ": " + "I'm here as well", strs[2]);
                Assert.AreEqual(tenv.Clients[2].OurName + ": " + "don't forget about *me*", strs[3]);
            }
            
            foreach (List<string> cChat in msgs)
                innerTest(cChat);
        }

        [TestMethod]
        public void OverflowChatTest()
        {
            List<List<string>> msgs = new List<List<string>>();
            for (int i = 0; i < 4; i++)
                msgs.Add(new List<string>());
            List<NetExecutorChat> chats = new List<NetExecutorChat>();

            TestEnvironment tenv = TestEnvironment.AutoConnected(3,
                (serv) =>
                {
                    NetExecutorChat chat = new NetExecutorChat(
                        (pinfo, msg) => { msgs[serv.OurPlayerId].Add(pinfo.Name + ": " + msg); },
                        (msg) => { return msg; });
                    chats.Add(chat);
                    serv.AddExecutor(chat);
                });


            // send some messages (a lot of messages!)
            // the test here is to overflow the 256 message cycle and see that everything still works
            for (int i = 0; i < 300; i++)
            {
                chats[0].ServerSendChat("welcome everyone " + i);
                tenv.TickRepeat(11, 5);
                chats[1].ClientSendChat("I'm here " + i);
                tenv.TickRepeat(11, 5);
                chats[2].ClientSendChat("I'm here as well " + i);
                tenv.TickRepeat(11, 5);
                chats[3].ClientSendChat("don't forget about *me* " + i);
                tenv.TickRepeat(11, 5);
            }


            // now verify receipts
            void innerTest(List<string> strs)
            {
                Assert.AreEqual(300 * 4, strs.Count);
                for (int i = 0; i < 300; i++)
                {
                    Assert.AreEqual(tenv.ServerHost.OurName + ": " + "welcome everyone " + i, strs[i * 4]);
                    Assert.AreEqual(tenv.Clients[0].OurName + ": " + "I'm here " + i, strs[i * 4 + 1]);
                    Assert.AreEqual(tenv.Clients[1].OurName + ": " + "I'm here as well " + i, strs[i * 4 + 2]);
                    Assert.AreEqual(tenv.Clients[2].OurName + ": " + "don't forget about *me* " + i, strs[i * 4 + 3]);
                }
            }

            foreach (List<string> cChat in msgs)
                innerTest(cChat);
        }
    }
}
