using Microsoft.VisualStudio.TestTools.UnitTesting;
using RelaNet.Sockets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace RelaNet.UT
{
    public class TestEnvironment
    {
        public NetServer ServerHost;
        public NetServer[] Clients;

        public VirtualSocket SocketHost;
        public VirtualSocket[] SocketClients;

        public TestEnvironment(int clientCount, Action<NetServer> execSetup)
        {
            ServerHost = new NetServer(true);
            execSetup(ServerHost);

            IPEndPoint hostEndpoint = new IPEndPoint(IPAddress.Parse("1.1.1.1"), 4444);

            SocketHost = ServerHost.OpenVirtual(new VirtualSocket[clientCount],
                new IPEndPoint[clientCount],
                hostEndpoint);

            Clients = new NetServer[clientCount];
            SocketClients = new VirtualSocket[clientCount];
            for (int i = 0; i < clientCount; i++)
            {
                IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.Parse("3.3.3." + i), 4444);
                Clients[i] = new NetServer(false, hostEndpoint);
                execSetup(Clients[i]);
                SocketClients[i] = Clients[i].OpenVirtual(new VirtualSocket[] { SocketHost },
                    new IPEndPoint[] { hostEndpoint },
                    clientEndpoint);

                SocketHost.Targets[i] = SocketClients[i];
                SocketHost.Addresses[i] = clientEndpoint;
            }
        }

        public void SetSocketRandom(int seed)
        {
            SocketHost.Random = new Random(seed);
            for (int i = 0; i < SocketClients.Length; i++)
                SocketClients[i].Random = new Random(seed + 5 + 73 * i);
        }

        public void SetDropChance(double percent)
        {
            SocketHost.SimulatedDropChance = percent;
            for (int i = 0; i < SocketClients.Length; i++)
                SocketClients[i].SimulatedDropChance = percent;
        }

        public void SetLatency(double min, double max)
        {
            SocketHost.SimulatedLatencyMin = min;
            SocketHost.SimulatedLatencyMax = max;
            for (int i = 0; i < SocketClients.Length; i++)
            {
                SocketClients[i].SimulatedLatencyMin = min;
                SocketClients[i].SimulatedLatencyMax = max;
            }
        }

        public void BeginChallenge(int clientindex, Action<NetServer.EChallengeResponse> callback, 
            string name, string password)
        {
            Clients[clientindex].BeginChallengeRequest(callback, name, password);
        }

        public void BeginChallengeAll(Action<NetServer.EChallengeResponse, int> callback,
            string[] names, string password)
        {
            for (int i = 0; i < Clients.Length; i++)
                BeginChallenge(i, (e) => { callback(e, i); }, names[i], password);
        }

        public void Tick(float elapsedms)
        {
            ServerHost.Tick(elapsedms);
            for (int i = 0; i < Clients.Length; i++)
                Clients[i].Tick(elapsedms);
        }

        public void TickRepeat(float elapsedms, int times)
        {
            for (int i = 0; i < times; i++)
                Tick(elapsedms);
        }

        public static string[] GetClientNames(int clientCount)
        {
            string[] names = new string[clientCount];
            for (int i = 0; i < clientCount; i++)
                names[i] = "client" + i;
            return names;
        }

        public static TestEnvironment AutoConnected(int clientCount, Action<NetServer> execSetup)
        {
            string[] names = GetClientNames(clientCount);

            TestEnvironment tenv = new TestEnvironment(clientCount, execSetup);
            tenv.BeginChallengeAll(
                (e, i) => { Assert.AreEqual(NetServer.EChallengeResponse.ACCEPT, e); },
                names, "");

            // let the environment tick
            tenv.TickRepeat(6, 100);

            return tenv;
        }
    }
}
