﻿using RelaRUN.Messages;
using RelaStructures;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RelaRUN.Sockets
{
    public class UdpSocket : ISocket
    {
        private UdpClient UdpClient = null;
        private bool Closed = false;
        private ReArrayIdPool<Receipt> Pool;
        private Receipt WritingReceipt;
        private NetLogger NetLogger;
        private CancellationTokenSource CancellationTokenSource;
        
        private int ReadingIndex = 0;

        public ISocket[] DirectTargets = new ISocket[0];
        public IPEndPoint[] DirectTargetIps = new IPEndPoint[0];
        public IPEndPoint DirectFromPoint; // endpoint that client sees our messages
                                           // as arriving from (ip / port)
                                           // for direct targets

        public int Port = 0;

        /// <summary>
        /// Tries to open a UdpSocket, starting with port, and if port is already 
        /// in use, incrementing and retrying until it hits maxPort.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="maxPort"></param>
        /// <param name="maxQueueSize"></param>
        /// <param name="netLogger"></param>
        public UdpSocket(int port, int maxPort, int maxQueueSize, NetLogger netLogger)
        {
            // check if port is available
            IPEndPoint[] actives = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
            while (true)
            {
                bool found = false;
                for (int i = 0; i < actives.Length; i++)
                {
                    if (actives[i].Port == port)
                    {
                        port++;
                        found = true;
                        break;
                    }
                }

                if (port > maxPort)
                {
                    throw new Exception("Tried to open UDP socket, but first available port '" + port + "' exceeded maximum port '" + maxPort + "'");
                }

                if (!found)
                {
                    break;
                }
            }

            Port = port;

            NetLogger = netLogger;
            UdpClient = new UdpClient(port);

            DirectFromPoint = new IPEndPoint(IPAddress.Loopback, port);

            // important note:
            // the pool here is ordered, which means returning an item is less efficient
            // but it guarantees that the last receipt, which is the empty one being
            // written to, stays at the end of the pool.
            Pool = new ReArrayIdPool<Receipt>(10, maxQueueSize,
                PoolCreate, (obj) => { obj.Clear(); });
            WritingReceipt = Pool.Request();

            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken ctoken = CancellationTokenSource.Token;

            Task.Run(async () =>
            {
                if (NetLogger.On)
                    NetLogger.Log("Opening [UDPv6] listener on port " + port + "...");

                while (!ctoken.IsCancellationRequested)
                {
                    try
                    {
                        Receipt rec = await ReceiveAsync();
                        // the receipt, by nature of its existence, goes into the queue.
                    }
                    catch (Exception e)
                    {
                        if (NetLogger.On)
                        {
                            NetLogger.Error("UdpClient listener encountered exception on " + port, e);
                        }
                    }
                }

                if (NetLogger.On)
                    NetLogger.Log("Closing listener on port " + port + "...");

                try
                {
                    UdpClient.Close();
                    UdpClient.Dispose();
                }
                catch
                {

                }
            });
        }

        public void AddDirectTarget(ISocket soc, IPEndPoint endpoint)
        {
            ISocket[] nsocs = new ISocket[DirectTargets.Length + 1];
            for (int i = 0; i < DirectTargets.Length; i++)
                nsocs[i] = DirectTargets[i];
            DirectTargets = nsocs;

            IPEndPoint[] nadds = new IPEndPoint[DirectTargetIps.Length + 1];
            for (int i = 0; i < DirectTargetIps.Length; i++)
                nadds[i] = DirectTargetIps[i];
            DirectTargetIps = nadds;

            DirectTargetIps[DirectTargetIps.Length - 1] = endpoint;
            DirectTargets[DirectTargets.Length - 1] = soc;
        }

        // Send messages
        public void Send(byte[] msg, int len, IPEndPoint target)
        {
            if (DirectTargetIps.Length > 0)
            {
                for (int i = 0; i < DirectTargetIps.Length; i++)
                {
                    if (DirectTargetIps[i].Address.Equals(target.Address) && DirectTargetIps[i].Port == target.Port)
                    {
                        DirectTargets[i].Receive(msg, len, DirectFromPoint);
                        return;
                    }
                }
            }

            UdpClient.Send(msg, len, target);
        }


        // Reception of messages
        private IAsyncResult BeginReceive(AsyncCallback requestCallback)
        {
            EndPoint tempRemoteEP;

            if (UdpClient.Client.AddressFamily == AddressFamily.InterNetwork)
            {
                tempRemoteEP = UdpClientExtensions.anyV4Endpoint;
            }
            else
            {
                tempRemoteEP = UdpClientExtensions.anyV6Endpoint;
            }

            // find the next unused buffer
            if (Pool.Count == Pool.MaxLength)
                throw new Exception("Not enough freed UDP receipts!");
            if(WritingReceipt.Length != 0)
                WritingReceipt = Pool.Request();
            return UdpClient.Client.BeginReceiveFrom(WritingReceipt.Data, 0, UdpClientExtensions.MaxUdpSize, SocketFlags.None, ref tempRemoteEP, requestCallback, null);
        }

        private void EndReceive(IAsyncResult asyncResult, Receipt receipt)
        {
            EndPoint tempRemoteEP;
            if (UdpClient.Client == null)
                return;

            if (UdpClient.Client.AddressFamily == AddressFamily.InterNetwork)
            {
                tempRemoteEP = UdpClientExtensions.anyV4Endpoint;
            }
            else
            {
                tempRemoteEP = UdpClientExtensions.anyV6Endpoint;
            }

            int received;
            try
            {
                received = UdpClient.Client.EndReceiveFrom(asyncResult, ref tempRemoteEP);
            }
            catch
            {
                return;
            }

            receipt.Length = received;
            receipt.EndPoint = (IPEndPoint)tempRemoteEP;
        }

        // (unused, see ReceiveAsync)
        private void ReceivedCallback(IAsyncResult result)
        {
            Receipt receipt = WritingReceipt;
            EndReceive(result, receipt);
            
            BeginReceive(new AsyncCallback(ReceivedCallback));
        }

        // for virtual socket only
        public void Receive(byte[] msg, int len, IPEndPoint from)
        {
            Receipt receipt = Pool.Request();
            Buffer.BlockCopy(msg, 0, receipt.Data, 0, len);
            receipt.Length = len;
            receipt.EndPoint = from;
        }

        // entry point into listening
        private Task<Receipt> ReceiveAsync()
        {
            return Task<Receipt>.Factory.FromAsync((callback, state) => BeginReceive(callback), (ar) =>
            {
                Receipt receipt = WritingReceipt;
                EndReceive(ar, receipt);
                return receipt;

            }, null);
        }

        // tick
        public void Tick(float elapsedms)
        {
            // nothing doing
        }

        
        // Cleanup
        public void Close()
        {
            if (NetLogger.On)
                NetLogger.Log("Closing UdpSocket on " + Port);

            Closed = true;
            CancellationTokenSource.Cancel();
            try
            {
                UdpClient.Close();
                UdpClient.Dispose();
            }
            catch
            {

            }
        }


        // Read from the message pool
        private Receipt PoolCreate()
        {
            return new Receipt(Pool);
        }

        public void StartRead()
        {
            ReadingIndex = 0;
            // caveat: if the first receipt is the receiving one, skip it
            if (Pool.Count == 0)
                return;
            if (Pool.Values[ReadingIndex].PoolId == WritingReceipt.PoolId)
                ReadingIndex++;
        }

        public bool CanRead()
        {
            return ReadingIndex < Pool.Count;
        }

        public Receipt Read()
        {
            int index = ReadingIndex;
            // increment the index
            ReadingIndex++;
            // skip next if it is the receiving
            if (ReadingIndex < Pool.Count && Pool.Values[ReadingIndex].PoolId == WritingReceipt.PoolId)
                ReadingIndex++;
            return Pool.Values[index];
        }

        public void EndRead()
        {
            for (int i = Pool.Count - 1; i >= 0; i--)
            {
                if (Pool.Values[i].CanBeReleased)
                    Pool.ReturnIndex(i);
            }
        }
    }
}
