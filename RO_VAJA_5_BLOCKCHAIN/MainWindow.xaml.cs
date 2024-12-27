using RO_VAJA_5_BLOCKCHAIN.DataStructures;
using RO_VAJA_5_BLOCKCHAIN.EventHandling;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RO_VAJA_5_BLOCKCHAIN
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel VM = new ViewModel();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = VM;
        }


        private void AddNodeBtn_Click(object sender, RoutedEventArgs e)
        {
            AddNodeWindow ANW = new AddNodeWindow(10548);
            if (ANW.ShowDialog() == true)
            {
                VM.blockchain.AddNode(ANW.node);
            }
        }

        private void AddNewRandomBlock_Click(object sender, RoutedEventArgs e)
        {
            VM.blockchain.AddBlock(new DataStructures.Block(VM.blockchain.Ledger.Count, VM.blockchain.Difficulty, "RandomString(10)blallasdhasd", System.DateTime.Now, VM.blockchain.GetLastBlockHash()));
        }
    }
}