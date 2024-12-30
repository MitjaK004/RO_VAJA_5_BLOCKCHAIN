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
        private Blockchain _blockchain = new Blockchain();
        public ObservableCollection<Block> Ledger
        {
            get { return blockchain.Ledger; }
        }
        public Blockchain blockchain
        {
            get { return _blockchain; }
            set
            {
                _blockchain = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("blockchain"));
            }
        }
        public ViewModel() {
        }
    }
}
