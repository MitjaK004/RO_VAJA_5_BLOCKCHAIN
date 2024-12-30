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
            VM.blockchain.AddBlock(new DataStructures.Block(VM.blockchain.Ledger.Count, RandomString(20), System.DateTime.Now, VM.blockchain.GetLastBlockHash()));
        }
        //function that generates a random string
        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            StringBuilder sb = new StringBuilder();
            Random random = new Random();
            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[random.Next(chars.Length)]);
            }
            return sb.ToString();
        }

        private void PauseBtn_Click(object sender, RoutedEventArgs e)
        {
            VM.blockchain.PauseMining();
        }

        private void ResumeBtn_Click(object sender, RoutedEventArgs e)
        {
            VM.blockchain.ResumeMining();
        }

        public void ScrollToBottom()
        {
            if (LedgerListView.Items.Count > 0)
            {
                var border = VisualTreeHelper.GetChild(LedgerListView, 0) as Decorator;
                if (border != null)
                {
                    var scrollViewer = border.Child as ScrollViewer;
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollToEnd();
                    }
                }
            }
        }
    }
}