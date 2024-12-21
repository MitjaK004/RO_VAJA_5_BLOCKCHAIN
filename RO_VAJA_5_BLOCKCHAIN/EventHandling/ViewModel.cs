using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RO_VAJA_5_BLOCKCHAIN.DataStructures;

namespace RO_VAJA_5_BLOCKCHAIN.EventHandling
{
    public class ViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private NetworkHandler _networkHandler = new NetworkHandler();
        public ObservableCollection<Block> Ledger
        {
            get { return networkHandler.Ledger; }
            set
            {
                networkHandler.Ledger = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Ledger"));
            }
        }
        public NetworkHandler networkHandler
        {
            get { return _networkHandler; }
            set
            {
                _networkHandler = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("networkHandler"));
            }
        }
        public ViewModel() {
            networkHandler.Ledger.Add(new Block(0, "Genesis Block", DateTime.Now, "0", "0"));
            networkHandler.StartStdServer();
        }
    }
}
