using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
            Ledger.CollectionChanged += Ledger_CollectionChanged;
        }

        private void Ledger_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.ScrollToBottom();
                }
            });
        }
    }
}
