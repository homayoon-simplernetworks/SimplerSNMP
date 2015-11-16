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


            ezp.setSystems(sy, "ez_systems.xml");

            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    (window as MainWindow).treeViewAdd(ipTextBox.Text);
                    (window as MainWindow).addRemoveTrapdestination(sy.Host, sy.Port, GetLocalIPAddress(), sy.Read_Community, sy.Write_Community, 4);
                }
                
            }





            this.Close();

        }

      
    }
}
