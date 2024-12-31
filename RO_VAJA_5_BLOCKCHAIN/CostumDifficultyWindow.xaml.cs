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
    /// Interaction logic for CostumDifficultyWindow.xaml
    /// </summary>
    public partial class CostumDifficultyWindow : Window
    {
        public string Difficulty = String.Empty;
        public CostumDifficultyWindow()
        {
            InitializeComponent();
        }

        private void SetDiffBtn_Click(object sender, RoutedEventArgs e)
        {
            Difficulty = DiffTxt.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
