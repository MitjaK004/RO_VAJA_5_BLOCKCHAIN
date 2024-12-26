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
    public class NetworkHandler: INotifyPropertyChanged
    {
        public ObservableCollection<Connection> _connections { get; private set; } = new ObservableCollection<Connection>();
        public ObservableCollection<Block> _ledger = new ObservableCollection<Block>();
        public event PropertyChangedEventHandler? PropertyChanged;
        private StandardConnectionServer _stdServer = new StandardConnectionServer(10548);
        public bool RunLedgerUpdate = false;

        public NetworkHandler() {}
        public NetworkHandler(int StdServerPort) {
            _stdServer._port = StdServerPort;
        }
        public StandardConnectionServer StdServer
        {
            get { return _stdServer; }
            set
            {
                _stdServer = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StdServer"));
            }
        }
        public bool AddNode(Node node)
        {
           if (_connections.Any(x => x._node2 == node) && _connections.Any(x => x._node1 == node))
           {
               return false;
           }
           Connection connection = new Connection(node);
           Connections.Add(connection);
           return true;
        }
        public void StartStdServer() {
           Task.Run(() => _stdServer.StartStandardServer(_connections));
        }
        public bool SendBlock(Block block)
        {
           foreach (Connection connection in _connections)
           {
               connection.Send(block.ToByteArray());
           }
           return false;
        }
        public ObservableCollection<Block> Ledger
        {
           get { return _ledger; }
           set
           {
               _ledger = value;
               PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Ledger"));
           }
        }
        private async Task UpdateLedger()
        {
           RunLedgerUpdate = false;
           while (RunLedgerUpdate)
           {
               await Task.Delay(100);
               foreach (Connection connection in _connections)
               {
                   Ledger.Add(new Block(connection.PopRecieved()));
               }
           }
        }
        public ObservableCollection<Connection> Connections
        {
            get { return _connections; }
            set
            {
                _connections = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Connections"));
            }
        }
    }
}
