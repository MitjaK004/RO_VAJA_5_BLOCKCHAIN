using RO_VAJA_5_BLOCKCHAIN.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RO_VAJA_5_BLOCKCHAIN
{
    /// <summary>
    /// Interaction logic for AddNodeWindow.xaml
    /// </summary>
    public partial class AddNodeWindow : Window
    {
        public Node node = new Node();
        public AddNodeWindow()
        {
            InitializeComponent();
            DataContext = node;
        }
        public AddNodeWindow(int DefaultPort)
        {
            InitializeComponent();
            DataContext = node;
            node.Port = DefaultPort;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
