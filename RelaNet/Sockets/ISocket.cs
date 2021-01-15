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

        void StartRead();
        bool CanRead();
        Receipt Read();
        void EndRead();

        void Tick(float elapsedms);

        void Close();
    }
}
