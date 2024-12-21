using RO_VAJA_5_BLOCKCHAIN.DataStructures;
using RO_VAJA_5_BLOCKCHAIN.EventHandling;
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
    /// Interaction logic for NodeManagerWindow.xaml
    /// </summary>
    public partial class NodeManagerWindow : Window
    {
        public ViewModel VM;
        public NodeManagerWindow(ViewModel _VM)
        {
            InitializeComponent();
            DataContext = VM = _VM;
        }

        private void AddNodeBtn_Click(object sender, RoutedEventArgs e)
        {
            AddNodeWindow ANW = new AddNodeWindow(Connection.StdServerPort);
            if (ANW.ShowDialog() == true)
            {
                VM.networkHandler.AddNode(ANW.node);
            }
        }
    }
}
