using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RO_VAJA_5_BLOCKCHAIN.DataStructures
{
    public class Connection
    {
        public static int StdServerPort = 10548;
        public static int MaxBufferSize = 4096;
        public static int defaultLocalPort = 25351;
        private List<byte[]> recievedData = new List<byte[]>();
        public bool Error { get; private set; } = false;
        public Node node1 { get; private set; }
        public Node node2 { get; private set; }
        public bool ConncetionRunning = false;

        TcpClient client;
        NetworkStream clientStream;
        TcpClient remoteClient;
        NetworkStream remoteClientStream;
        TcpListener server;
        public Connection(Node node1, Node node2)
        {
            this.node1 = node1;
            this.node2 = node2;
            client = new TcpClient(node2.IPEndPoint);
            server = new TcpListener(node1.IPEndPoint);
            RunConnection(false);
        }
        public Connection(Node node2)
        {
            this.node2 = node2;
            this.node1 = new Node("127.0.0.1", defaultLocalPort++);
            client = new TcpClient();
            server = new TcpListener(node1.IPEndPoint);
            RunConnection(false);
        }
        private void ConnctToClientSServer()
        {
            try
            {
                byte[] buffer = new byte[MaxBufferSize];
                int bytesRead = remoteClientStream.Read(buffer, 0, MaxBufferSize);
                Array.Resize(ref buffer, bytesRead);
                string res = Encoding.UTF8.GetString(buffer);
                string[] ipPort = res.Split(';');
                node2.IP = ipPort[0];
                node2.Port = int.Parse(ipPort[1]);
                byte[] confirmation = new byte[1];
                confirmation[0] = 1;
                remoteClientStream.Write(confirmation, 0, confirmation.Length);
                client = new TcpClient();
                client.Connect(node2.IPEndPoint);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n" + e.StackTrace, "ERROR");
                return;
            }
        }
        public Connection(Node node1, TcpListener _server, TcpClient _rclient)
        {
            this.node1 = node1;
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
        }
        private void ConnectToServer()
        {
            try
            {
                byte[] confirmation = new byte[1];
                client.Connect(node2.IPEndPoint);
                clientStream = client.GetStream();
                byte[] IpAndPortOfTheServer = Encoding.ASCII.GetBytes($"{node1.IP.ToString()};{node1.Port.ToString()}");
                clientStream.Write(IpAndPortOfTheServer, 0, 5);
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
            return;
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
    }
}
