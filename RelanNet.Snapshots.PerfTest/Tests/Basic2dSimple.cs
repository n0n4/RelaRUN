using RelaNet.Snapshots;
using RelaNet.Snapshots.Basic2d;
using RelaNet.Snapshots.UT.Basic2d;
using System;
using System.Collections.Generic;
using System.Text;

namespace RelanNet.Snapshots.PerfTest.Tests
{
    public static class Basic2dSimple
    {
        public static void ConsoleInteraction(Reporter rep)
        {
            Console.WriteLine("");
            Console.WriteLine("Select a Test");
            Console.WriteLine("1) Test");
            while (true)
            {
                string s = Console.ReadLine();
                s = s.Trim().ToLower();

                if (s == "1")
                {
                    ConsoleTest(rep);
                    break;
                }
            }
        }

        public static void ConsoleTest(Reporter rep)
        {
            Console.WriteLine("");
            Console.WriteLine("How many clients (max 256)?");
            int clients = 0;
            while (true)
            {
                string s = Console.ReadLine();
                s = s.Trim().ToLower();

                if (int.TryParse(s, out int a))
                {
                    clients = a;
                    break;
                }
            }

            Console.WriteLine("");
            Console.WriteLine("How many dynamic 1st entities (max 256)?");
            int firsts = 0;
            while (true)
            {
                string s = Console.ReadLine();
                s = s.Trim().ToLower();

                if (int.TryParse(s, out int a))
                {
                    firsts = a;
                    break;
                }
            }

            int maxFirstStatic = 256 - firsts;
            Console.WriteLine("");
            Console.WriteLine("How many static 1st entities (max " + maxFirstStatic + ")?");
            int firstStatics = 0;
            while (true)
            {
                string s = Console.ReadLine();
                s = s.Trim().ToLower();

                if (int.TryParse(s, out int a))
                {
                    firstStatics = a;
                    break;
                }
            }

            Console.WriteLine("");
            Console.WriteLine("How many dynamic 2nd entities (max 65535)?");
            int seconds = 0;
            while (true)
            {
                string s = Console.ReadLine();
                s = s.Trim().ToLower();

                if (int.TryParse(s, out int a))
                {
                    seconds = a;
                    break;
                }
            }

            int maxSecondStatic = 65535 - seconds;
            Console.WriteLine("");
            Console.WriteLine("How many static 2nd entities (max " + maxSecondStatic + ")?");
            int secondStatics = 0;
            while (true)
            {
                string s = Console.ReadLine();
                s = s.Trim().ToLower();

                if (int.TryParse(s, out int a))
                {
                    secondStatics = a;
                    break;
                }
            }

            Console.WriteLine("");
            Console.WriteLine("How many runs?");
            int runs = 0;
            while (true)
            {
                string s = Console.ReadLine();
                s = s.Trim().ToLower();

                if (int.TryParse(s, out int a))
                {
                    runs = a;
                    break;
                }
            }

            Test(rep, clients, (byte)firsts, (byte)firstStatics,
                (ushort)seconds, (ushort)secondStatics, runs);
        }

        public static void Test(Reporter rep, int clients, 
            byte amount, byte staticAmount,
            ushort secondAmount, ushort staticSecondAmount, 
            int runs)
        {
            rep.StartRecord("Basic2dSimple", "Test " + clients + "c " + amount 
                + "e " + secondAmount + "e", runs);
            rep.StartTimer();

            SnapBasic2dEnvironment senv = new SnapBasic2dEnvironment(clients);

            senv.Activate();
            senv.FastTick();

            // have the server ghost some new entities
            for (int i = 0; i < amount; i++)
            {
                senv.AddEntityFirst(new NentBasic2d()
                {
                    X = 10f + i,
                    Y = 5f + i,
                    XVel = 3f
                }, out byte eid);
            }

            for (int i = 0; i < staticAmount; i++)
            {
                senv.AddEntityFirst(new NentBasic2d()
                {
                    X = 10f + i,
                    Y = 5f + i
                }, out byte eid);
            }

            for (int i = 0; i < secondAmount; i++)
            {
                senv.AddEntitySecond(new NentBasic2d()
                {
                    X = 10f + i,
                    Y = 5f + i,
                    XVel = 3f
                }, out ushort eid);
            }

            for (int i = 0; i < staticSecondAmount; i++)
            {
                senv.AddEntitySecond(new NentBasic2d()
                {
                    X = 10f + i,
                    Y = 5f + i
                }, out ushort eid);
            }

            ushort timeAdded = senv.NetSnappers[0].CurrentTime;

            senv.FastTick();

            rep.EndWarmup();

            // verify that the client has the entities
            float tickTime = senv.NetSnappers[0].TickMSTarget;
            for (int r = 0; r < runs; r++)
            {
                rep.StartTimer();
                senv.Tick(tickTime);
                rep.EndRun();
            }

            rep.EndRecord();
        }
    }
}
