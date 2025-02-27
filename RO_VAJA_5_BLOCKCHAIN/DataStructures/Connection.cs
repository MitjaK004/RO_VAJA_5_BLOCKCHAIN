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
        private readonly byte[] SEND_LEDGER_SIGNAL = new byte[] { 120, 128, 250, 255, 120 };
        private readonly byte[] SEND_LEDGER_SIGNAL_RECV_READY = new byte[] { 255, 128, 255, 128, 255 };
        private readonly byte[] RECV_LEDGER_SIGNAL = new byte[] { 250, 120, 250, 120, 250 };
        private readonly byte[] RECV_READY = new byte[] { 123, 45, 67, 89, 10 };
        private readonly byte[] DATA_OK = new byte[] { 45, 45, 10, 89, 123 };
        private readonly byte[] DATA_ERR = new byte[] { 10, 10, 10, 10, 10 };
        private readonly byte[] CONTINUE = new byte[] { 22, 11, 77, 66, 55 };
        public static string LocalNodeId = "0";
        public static int MaxBufferSize = 4096;
        public static int defaultLocalPort = 25351;
        public static bool NewDataRecieved = false;
        //private ObservableCollection<Block> Ledger;
        public static Action Pause = () => { };
        public static Action Resume = () => { };
        private Action<ObservableCollection<Block>> LedgerSetter;
        private Func<ObservableCollection<Block>> LedgerGetter;
        private List<byte[]> recievedData = new List<byte[]>();
        public bool Error { get; private set; } = false;
        public Node _localNode { get; private set; } = new Node(LocalNodeId);
        public Node _remoteNode { get; private set; }
        public bool ConncetionRunning = false;
        public event PropertyChangedEventHandler? PropertyChanged;
        public bool Disconected = false;

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
            InstantSendSync_NoConfirm(ledgerSize);
            int a = 0;
            foreach (Block block in ledger)
            {
                InstantSendSync(block.ToByteArray());
                //MessageBox.Show(block.ToString());
                a++;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                recievedData.Clear();
            });
        }
        private void RecieveLedger() { 
            ObservableCollection<Block> ledger = new ObservableCollection<Block>();
            int ledgerSize = BitConverter.ToInt32(InstantRecieveSync_NoConfirm(), 0);
            for (int i = 0; i < ledgerSize; i++)
            {
                byte[] data = InstantRecieveSync();
                Block block = new Block(data);
                //MessageBox.Show(block.ToString());
                ledger.Add(block);
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                LedgerSetter(ledger);
                recievedData.Clear();
            });
        }
        private Task Reciever()
        {
            while (ConncetionRunning)
            {
                try
                {
                    byte[] data = new byte[MaxBufferSize];
                    byte[] confirmation = new byte[1];
                    confirmation[0] = 1;
                    int bytesRead = remoteClientStream.Read(data, 0, data.Length);
                    Array.Resize(ref data, bytesRead);
                    if (data.SequenceEqual(SEND_LEDGER_SIGNAL))
                    {
                        //MessageBox.Show("SEND_LEDGER_SIGNAL");
                        Application.Current.Dispatcher.Invoke(() => { Pause(); });
                        InstantSendSync_NoConfirm(RECV_LEDGER_SIGNAL);
                        InstantRecieveSync_NoConfirm();
                        SendLedger();
                        Application.Current.Dispatcher.Invoke(() => { Resume(); });
                    }
                    else if (data.SequenceEqual(RECV_LEDGER_SIGNAL))
                    {
                        //MessageBox.Show("RECV_LEDGER_SIGNAL");
                        Application.Current.Dispatcher.Invoke(() => { Pause(); });
                        InstantSendSync_NoConfirm(SEND_LEDGER_SIGNAL_RECV_READY);
                        RecieveLedger();
                        Application.Current.Dispatcher.Invoke(() => { Resume(); });
                    }
                    else if (data.SequenceEqual(SEND_LEDGER_SIGNAL_RECV_READY))
                    {
                        //MessageBox.Show("SEND_LEDGER_SIGNAL_RECV_READY");
                        Application.Current.Dispatcher.Invoke(() => { Pause(); });
                        SendLedger();
                        Application.Current.Dispatcher.Invoke(() => { Resume(); });
                    }
                    else
                    {
                        recievedData.Add(data);
                        remoteClient.GetStream().Write(confirmation, 0, confirmation.Length);
                        NewDataRecieved = true;
                    }
                }
                catch (Exception ex)
                {
                    ConncetionRunning = false;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        RemoteNode.NodeDisconnected();
                        Disconected = true;
                        Resume();
                    });
                }
            }
            return Task.CompletedTask;
        }
        public void Send(byte[] data)
        {
            Task.Run(() => InstantSend(data));
            return;
        }
        private Task InstantSend(byte[] data)
        {
            if (!Disconected)
            {
                try
                {
                    byte[] confirmation = new byte[1];
                    confirmation[0] = 0;
                    while (confirmation[0] == 0)
                    {
                        client.GetStream().Write(data, 0, data.Length);
                        client.GetStream().Read(confirmation, 0, confirmation.Length);
                    }
                }
                catch { }
            }
            return Task.CompletedTask;
        }
        private void InstantSendSync(byte[] data)
        {
            if (!Disconected)
            {
                try
                {
                    byte[] confirmation = new byte[5];
                    client.GetStream().Read(confirmation, 0, confirmation.Length);
                    while (!confirmation.SequenceEqual(DATA_OK))
                    {
                        client.GetStream().Write(data, 0, data.Length);
                        client.GetStream().Read(confirmation, 0, confirmation.Length);
                    }
                    client.GetStream().Write(CONTINUE, 0, CONTINUE.Length);
                }
                catch { }
            }
        }
        private byte[] InstantRecieveSync()
        {
            byte[] data = new byte[MaxBufferSize];
            if (!Disconected)
            {
                try
                {
                    byte[] confirmation = new byte[5];
                    remoteClientStream.Write(RECV_READY, 0, RECV_READY.Length);
                    int bytesRecieved = remoteClientStream.Read(data, 0, data.Length);
                    Array.Resize(ref data, bytesRecieved);
                    remoteClientStream.Write(DATA_OK, 0, DATA_OK.Length);
                    remoteClientStream.Read(confirmation, 0, confirmation.Length);
                }
                catch { }
            }
            return data;
        }
        private void InstantSendSync_NoConfirm(byte[] data)
        {
            if (!Disconected)
            {
                try
                {
                    client.GetStream().Write(data, 0, data.Length);
                }
                catch { }
            }
        }
        private byte[] InstantRecieveSync_NoConfirm()
        {
            byte[] data = new byte[MaxBufferSize];
            if (!Disconected)
            {
                try
                {
                    int bytesRecieved = remoteClientStream.Read(data, 0, data.Length);
                    Array.Resize(ref data, bytesRecieved);
                }
                catch { }
            }
            return data;
        }
        public void ReceveRemoteLedger()
        {
            if (!Disconected)
            {
                try
                {
                    InstantSendSync_NoConfirm(SEND_LEDGER_SIGNAL);
                }
                catch { }
            }
        }
        public void SendLocalLedger()
        {
            if (!Disconected)
            {
                try
                {
                    InstantSendSync_NoConfirm(RECV_LEDGER_SIGNAL);
                }
                catch { }
            }
        }
        
        private async void RunConnection(bool connectionEstablished)
        {
            try
            {
                if (!connectionEstablished)
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
                _ = Task.Run(() => Reciever());
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Node1"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Node2"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Connection"));
            } catch { }
        }
        private void ConnectToServer()
        {
            try
            {
                byte[] buffer = new byte[4096];
                client.Connect(_remoteNode.IPEndPoint);
                clientStream = client.GetStream();
                byte[] IdIpPort = Encoding.ASCII.GetBytes($"{LocalNodeId};{_localNode.IP.ToString()};{_localNode.Port.ToString()}");
                clientStream.Write(IdIpPort, 0, IdIpPort.Count());
                int br = clientStream.Read(buffer, 0, 4096);
                Array.Resize(ref buffer, br);
                string res = Encoding.UTF8.GetString(buffer);
                string[] remIdIpPort = res.Split(';');
                _remoteNode = new Node(remIdIpPort[0], remIdIpPort[1], int.Parse(remIdIpPort[2]));
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
                byte[] buffer2 = Encoding.ASCII.GetBytes($"{LocalNodeId};{_localNode.IP.ToString()};{_localNode.Port.ToString()}"); ;
                remoteClientStream.Write(buffer2, 0, buffer2.Length);
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
            try
            {
                defaultLocalPort = FindAvailablePort(defaultLocalPort);
                if (defaultLocalPort == -1)
                {
                    defaultLocalPort = FindAvailablePort(0);
                    if (defaultLocalPort == -1)
                    {
                        MessageBox.Show("No available ports", "ERROR");
                    }
                }
            }
            catch { }
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
