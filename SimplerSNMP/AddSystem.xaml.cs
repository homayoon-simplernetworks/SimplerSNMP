using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
    /// Interaction logic for AddSystem.xaml
    /// </summary>
    public partial class AddSystem : Window
    {
        public AddSystem()
        {
            InitializeComponent();
            button.IsEnabled = false;
        }
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
        private void button_Click(object sender, RoutedEventArgs e)
        {
            XMLtoDataset xd = new XMLtoDataset();
            ezEdgeSystems sy = new ezEdgeSystems();
            ezSystemProvider ezp = new ezSystemProvider();
            sy.Host = ipTextBox.Text;
            sy.Port = Int16.Parse(portTestbox.Text);
            sy.Description = desTextbox.Text;
            sy.Read_Community = rcTextbox.Text;
            sy.Write_Community = wcTextbox.Text;
            sy.mibAddress = mibFileTextBox.Text;


            ezp.setSystems(sy, "ez_systems.xml");

            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    (window as MainWindow).treeViewAdd(ipTextBox.Text);
                    Task trapTableAdd = new Task(new Action(() => { (window as MainWindow).addRemoveTrapdestination(sy.Host, sy.Port, GetLocalIPAddress(), sy.Read_Community, sy.Write_Community, 4); }));
                    trapTableAdd.Start();
                    
                }
                
            }





            this.Close();

        }

        private void fileBrowser_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();



            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".xml";
            dlg.Filter = "XML Files (*.xml)|*.xml";


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                mibFileTextBox.Text = filename;
            }

            checkIfAllDataIsAvailable();

        }

        

        private void checkIfAllDataIsAvailable()
        {
            if (ipTextBox.Text != "" &&
             portTestbox.Text != "" &&
             desTextbox.Text != "" &&
             rcTextbox.Text != "" &&
             wcTextbox.Text != "" &&
             mibFileTextBox.Text != "")
            {
                button.IsEnabled = true;
            }
            else
            {
                button.IsEnabled = false;
            }

        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            checkIfAllDataIsAvailable();
        }
    }
}
