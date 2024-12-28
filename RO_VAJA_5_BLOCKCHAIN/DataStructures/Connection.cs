﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RO_VAJA_5_BLOCKCHAIN.DataStructures
{
    public class Connection : INotifyPropertyChanged
    {
        public static string LocalNodeId = "0";
        public static int MaxBufferSize = 4096;
        public static int defaultLocalPort = 25351;
        public static bool NewDataRecieved = false;
        private List<byte[]> recievedData = new List<byte[]>();
        public bool Error { get; private set; } = false;
        public Node _localNode { get; private set; } = new Node(LocalNodeId);
        public Node _remoteNode { get; private set; }
        public bool ConncetionRunning = false;
        public event PropertyChangedEventHandler? PropertyChanged;

        TcpClient client;
        NetworkStream clientStream;
        TcpClient remoteClient;
        NetworkStream remoteClientStream;
        TcpListener server;
        public Connection(Node localNode, Node remoteNode)
        {
            this._localNode = localNode;
            this._remoteNode = remoteNode;
            client = new TcpClient(remoteNode.IPEndPoint);
            server = new TcpListener(localNode.IPEndPoint);
            RunConnection(false);
        }
        public Connection(Node remoteNode)
        {
            this._remoteNode = remoteNode;
            FindAvailableLocalPort();
            this._localNode = new Node(LocalNodeId, "127.0.0.1", defaultLocalPort++);
            client = new TcpClient();
            server = new TcpListener(_localNode.IPEndPoint);
            RunConnection(false);
        }
        
        public Connection(Node localNode, TcpListener _server, TcpClient _rclient)
        {
            this._localNode = localNode;
            server = _server;
            remoteClient = _rclient;
            remoteClientStream = remoteClient.GetStream();
            RunConnection(true);
        }
        public byte[] PopRecieved()
        {
            byte[] data = recievedData[0];
            recievedData.RemoveAt(0);
            return data;
        }
        public bool RecievedData()
        {
            return recievedData.Count > 0;
        }
        private async void SendLedger(ObservableCollection<Block> ledger)
        {
            client.GetStream().Write(BitConverter.GetBytes(ledger.Count), 0, 4);
            client.GetStream().Read(new byte[1], 0, 1);
            foreach (Block block in ledger)
            {
                await InstantSend(block.ToByteArray());
            }
        }
        private ObservableCollection<Block>? RecieveLedger() 
        {
            ObservableCollection<Block>? ledger = null;
            byte[] buffer = new byte[4];
            remoteClientStream.Read(buffer, 0, 4);
            int count = BitConverter.ToInt32(buffer, 0);
            byte[] confirmation = new byte[1];
            confirmation[0] = 1;
            remoteClientStream.Write(confirmation, 0, 1);
            for(int i = 0; i < count; i++)
            {
                byte[] data = new byte[MaxBufferSize];
                int bytesRead = remoteClientStream.Read(data, 0, data.Length);
                Array.Resize(ref data, bytesRead);
                ledger.Add(new Block(data));
                remoteClientStream.Write(confirmation, 0, 1);
            }
            return ledger;
        }
        private void SendHashesUntilSignal(ObservableCollection<Block> ledger)
        {
            byte[] buffer = new byte[1];
            int i = ledger.Count - 1;
            while (buffer[0] == 2)
            {
                string hash = ledger[i].Hash;
                byte[] hashes = Encoding.UTF8.GetBytes(hash);
                client.GetStream().Write(hashes, 0, hashes.Length);
                client.GetStream().Read(buffer, 0, 1);
                i--;
                if(i < 0)
                    break;
            }
        }
        private void RecieveHashRange(ObservableCollection<byte[]> data, int range) {
            for (int i = 0; i < range; i++)
            {
                byte[] buffer = new byte[MaxBufferSize];
                int bytesRead = remoteClientStream.Read(buffer, 0, MaxBufferSize);
                Array.Resize(ref buffer, bytesRead);
                data.Add(buffer);
                if(i != range - 1)
                {
                    byte[] confirmation = new byte[1];
                    confirmation[0] = 1;
                    remoteClientStream.Write(confirmation, 0, 1);
                }
            }
        }
        private void EndFindingHashes()
        {
            byte[] buffer = new byte[1];
            buffer[0] = 2;
            client.GetStream().Write(buffer, 0, 1);
        }
        private void FindLastCorrectHash(ObservableCollection<Block> ledger)
        {
            byte[] buffer = new byte[MaxBufferSize];
            byte[] confirm = new byte[1];
            confirm[0] = 1;
            
        }
        private async Task Reciever()
        {
            while (ConncetionRunning)
            {
                byte[] data = new byte[MaxBufferSize];
                byte[] confirmation = new byte[1];
                confirmation[0] = 1;
                int bytesRead = remoteClientStream.Read(data, 0, data.Length);
                Array.Resize(ref data, bytesRead);
                recievedData.Add(data);
                remoteClient.GetStream().Write(confirmation, 0, confirmation.Length);
                NewDataRecieved = true;
            }
        }
        public void Send(byte[] data)
        {
            Task.Run(() => InstantSend(data));
            return;
        }
        private async Task InstantSend(byte[] data)
        {
            byte[] confirmation = new byte[1];
            confirmation[0] = 0;
            while (confirmation[0] == 0)
            {
                client.GetStream().Write(data, 0, data.Length);
                client.GetStream().Read(confirmation, 0, confirmation.Length);
            }
        }
        private async void RunConnection(bool connectionEstablished)
        {
            if(!connectionEstablished)
            {
                try
                {
                    ConnectToServer();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message + "\n" + e.StackTrace, "ERROR");
                    Error = true;
                    client.Close();
                    return;
                }
                ServerListen();
                ConncetionRunning = true;
            }
            else
            {
                ConnctToClientSServer();
                ConncetionRunning = true;
            }
            Task.Run(() => Reciever());
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Node1"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Node2"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Connection"));
        }
        private void ConnectToServer()
        {
            try
            {
                byte[] confirmation = new byte[1];
                client.Connect(_remoteNode.IPEndPoint);
                clientStream = client.GetStream();
                byte[] IdIpPort = Encoding.ASCII.GetBytes($"{LocalNodeId};{_localNode.IP.ToString()};{_localNode.Port.ToString()}");
                clientStream.Write(IdIpPort, 0, IdIpPort.Count());
                clientStream.Read(confirmation, 0, 1);
                if (confirmation[0] == 1)
                {
                    return;
                }
                else
                {
                    throw new Exception("Connection failed");
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message + "\n" + e.StackTrace, "ERROR");
                return;
            }
        }
        private void ConnctToClientSServer()
        {
            try
            {
                byte[] buffer = new byte[MaxBufferSize];
                int bytesRead = remoteClientStream.Read(buffer, 0, MaxBufferSize);
                Array.Resize(ref buffer, bytesRead);
                string res = Encoding.UTF8.GetString(buffer);
                string[] idIpPort = res.Split(';');
                _remoteNode = new Node(idIpPort[0], idIpPort[1], int.Parse(idIpPort[2]));
                byte[] confirmation = new byte[1];
                confirmation[0] = 1;
                remoteClientStream.Write(confirmation, 0, confirmation.Length);
                client = new TcpClient();
                client.Connect(_remoteNode.IPEndPoint);
                clientStream = client.GetStream();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n" + e.StackTrace, "ERROR");
                return;
            }
        }
        private void ServerListen()
        {
            try 
            {
                server.Start();
                remoteClient = server.AcceptTcpClient();
                remoteClientStream = remoteClient.GetStream();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n" + e.StackTrace, "ERROR");
                return;
            }
            return;
        }
        public Node LocalNode
        {
            get { return _localNode; }
        }
        public Node RemoteNode
        {
            get { return _remoteNode; }
        }
        public void FindAvailableLocalPort()
        {
            defaultLocalPort = FindAvailablePort(defaultLocalPort);
            if(defaultLocalPort == -1)
            {
                defaultLocalPort = FindAvailablePort(0);
                if(defaultLocalPort == -1)
                {
                    MessageBox.Show("No available ports", "ERROR");
                }
            }
        }
        public static int FindAvailablePort(int start = 25351)
        {
            int port = start;
            while (true)
            {
                try
                {
                    TcpListener listener = new TcpListener(System.Net.IPAddress.Parse("127.0.0.1"), port);
                    listener.Start();
                    listener.Stop();
                    return port;
                }
                catch
                {
                    if (port >= 65534)
                    {
                        return -1;
                    }
                    else
                    {
                        port++;
                    }
                }
            }
        }
    }
}
