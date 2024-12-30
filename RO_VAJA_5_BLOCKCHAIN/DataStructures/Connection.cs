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
        private readonly byte[] SEND_LEDGER_SIGNAL = new byte[] { 128, 128, 255, 255, 128 };
        private readonly byte[] SEND_LEDGER_SIGNAL_RECV_READY = new byte[] { 255, 128, 255, 128, 255 };
        private readonly byte[] RECV_LEDGER_SIGNAL = new byte[] { 255, 128, 255, 128, 255 };
        public static string LocalNodeId = "0";
        public static int MaxBufferSize = 4096;
        public static int defaultLocalPort = 25351;
        public static bool NewDataRecieved = false;
        //private ObservableCollection<Block> Ledger;
        public static Action PauseMining = () => { };
        public static Action ResumeMining = () => { };
        private Action<ObservableCollection<Block>> LedgerSetter;
        private Func<ObservableCollection<Block>> LedgerGetter;
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
        public Connection(Node localNode, Node remoteNode, Action<ObservableCollection<Block>> ledgerSetter, Func<ObservableCollection<Block>> ledgerGetter)
        {
            this._localNode = localNode;
            this._remoteNode = remoteNode;
            client = new TcpClient(remoteNode.IPEndPoint);
            server = new TcpListener(localNode.IPEndPoint);
            LedgerSetter = ledgerSetter;
            LedgerGetter = ledgerGetter;
            RunConnection(false);
        }
        public Connection(Node remoteNode, Action<ObservableCollection<Block>> ledgerSetter, Func<ObservableCollection<Block>> ledgerGetter)
        {
            this._remoteNode = remoteNode;
            FindAvailableLocalPort();
            this._localNode = new Node(LocalNodeId, "127.0.0.1", defaultLocalPort++);
            client = new TcpClient();
            server = new TcpListener(_localNode.IPEndPoint);
            LedgerSetter = ledgerSetter;
            LedgerGetter = ledgerGetter;
            RunConnection(false);
        }
        
        public Connection(Node localNode, TcpListener _server, TcpClient _rclient, Action<ObservableCollection<Block>> ledgerSetter, Func<ObservableCollection<Block>> ledgerGetter)
        {
            this._localNode = localNode;
            server = _server;
            remoteClient = _rclient;
            remoteClientStream = remoteClient.GetStream();
            LedgerSetter = ledgerSetter;
            LedgerGetter = ledgerGetter;
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
        private void SendLedger()
        {
            ObservableCollection<Block> ledger = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                ledger = LedgerGetter();
            });
            byte[] ledgerSize = BitConverter.GetBytes(ledger.Count);
            client.GetStream().Write(ledgerSize, 0, 4);
            int a = 0;
            foreach (Block block in ledger)
            {
                InstantSendSync(block.ToByteArray());
                MessageBox.Show(block.ToString());
                a++;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                recievedData.Clear();
            });
        }
        private void RecieveLedger() { 
            ObservableCollection<Block> ledger = new ObservableCollection<Block>();
            int ledgerSize = BitConverter.ToInt32(InstantRecieveSync(), 0);
            for (int i = 0; i < ledgerSize; i++)
            {
                byte[] data = InstantRecieveSync();
                Block block = new Block(data);
                MessageBox.Show(block.ToString());
                ledger.Add(block);
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                LedgerSetter(ledger);
                recievedData.Clear();
            });
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
                if (data.SequenceEqual(SEND_LEDGER_SIGNAL))
                {
                    Application.Current.Dispatcher.Invoke(() => { PauseMining(); });
                    InstantSendSync(RECV_LEDGER_SIGNAL);
                    SendLedger();
                    remoteClientStream.Write(confirmation, 0, confirmation.Length);
                    //Application.Current.Dispatcher.Invoke(() => { ResumeMining(); });
                }
                else if(data.SequenceEqual(RECV_LEDGER_SIGNAL))
                {
                    Application.Current.Dispatcher.Invoke(() => { PauseMining(); });
                    remoteClientStream.Write(SEND_LEDGER_SIGNAL_RECV_READY, 0, SEND_LEDGER_SIGNAL_RECV_READY.Length);
                    RecieveLedger();
                    remoteClientStream.Write(confirmation, 0, confirmation.Length);
                    //Application.Current.Dispatcher.Invoke(() => { ResumeMining(); });
                }
                else if(data.SequenceEqual(SEND_LEDGER_SIGNAL_RECV_READY))
                {
                    Application.Current.Dispatcher.Invoke(() => { PauseMining(); });
                    SendLedger();
                    remoteClientStream.Write(confirmation, 0, confirmation.Length);
                    //Application.Current.Dispatcher.Invoke(() => { ResumeMining(); });
                }
                else
                {
                    recievedData.Add(data);
                    remoteClient.GetStream().Write(confirmation, 0, confirmation.Length);
                    NewDataRecieved = true;
                }
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
        private void InstantSendSync(byte[] data)
        {
            byte[] confirmation = new byte[5];
            confirmation[0] = 0;
            client.GetStream().Read(confirmation, 0, confirmation.Length);
            client.GetStream().Write(data, 0, data.Length);
        }
        private byte[] InstantRecieveSync()
        {
            byte[] data = new byte[MaxBufferSize];
            byte[] confirmation = new byte[1];
            confirmation[0] = 1;
            remoteClientStream.Write(confirmation, 0, confirmation.Length);
            int bytesRecieved = remoteClientStream.Read(data, 0, data.Length);
            Array.Resize(ref data, bytesRecieved);
            return data;
        }
        private void InstantSendSync_NoConfirm(byte[] data)
        {
            client.GetStream().Write(data, 0, data.Length);
        }
        public void ReceveLongerLedger()
        {
            InstantSendSync_NoConfirm(SEND_LEDGER_SIGNAL);
        }
        public void SendLongerLedger()
        {
            InstantSendSync_NoConfirm(RECV_LEDGER_SIGNAL);
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
