using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RelaRUN.Sockets
{
    public static class UdpClientExtensions
    {
        public const int MaxUdpSize = 500;
        public const int AnyPort = 0;
        public static EndPoint anyV4Endpoint = new IPEndPoint(IPAddress.Any, AnyPort);
        public static EndPoint anyV6Endpoint = new IPEndPoint(IPAddress.IPv6Any, AnyPort);
        // A note:
        // the default UDPClient implementation allocates a new byte[] buffer for every
        // single reception. Naturally, this is a terrible idea
        // to this end, we will present the following extension:
        public static int ReceiveIntoBuffer(this UdpClient client, byte[] buffer, ref IPEndPoint remoteEP)
        {
            var socket = client.Client;
            int received;
            EndPoint tempRemoteEP;
            if (socket.AddressFamily == AddressFamily.InterNetwork)
            {
                tempRemoteEP = anyV4Endpoint;
                received = socket.ReceiveFrom(buffer, 0, MaxUdpSize, SocketFlags.None, ref tempRemoteEP);
            }
            else
            {
                tempRemoteEP = anyV6Endpoint;
                received = socket.ReceiveFrom(buffer, 0, MaxUdpSize, SocketFlags.None, ref tempRemoteEP);
            }

            remoteEP = (IPEndPoint)tempRemoteEP;
            return received;
        }
    }
}
