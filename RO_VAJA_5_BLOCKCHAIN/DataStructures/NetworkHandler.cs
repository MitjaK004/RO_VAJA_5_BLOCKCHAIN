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
        public ObservableCollection<Connection> Connections { get; private set; } = new ObservableCollection<Connection>();
        public ObservableCollection<Block> _ledger = new ObservableCollection<Block>();
        public event PropertyChangedEventHandler? PropertyChanged;
        private StandardConnectionServer StdServer = new StandardConnectionServer(10548);
        public bool RunLedgerUpdate = false;

        public NetworkHandler() {}
        public NetworkHandler(int StdServerPort) {
            StdServer.Port = StdServerPort;
        }
        public int StdServerPort
        {
            get { return StdServer.Port; }
            set
            {
                StdServer.Port = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StdServerPort"));
            }
        }
        public bool AddNode(Node node)
        {
           if (Connections.Any(x => x.node2 == node))
           {
               return false;
           }
           Connection connection = new Connection(node);
           Connections.Add(connection);
           return true;
        }
        public void StartStdServer() {
           Task.Run(() => StdServer.StartStandardServer(Connections));
        }
        public bool SendBlock(Block block)
        {
           foreach (Connection connection in Connections)
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
               foreach (Connection connection in Connections)
               {
                   Ledger.Add(new Block(connection.PopRecieved()));
               }
           }
        }
    }
}
