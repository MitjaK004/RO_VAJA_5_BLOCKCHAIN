using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;


namespace RO_VAJA_5_BLOCKCHAIN.DataStructures
{
    public class DisplayLedgerItem : INotifyPropertyChanged
    {
        private Block _block;
        private bool _valid;
        private Brush _color;
        public event PropertyChangedEventHandler? PropertyChanged = null;
        public DisplayLedgerItem(Block block, bool valid)
        {
            _block = block;
            _valid = valid;
            System.Windows.Media.Color Red = System.Windows.Media.Color.FromRgb(255, 0, 0);
            System.Windows.Media.Color White = System.Windows.Media.Color.FromRgb(255, 255, 255);
            _color = valid ? new SolidColorBrush(White) : new SolidColorBrush(Red);
        }
        public Block Block
        {
            get => _block;
            set
            {
                _block = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Block"));
            }
        }
        public bool Valid
        {
            get => _valid;
            set
            {
                _valid = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Valid"));
            }
        }
        public Brush Color
        {
            get => _color;
            set
            {
                _color = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color"));
            }
        }
    }
}
