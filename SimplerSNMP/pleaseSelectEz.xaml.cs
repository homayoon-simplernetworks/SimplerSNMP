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

namespace SimplerSNMP
{
    /// <summary>
    /// Interaction logic for pleaseSelectEz.xaml
    /// </summary>
    public partial class pleaseSelectEz : Window
    {
        public pleaseSelectEz()
        {
            InitializeComponent();
        }

        private void closbutton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
