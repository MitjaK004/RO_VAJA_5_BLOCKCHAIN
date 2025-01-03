using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_VAJA_5_BLOCKCHAIN.DataStructures
{
    public class DisplayLedgerItem : INotifyPropertyChanged
    {
        private Block _block;
        private bool _valid;
        private Color _color;
        public event PropertyChangedEventHandler? PropertyChanged = null;
        DisplayLedgerItem(Block block, bool valid)
        {
            _block = block;
            _valid = valid;
            _color = valid ? Color.Green : Color.Red;
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
        public Color Color
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
