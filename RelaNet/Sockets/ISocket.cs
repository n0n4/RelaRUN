using RelaNet.Messages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace RelaNet.Sockets
{
    public interface ISocket
    {
        void Send(byte[] msg, int len, IPEndPoint target);
        void Receive(byte[] msg, int len, IPEndPoint from); // for virtual socket, direct receive

        bool CanRead(int skips);
        Receipt Read(int skips); // skips -> number of packets to skip
        void EndRead(int skips);

        void Tick(float elapsedms);

        void Close();
    }
}
