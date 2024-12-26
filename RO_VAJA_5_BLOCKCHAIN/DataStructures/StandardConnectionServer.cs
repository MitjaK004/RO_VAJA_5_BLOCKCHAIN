using System;
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
    public class StandardConnectionServer : INotifyPropertyChanged
    {
        public string IP { get; set; } = "127.0.0.1";
        public int _port { get; set; } = 10548;
        public bool RunStdServer = false;
        public event PropertyChangedEventHandler? PropertyChanged;
        private System.Net.IPEndPoint IPEndPoint
        {
            get
            {
                return new System.Net.IPEndPoint(System.Net.IPAddress.Parse(IP), _port);
            }
        }
        public Node ToNode()
        {
            return new Node(IP, _port);
        }
        public StandardConnectionServer() { RunStdServer = true;  }
        public StandardConnectionServer(int port) { this._port = port; RunStdServer = true; }
        public async Task StartStandardServer(ObservableCollection<Connection> cl)
        {
            await Task.Run(async () =>
            {
                FindAvailablePort();
                while (RunStdServer)
                {
                    TcpListener server = new TcpListener(this.IPEndPoint);
                    server.Start();
                    TcpClient client = await server.AcceptTcpClientAsync();
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        cl.Add(new Connection(this.ToNode(), server, client));
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Connections"));
                    });
                    FindAvailablePort();
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Port"));
                    });
                }
            });
        }
        public void FindAvailablePort()
        {
            while (true)
            {
                try
                {
                    TcpListener listener = new TcpListener(System.Net.IPAddress.Parse(IP), _port);
                    listener.Start();
                    listener.Stop();
                    return;
                }
                catch
                {
                    _port++;
                }
            }
        }
        public int Port
        {
            get { return _port; }
        }
    }
}
