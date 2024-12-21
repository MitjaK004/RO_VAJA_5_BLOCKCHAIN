using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RO_VAJA_5_BLOCKCHAIN.DataStructures
{
    public class StandardConnectionServer
    {
        public string IP { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 10548;
        public bool RunStdServer = false;
        private System.Net.IPEndPoint IPEndPoint
        {
            get
            {
                return new System.Net.IPEndPoint(System.Net.IPAddress.Parse(IP), Port);
            }
        }
        public Node ToNode()
        {
            return new Node(IP, Port);
        }
        public StandardConnectionServer() { RunStdServer = true;  }
        public StandardConnectionServer(int port) { this.Port = port; RunStdServer = true; }
        public async Task StartStandardServer(ObservableCollection<Connection> cl)
        {
            FindAvailablePort();
            while (RunStdServer)
            {
                TcpListener server = new TcpListener(this.IPEndPoint);
                server.Start();
                TcpClient client = server.AcceptTcpClient();
                cl.Add(new Connection(this.ToNode(), server, client));
                FindAvailablePort();
            }
        }
        public void FindAvailablePort()
        {
            while (true)
            {
                try
                {
                    TcpListener listener = new TcpListener(System.Net.IPAddress.Parse(IP), Port);
                    listener.Start();
                    listener.Stop();
                    return;
                }
                catch
                {
                    Port++;
                }
            }
        }
    }
}
