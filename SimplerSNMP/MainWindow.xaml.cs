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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using SnmpSharpNet;
using System.Net;
using Action = System.Action;
using System.Timers;
using System.Net.Sockets;
using System.Xml;
using System.Windows.Threading;
using System.Data;
using System.Collections.ObjectModel;
using System.Threading;
using System.Net.Mail;
using System.ComponentModel;
using System.Collections;
using System.Reflection;
using System.Globalization;
using Microsoft.Win32;
using System.Data.OleDb;
using System.Net.NetworkInformation;

namespace SimplerSNMP
{
    /// <summary>
    /// es-edge SNMP auto tester
    /// 
    /// </summary>
    /// 





    public class ezEdgeSystems
    {


        public string Host { get; set; }
        public int Port { get; set; }
        public string Description { get; set; }
        public string Read_Community { get; set; }
        public string Write_Community { get; set; }
        public string Identifier { get; set; }
        public string NTP_Server { get; set; }
        public string Boot_Loader_Version { get; set; }
        public string Image_Version { get; set; }
        public string AID { get; set; }
        public string Admin_State { get; set; }
        public string Oper_State { get; set; }
        public string Status { get; set; }
        public string Configuration { get; set; }
        public int Sub_Ports { get; set; }
        public int OE_Ports { get; set; }
        public int Bundle_Size { get; set; }
        public string mibAddress { get; set; }


    }
    public class ezSystemProvider
    {
        
        public void setSystems(ezEdgeSystems ez, string fn)
        {
            var eZsystems = new List<ezEdgeSystems>();
            DataTable dt = new DataTable();
            DataSet ds = new DataSet();

            //dt.ReadXml(fn);
            //dt.Rows.Find(ez.Host);
            ezEdgeSystems eze = new ezEdgeSystems();

            Type type = ez.GetType();
            PropertyInfo[] properties = type.GetProperties();

            try
            {
                ds.ReadXml(fn);
                dt = ds.Tables[0];
            }
            catch (Exception)
            {

                dt.TableName = "systems";
                foreach (PropertyInfo property in properties)
                {
                    //Console.WriteLine("Name: " + property.Name + ", Value: " + property.GetValue(ez, null));
                    dt.Columns.Add(property.Name);

                }
            }

            DataRow dr = dt.NewRow();

            foreach (PropertyInfo property in properties)
            {
                if (property.GetValue(ez, null) != null)
                {
                    dr[property.Name] = property.GetValue(ez, null);
                }
                else
                {
                    dr[property.Name] = " ";
                }


            }

            //dt.Columns.Add(ez);
            dt.Rows.Add(dr);
            dt.WriteXml(fn);

            

        }
        public void removeSystems(string rHost, string fn)
        {

            DataTable dt = new DataTable();
            DataSet ds = new DataSet();

            try
            {
                ds.ReadXml(fn);
                dt = ds.Tables[0];
            }
            catch (Exception)
            {
                return;
            }
            DataRow[] foundRows;
            foundRows = dt.Select("Host Like '" + rHost + "%'");


            foreach (var row in foundRows)
            {
                dt.Rows.Remove(row);
            }


            dt.WriteXml(fn);

        }
    }

    public class XMLtoDataset
    {
        private string fileName;
        private string tableName;

        public XMLtoDataset(string fn, string tn)
        {
            fileName = fn;
            tableName = tn;
        }

        public XMLtoDataset()
        {

        }
        public DataTable xmlReader(string fn, string tn)
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            ds.ReadXml(fn);
            dt = ds.Tables[tn];
            return dt;
        }

        public void xmlDumper(string fn, DataTable dt)
        {
            dt.WriteXml(fn);
        }
    }



    public partial class MainWindow : Window
    {
        public int SNMPtimeout { get; private set; }

        public int SNMPretry { get; private set; }

        public struct ezEdgeSystems1
        {
            public string Host;
            public int Port;
            public string Description;
            public string Read_Community;
            public string Write_Community;
            public string Identifier;
            public string NTP_Server;
            public string Boot_Loader_Version;
            public string Image_Version;
            public string AID;
            public string Admin_State;
            public string Oper_State;
            public string Status;
            public string Configuration;
            public int Sub_Ports;
            public int OE_Ports;
            public int Bundle_Size;
        }

        public struct serviceAlarms
        {
            public string AID;
            public string Description;

            public int systemLimitCritical;
            public int systemLimitMajor;
            public int systemLimitMinor;

            public int shelveLimitCritical;
            public int shelveLimitMajor;
            public int shelveLimitMinor;

            public int cardLimitCritical;
            public int cardLimitMajor;
            public int cardLimitMinor;

            public string priority;
            public bool isDefault;

        }

        public string prifixOID   { get; private set; } 


    public MainWindow()
        {
            InitializeComponent();
            SNMPtimeout = 15000; //to do: all variables will be load from variables.XML
            SNMPretry = 2;
            Thread t1 = new Thread(new ThreadStart(TrapRe));
            t1.IsBackground = true;
            t1.Start();
            Thread t2 = new Thread(new ThreadStart(syslogLogger));
            t2.IsBackground = true;
            t2.Start();

            /*

            Task task11 = new Task(() => { alarmAutoRefreshMethod(); });
            task11.Start();
            Task task12 = new Task(() => { xcAutoRefreshMethod(); });
            task12.Start();



            //xcAutoRefreshMethod();


            /* Thread t3 = new Thread(new ThreadStart(alarmAutoRefreshMethod));
             t3.IsBackground = true;
             t3.Start();

             Thread t4 = new Thread(new ThreadStart(xcAutoRefreshMethod));
             t4.IsBackground = true;
             t4.Start(); */

            sniTableId();

            prifixOID = "1.3.6.1.4.1";

            tableComboBox.ItemsSource = tableIdList;

            
            tableComboBox.DisplayMemberPath = "Key";
            tableComboBox.SelectedValuePath = "Value";
            tableComboBox.SelectedItem = tableComboBox.Items[0];




        }

        // add loges to UI
        // to do: loges should be added  to a text file 
        public void AppendTextToOutput(string text)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture);
            LogBox.Text = LogBox.Text + timestamp + ", " + text + "\r\n";
            
            LogBox.ScrollToEnd();
        }

        public DataRow[] columnProvider (string tableId, string fn)
        {
            XMLtoDataset xmlt = new XMLtoDataset();
            DataTable dt;
            dt = xmlt.xmlReader(fn, "column");

            DataRow[] dtt;
            dtt = dt.Select("row_Id =" + tableId);

            return dtt;
        }


        #region tree-view
        // add ez-edge systems to tree-view

        public bool IsSelected { get; set; }


        private List<ezEdgeSystems> ezSystemList = new List<ezEdgeSystems>();


        public ezEdgeSystems getEzSystems(string sHost)
        {
            
            foreach (ezEdgeSystems ez in ezSystemList)
                {
                    if (ez.Host == sHost)
                    {
                        return ez;
                    }
                }
            return ezSystemList[0];
        }

        public void treeVeiwRefresh()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                treeviewLoader(treeView, "ez_systems.xml");
            }));
            
        }

        private void treeviewLoader(TreeView tv, string fn)
        {
            DataTable dt = new DataTable();
            DataSet ds = new DataSet();      



            try
            {
                ds.ReadXml(fn);
                dt = ds.Tables[0];


                List<TreeViewItem> itl = new List<TreeViewItem>();

                tv.Items.Clear();



                foreach (DataRow dr in dt.Rows)
                {
                    TreeViewItem treeItem = null;                    
                    treeItem = new TreeViewItem();
                    ezEdgeSystems eze = new ezEdgeSystems();


                    treeItem.Header = dr["Host"];
                    
                    eze.Host = dr["Host"].ToString();
                    eze.Port = Int16.Parse( dr["Port"].ToString());                 ;
                    eze.Write_Community = dr["Write_Community"].ToString();
                    eze.Read_Community = dr["Read_Community"].ToString();
                    eze.mibAddress = dr["mibAddress"].ToString();


                    treeItem.IsSelected = true;
                    tv.Items.Add(treeItem);
                    ezSystemList.Add(eze);

                }

                // ... Get TreeView reference and add both items.
                TreeViewItem tvi = tv.ItemContainerGenerator.Items[0] as TreeViewItem;
                tvi.IsSelected = true;

            }
            catch (IOException IOE)
            {
                tv.Items.Clear();
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("Load treeview :  " + IOE.Message);
                }));
                AddSystem ad = new AddSystem();
                ad.Show();

            }
            catch (IndexOutOfRangeException ind)
            {
                tv.Items.Clear();
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("Load IndexOutOfRangeException :  " + ind.Message);
                }));
                AddSystem ad = new AddSystem();
                ad.Show();
            }
        }

        private void treeView_Loaded(object sender, RoutedEventArgs e)
        {
            var tree = sender as TreeView;
            treeviewLoader(tree, "ez_systems.xml");

        }

        public void treeViewAdd(string ip)
        {
            TreeViewItem treeItem = null;

            // North America
            treeItem = new TreeViewItem();
            treeItem.Header = ip;
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {

                treeView.Items.Add(treeItem);

            }));



        }

        private void removeSystem_Click(object sender, RoutedEventArgs e)
        {
            ezSystemProvider ezp = new ezSystemProvider();
            var item = treeView.SelectedItem as TreeViewItem;

            string rHost = item.Header.ToString();
            ezp.removeSystems(rHost, "ez_systems.xml");
            treeviewLoader(treeView, "ez_systems.xml");
            Task trapTableRemove = new Task(new Action(() => { addRemoveTrapdestination(rHost, 161, GetLocalIPAddress(), "public", "AdminPublic", 6); }));

            
            trapTableRemove.Start();
            
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var tree = sender as TreeView;

            // ... Determine type of SelectedItem.
            if (tree.SelectedItem is TreeViewItem)
            {
                // ... Handle a TreeViewItem.
                var item = tree.SelectedItem as TreeViewItem;
                this.Title = "Selected System: " + item.Header.ToString();
                //selectionChangedMethod();
            }
            else if (tree.SelectedItem is string)
            {
                // ... Handle a string.
                this.Title = "Selected: " + tree.SelectedItem.ToString();
            }

        }

        private void adNewSystem_Click(object sender, RoutedEventArgs e)
        {


            AddSystem ad = new AddSystem();
            ad.Show();
        } 



        #endregion


        #region cross connections

        //cross connections
        private string connectionTable = "1.3.6.1.4.1.4987.1.1.1.";



        private void fileBrowser_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();



            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".xlsx";
            dlg.Filter = "XML Files (*.xlsx)|*.xlsx";


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                xcFileTextBox.Text = filename;
                

            }
        }

        private void importXCButton_Click(object sender, RoutedEventArgs e)
        {
            loadXls(xcFileTextBox.Text);
        }

        public void loadXls(string filename)
        {

          try
            {
                ExcelDataService exSer = new ExcelDataService();

                ObservableCollection<XcConnectionLine> xcConnectionLines = new ObservableCollection<XcConnectionLine>();
                xcConnectionLines = exSer.loadExcel(filename);
                ezEdgeSystems ez;
                ez = currentEzEdgeSystem();


                Task task = new Task(() => { createCrossConnection(xcConnectionLines, ez); });
                task.Start();
            }
            catch (Exception ex)
            {

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("create jumper using excel file:  " + ex.Message);
                }));

            }


        }

        public string ConnectionTable
        {
            get
            {
                return connectionTable;
            }

            set
            {
                connectionTable = value;
            }
        }
        public string crossConnection(string ipAdd, int sPort, string originCard, string originPort, string destCard, string destPort , string serviceID, string cirutId , string tableOid, string WriteComminuty , int addDel)
        {
            // Prepare target
            UdpTarget target = new UdpTarget((IPAddress)new IpAddress(ipAdd), sPort, SNMPtimeout, SNMPretry);
            // Create a SET PDU
            Pdu pdu = new Pdu(PduType.Set);



            // Set sysLocation.0 to a new string
            originCard += ".";
            originPort += ".";
            destCard += ".";
            destPort += ".";
            string crossJumper = originCard + originPort + destCard + destPort;

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                AppendTextToOutput("create jumper :  (" + ipAdd + ") " + crossJumper);
            }));


            pdu.VbList.Add(new Oid(tableOid + "16." + crossJumper + serviceID), new Integer32(addDel));

            if (addDel == 4)
            {
                // Set a value to integer
                pdu.VbList.Add(new Oid(tableOid + "6." + crossJumper + serviceID), new Integer32(0));
                // Set a value to unsigned integer
                pdu.VbList.Add(new Oid(tableOid + "7." + crossJumper + serviceID), new Integer32(0));
                pdu.VbList.Add(new Oid(tableOid + "12." + crossJumper + serviceID), new OctetString(cirutId));
            }
            

            // Set Agent security parameters
            AgentParameters aparam = new AgentParameters(SnmpVersion.Ver2, new OctetString(WriteComminuty));

            // Response packet
            SnmpV2Packet response;
            try
            {
                // Send request and wait for response
                response = target.Request(pdu, aparam) as SnmpV2Packet;

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("response :  (" + ipAdd + ") " + response);
                }));

            }
            catch (Exception ex)
            {
                // If exception happens, it will be returned here

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput(System.String.Format("Request failed with exception: {0} (" + ipAdd + ") ", ex.Message));
                }));
                return "error";
            }
            // Make sure we received a response
            if (response == null)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("Error in sending SNMP request. (" + ipAdd + ") ");
                }));
                return "error";
            }
            else
            {
                // Check if we received an SNMP error from the agent
                if (response.Pdu.ErrorStatus != 0)
                {

                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        AppendTextToOutput(System.String.Format("(" + ipAdd + ")SNMP agent returned ErrorStatus {0} on index {1}",
                        response.Pdu.ErrorStatus, response.Pdu.ErrorIndex));
                    }));
                    return "error";
                }
                else
                {
                    // Everything is ok. Agent will return the new value for the OID we changed
                    return response.ToString();

                }
            }
        }

        public string tapConnection(string ipAdd, int sPort, string originCard, string originPort, string destCard, string destPort, string tableOid, string WriteComminuty, string tapOut , string tapIn )
        {
            // Prepare target
            UdpTarget target = new UdpTarget((IPAddress)new IpAddress(ipAdd), sPort, SNMPtimeout, SNMPretry);
            // Create a SET PDU
            Pdu pdu = new Pdu(PduType.Set);



            // Set sysLocation.0 to a new string
            originCard += ".";
            originPort += ".";
            destCard += ".";
            destPort += ".";
            string crossJumper = originCard + originPort + destCard + destPort;

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                AppendTextToOutput("create Tap :  (" + ipAdd + ") " + crossJumper);
            }));




            try
            {
                // Set a value to integer
                int itapout = Int32.Parse(tapOut.Trim());
                pdu.VbList.Add(new Oid(tableOid + "6." + crossJumper + "2"), new Integer32(itapout));
            }
            catch
            {
                pdu.VbList.Add(new Oid(tableOid + "6." + crossJumper + "2"), new Integer32(0));
            }


            try
            {
                // Set a value to unsigned integer
                int itapin = Int32.Parse(tapIn.Trim());
                pdu.VbList.Add(new Oid(tableOid + "7." + crossJumper + "2"), new Integer32(itapin));
            }
            catch
            {
                pdu.VbList.Add(new Oid(tableOid + "7." + crossJumper + "2"), new Integer32(0));
            }


            // Set Agent security parameters
            AgentParameters aparam = new AgentParameters(SnmpVersion.Ver2, new OctetString(WriteComminuty));

            // Response packet
            SnmpV2Packet response;
            try
            {
                // Send request and wait for response
                response = target.Request(pdu, aparam) as SnmpV2Packet;

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("response :  (" + ipAdd + ") " + response);
                }));

            }
            catch (Exception ex)
            {
                // If exception happens, it will be returned here

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput(System.String.Format("Request failed with exception: {0} (" + ipAdd + ") ", ex.Message));
                }));
                return "error";
            }
            // Make sure we received a response
            if (response == null)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("Error in sending SNMP request. (" + ipAdd + ") ");
                }));
                return "error";
            }
            else
            {
                // Check if we received an SNMP error from the agent
                if (response.Pdu.ErrorStatus != 0)
                {

                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        AppendTextToOutput(System.String.Format("(" + ipAdd + ")SNMP agent returned ErrorStatus {0} on index {1}",
                        response.Pdu.ErrorStatus, response.Pdu.ErrorIndex));
                    }));
                    return "error";
                }
                else
                {
                    // Everything is ok. Agent will return the new value for the OID we changed
                    return response.ToString();

                }
            }
        }


        public string crossConnectionDel(string ipAdd, int sPort, string originCard, string originPort, string destCard, string destPort)
        {
            // Prepare target
            UdpTarget target = new UdpTarget((IPAddress)new IpAddress(ipAdd), sPort, 15000, 0);
            // Create a SET PDU
            Pdu pdu = new Pdu(PduType.Set);



            // Set sysLocation.0 to a new string
            originCard += ".";
            originPort += ".";
            destCard += ".";
            destPort += ".";
            string crossJumper = originCard + originPort + destCard + destPort;

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                AppendTextToOutput("delete jumper :  (" + ipAdd + ") " + crossJumper);
            }));


            pdu.VbList.Add(new Oid(connectionTable + "16." + originCard + originPort + destCard + destPort + "4"), new Integer32(6));

            // Set Agent security parameters
            AgentParameters aparam = new AgentParameters(SnmpVersion.Ver2, new OctetString("AdminPublic"));

            // Response packet
            SnmpV2Packet response;
            try
            {
                // Send request and wait for response
                response = target.Request(pdu, aparam) as SnmpV2Packet;

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("response :  (" + ipAdd + ") " + response);
                }));

            }
            catch (Exception ex)
            {
                // If exception happens, it will be returned here

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput(System.String.Format("Request failed with exception: {0} (" + ipAdd + ") ", ex.Message));
                }));
                return "error";
            }
            // Make sure we received a response
            if (response == null)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("Error in sending SNMP request. (" + ipAdd + ") ");
                }));
                return "error";
            }
            else
            {
                // Check if we received an SNMP error from the agent
                if (response.Pdu.ErrorStatus != 0)
                {

                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        AppendTextToOutput(System.String.Format(" (" + ipAdd + ") SNMP agent returned ErrorStatus {0} on index {1}",
                        response.Pdu.ErrorStatus, response.Pdu.ErrorIndex));
                    }));
                    return "error";
                }
                else
                {
                    // Everything is ok. Agent will return the new value for the OID we changed
                    return response.ToString();

                }
            }
        }


        private void createCrossConnection(ObservableCollection<XcConnectionLine> xcConnectionLines , ezEdgeSystems ez)
        {

            string result;
                     

            foreach (XcConnectionLine xc in xcConnectionLines)
            {
                if (xc.command.ToLower().Equals("create"))
                {
                    result = crossConnection(ez.Host, ez.Port, xc.fromCard, xc.FromPort, xc.toCard, xc.toPort, xc.service, xc.circuitId, ConnectionTable, ez.Write_Community, 4);
                }
                else if (xc.command.ToLower().Equals("delete"))
                {
                    result = crossConnection(ez.Host, ez.Port, xc.fromCard, xc.FromPort, xc.toCard, xc.toPort, xc.service, xc.circuitId, ConnectionTable, ez.Write_Community, 6);
                }
                else if (xc.command.ToLower().Equals("update"))
                {

                }
            }


            
        }

        private void createCrossConnection(ezEdgeSystems ez, string originCard, string originPort, string destCard, string destPort, string crossNumber)
        { 

            string result;
            string serviceID = "2";
            string cirutId = "test";


            for (int i = 0; i < Int16.Parse(crossNumber); i++)

            {

                result = crossConnection(ez.Host, ez.Port , originCard, originPort, destCard, destPort , serviceID, cirutId,  ConnectionTable, ez.Write_Community , 4);
                originPort = (Int16.Parse(originPort) + 1).ToString();
                destPort = (Int16.Parse(destPort) + 1).ToString();


            }
        }
        private void createCrossConnectionDel(string ipAdress, int sPort, string originCard, string originPort, string destCard, string destPort, string crossNumber)
        { /*
            string originCard = fromCard.Text;
            string originPort = fromPort.Text;
            string destCard = toCard.Text;
            string destPort = toPort.Text;
            */


            string result;
            for (int i = 0; i < Int16.Parse(crossNumber); i++)

            {

                result = crossConnectionDel(ipAdress, sPort, originCard, originPort, destCard, destPort);
                originPort = (Int16.Parse(originPort) + 1).ToString();
                destPort = (Int16.Parse(destPort) + 1).ToString();


            }
        }

        private ezEdgeSystems currentEzEdgeSystem()
        {
            var item = treeView.SelectedItem as TreeViewItem;
            string ipAddress = item.Header.ToString();

            foreach (ezEdgeSystems ez in ezSystemList)
            {
                if (ez.Host == ipAddress)
                {
                    return ez;
                }
            }
            return ezSystemList[0];

        }

        private void createCross_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string originCard = fromCard.Text;
                string originPort = fromPort.Text;
                string destCard = toCard.Text;
                string destPort = toPort.Text;
                string scrossNumber = crossNumber.Text;

                //string ipAdress = "192.168.3.115";

                ezEdgeSystems ez;
                ez = currentEzEdgeSystem();


                Task task = new Task(() => { createCrossConnection(ez, originCard, originPort, destCard, destPort, scrossNumber); });
                task.Start();
            }
            catch (Exception)
            {


                pleaseSelectEz pl = new pleaseSelectEz();
                pl.Show();
            }
        }

        private void delCross_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string originCard = fromCard.Text;
                string originPort = fromPort.Text;
                string destCard = toCard.Text;
                string destPort = toPort.Text;
                string scrossNumber = crossNumber.Text;

                //string ipAdress = "192.168.3.115";
                var item = treeView.SelectedItem as TreeViewItem;

                string ipAdress = item.Header.ToString();
                int sPort = 161;

                Task task = new Task(() => { createCrossConnectionDel(ipAdress, sPort, originCard, originPort, destCard, destPort, scrossNumber); });
                task.Start();
            }
            catch (Exception)
            {

                pleaseSelectEz pl = new pleaseSelectEz();
                pl.Show();
            }
        }

        public DataTable tableBrowserNext_del(string tHost, int tPort, string tCommunity, string tOID, int columnNumber)
        {
            // SNMP community name
            OctetString community = new OctetString(tCommunity);

            // Define agent parameters class
            AgentParameters param = new AgentParameters(community);
            // Set SNMP version to 1
            param.Version = SnmpVersion.Ver1;
            // Construct the agent address object
            // IpAddress class is easy to use here because
            //  it will try to resolve constructor parameter if it doesn't
            //  parse to an IP address
            IpAddress agent = new IpAddress(tHost);

            // Construct target
            UdpTarget target = new UdpTarget((IPAddress)agent, tPort, SNMPtimeout, SNMPretry);

            // Define Oid that is the root of the MIB
            //  tree you wish to retrieve
            Oid rootOid = new Oid(tOID); // ifDescr

            // This Oid represents last Oid returned by
            //  the SNMP agent
            Oid lastOid = (Oid)rootOid.Clone();


            //list of variables in each column 
            List<string> columnList = new List<string>();
            DataTable dataTable = new DataTable();
            List<Oid> lastOidList = new List<Oid>();
            for (int i = 1; i <= columnNumber; i++)
            {
                lastOid.Add(i);
                lastOidList.Add(lastOid);
                lastOid = (Oid)rootOid.Clone();
                dataTable.Columns.Add("column" + i.ToString());
            }


            // Pdu class used for all requests
            Pdu pdu = new Pdu(PduType.GetNext);

            // Loop through results
            while (lastOid != null)
            {
                // When Pdu class is first constructed, RequestId is set to a random value
                // that needs to be incremented on subsequent requests made using the
                // same instance of the Pdu class.
                if (pdu.RequestId != 0)
                {
                    pdu.RequestId += 1;
                }
                // Clear Oids from the Pdu class.
                pdu.VbList.Clear();
                // Initialize request PDU with the last retrieved Oid


                pdu.VbList.Add(lastOidList);

                // Make SNMP request
                SnmpV1Packet result = null;

                try
                {
                    result = (SnmpV1Packet)target.Request(pdu, param);
                }
                catch (Exception e)
                {

                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        AppendTextToOutput("table browser : " + e.Message);
                    }));
                    lastOid = null;
                }
                // You should catch exceptions in the Request if using in real application.

                // If result is null then agent didn't reply or we couldn't parse the reply.
                if (result != null)
                {
                    // ErrorStatus other then 0 is an error returned by 
                    // the Agent - see SnmpConstants for error definitions
                    if (result.Pdu.ErrorStatus != 0)
                    {
                        // agent reported an error with the request


                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            AppendTextToOutput("table browser : Error in SNMP reply.Error " + result.Pdu.ErrorStatus + " : " + result.Pdu.ErrorIndex);
                        }));

                        lastOid = null;
                        break;
                    }
                    else
                    {
                        // Walk through returned variable bindings
                        lastOidList.Clear();
                        columnList.Clear();
                        foreach (Vb v in result.Pdu.VbList)
                        {
                            // Check that retrieved Oid is "child" of the root OID
                            if (rootOid.IsRootOf(v.Oid))
                            {





                                columnList.Add(v.Value.ToString());
                                lastOid = v.Oid;
                                lastOidList.Add(lastOid);
                            }
                            else
                            {
                                // we have reached the end of the requested
                                // MIB tree. Set lastOid to null and exit loop
                                lastOid = null;
                            }

                        }

                        if (lastOid != null)
                        {
                            dataTable.Rows.Add(columnList.ToArray());
                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                AppendTextToOutput("list of all cross : " + String.Join(",", columnList.ToArray()));
                            }));
                        }

                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        AppendTextToOutput("table browser : No response received from SNMP agent.");
                    }));
                }

            }
            target.Close();
            return dataTable;
        }

        private void delCrossAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = treeView.SelectedItem as TreeViewItem;
                string ipAddress = item.Header.ToString();
                Task task = new Task(() => { dellAllCross(ipAddress); });
                task.Start();
            }
            catch (Exception)
            {


                pleaseSelectEz pl = new pleaseSelectEz();
                pl.Show();
            }
        }
        public void dellAllCross(string ipAddress)
        {

            int sPort = 161;
            string originCard;
            string originPort;
            string destCard;
            string destPort;

            string result;
            DataTable dt = new DataTable();

            dt = tableBrowserNext_del(ipAddress, 161, "public", "1.3.6.1.4.1.4987.1.1.1", 16);
            foreach (DataRow dr in dt.Rows)
            {
                originCard = dr[0].ToString();
                originPort = dr[1].ToString();
                destCard = dr[2].ToString();
                destPort = dr[3].ToString();

                result = crossConnectionDel(ipAddress, sPort, originCard, originPort, destCard, destPort);

            }
        }

        #endregion


        #region Trap
        //Trap 
        public void AppendTextToTrapLogger(string text)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff",
                                           CultureInfo.InvariantCulture);

            trapLoggerTextBox.AppendText(timestamp + ",   " + text + "\r\n");
            trapLoggerTextBox.ScrollToEnd();
        }
        public void TrapRe()
        {
            // Construct a socket and bind it to the trap manager port 162 
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 1162);
            EndPoint ep = (EndPoint)ipep;
            socket.Bind(ep);
            // Disable timeout processing. Just block until packet is received 
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 0);
            bool run = true;
            int inlen = -1;
            while (run)
            {
                byte[] indata = new byte[16 * 1024];
                // 16KB receive buffer int inlen = 0;
                IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
                EndPoint inep = (EndPoint)peer;
                try
                {
                    inlen = socket.ReceiveFrom(indata, ref inep);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception {0}", ex.Message);
                    inlen = -1;
                }
                if (inlen > 0)
                {
                    // Check protocol version int 
                    int ver = SnmpPacket.GetProtocolVersion(indata, inlen);
                    if (ver == (int)SnmpVersion.Ver1)
                    {
                        // Parse SNMP Version 1 TRAP packet 
                        SnmpV1TrapPacket pkt = new SnmpV1TrapPacket();
                        pkt.decode(indata, inlen);
                        Console.WriteLine("** SNMP Version 1 TRAP received from {0}:", inep.ToString());
                        Console.WriteLine("*** Trap generic: {0}", pkt.Pdu.Generic);
                        Console.WriteLine("*** Trap specific: {0}", pkt.Pdu.Specific);
                        Console.WriteLine("*** Agent address: {0}", pkt.Pdu.AgentAddress.ToString());
                        Console.WriteLine("*** Timestamp: {0}", pkt.Pdu.TimeStamp.ToString());
                        Console.WriteLine("*** VarBind count: {0}", pkt.Pdu.VbList.Count);
                        Console.WriteLine("*** VarBind content:");
                        foreach (Vb v in pkt.Pdu.VbList)
                        {
                            Console.WriteLine("**** {0} {1}: {2}", v.Oid.ToString(), SnmpConstants.GetTypeName(v.Value.Type), v.Value.ToString());
                        }
                        Console.WriteLine("** End of SNMP Version 1 TRAP data.");
                    }
                    else
                    {
                        // Parse SNMP Version 2 TRAP packet 
                        SnmpV2Packet pkt = new SnmpV2Packet();
                        pkt.decode(indata, inlen);
                        string pinfo;


                        pinfo = "** SNMP Version 2 TRAP received from {0}:" + inep.ToString();
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            AppendTextToTrapLogger("** SNMP Version 2 TRAP received from {0}:" + inep.ToString());
                        }));

                        if ((SnmpSharpNet.PduType)pkt.Pdu.Type != PduType.V2Trap)
                        {
                            Console.WriteLine("*** NOT an SNMPv2 trap ****");
                        }
                        else
                        {
                           
                            pinfo += "\n " + "              *** Community: " + pkt.Community.ToString();
                            pinfo += "\n " + "              *** VarBind count: " + pkt.Pdu.VbList.Count;
                            pinfo += "\n " + "              *** VarBind content:";
                            

                            foreach (Vb v in pkt.Pdu.VbList)
                            {
                                pinfo += "\n " + "                          " + v.Oid.ToString() + "  "+ SnmpConstants.GetTypeName(v.Value.Type)+ " : " + v.Value.ToString();

                              
                            }

                            pinfo += "\n " + " * *End of SNMP Version 2 TRAP data.  \n   \n";

                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                AppendTextToTrapLogger(pinfo);
                            }));
                        }
                    }
                }
                else
                {
                    if (inlen == 0)
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            AppendTextToTrapLogger("Zero length packet received.");
                        }));
                }
            }
        }

        private string trapTable = "1.3.6.1.4.1.4987.1.11.5.1.3";

        public string TrapTable
        {
            get
            {
                return trapTable;
            }

            set
            {
                trapTable = value;
            }
        }

        public string addRemoveTrapdestination(string ipAdd, int sPort, string localIPaddress, string trapComminuty, string adminComminuty, int addRemove)
        {
            // Prepare target
            UdpTarget target = new UdpTarget((IPAddress)new IpAddress(ipAdd), sPort, 15000, 0);
            // Create a SET PDU
            Pdu pdu = new Pdu(PduType.Set);



            // Set sysLocation.0 to a new string



            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                AppendTextToOutput("add system to trap destination :  (" + ipAdd + ") ");
            }));


            pdu.VbList.Add(new Oid(TrapTable + "." + localIPaddress + ".6.112.117.98.108.105.99"), new Integer32(addRemove));


            // Set Agent security parameters
            AgentParameters aparam = new AgentParameters(SnmpVersion.Ver2, new OctetString("AdminPublic"));

            // Response packet
            SnmpV2Packet response;
            try
            {
                // Send request and wait for response
                response = target.Request(pdu, aparam) as SnmpV2Packet;

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("response :  (" + ipAdd + ") " + response);
                }));

            }
            catch (Exception ex)
            {
                // If exception happens, it will be returned here

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput(System.String.Format("Request failed with exception: {0} (" + ipAdd + ") ", ex.Message));
                }));
                return "error";
            }
            // Make sure we received a response
            if (response == null)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("Error in sending SNMP request. (" + ipAdd + ") ");
                }));
                return "error";
            }
            else
            {
                // Check if we received an SNMP error from the agent
                if (response.Pdu.ErrorStatus != 0)
                {

                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        AppendTextToOutput(System.String.Format("(" + ipAdd + ")SNMP agent returned ErrorStatus {0} on index {1}",
                        response.Pdu.ErrorStatus, response.Pdu.ErrorIndex));
                    }));
                    return "error";
                }
                else
                {
                    // Everything is ok. Agent will return the new value for the OID we changed
                    return response.ToString();

                }
            }
        }

        #endregion


        #region syslog
        //syslog

        public void AppendTextTosyslogLogger(string text)
        {
            if (text.Contains(syslogFilterTextbox.Text))
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture);
                syslogLoggerTextBox.AppendText(timestamp + ",   " + text + "\r\n");
                syslogLoggerTextBox.ScrollToEnd();
            }
        }

        public void syslogLogger()
        {
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
            UdpClient udpListener = new UdpClient(514);

            int loggerBuffer = 0;
            string tobelog;

            Tuple<string, string> tobelogTuple = new Tuple<string, string>("","");
            List <Tuple<string, string>> syslogList = new List<Tuple<string, string>>();
            byte[] bReceive; string sReceive; string sourceIP;
            

            /* Main Loop */
            /* Listen for incoming data on udp port 514 (default for SysLog events) */
            while (true)
            {
                try
                {
                    bReceive = udpListener.Receive(ref anyIP);
                    /* Convert incoming data from bytes to ASCII */
                    sReceive = Encoding.ASCII.GetString(bReceive);
                    /* Get the IP of the device sending the syslog */
                    sourceIP = anyIP.Address.ToString();
                    
                    new Thread(new logHandler(sourceIP, sReceive).handleLog).Start();
                                       

                    tobelog = sReceive.Replace(Environment.NewLine, "").Trim(); /* Syslog data */

                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        AppendTextTosyslogLogger(sourceIP + "  -->  " +tobelog);
                    }));



                    /* Start a new thread to handle received syslog event */
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        AppendTextToOutput("syslog exception: " + ex.ToString());
                    }));

                }
            }
        } //syslogLogger 


        #endregion


        #region table browser

        //to load table IDs from text file

        private Dictionary<string, string> tableIdList = new Dictionary<string, string>();
        public  void sniTableId()
        {
            try
            {
                using (StreamReader sr = new StreamReader("sniMibTableID.txt"))
                {
                    string line = "";
                    string[] tableAr;
                   
                    while (sr.Peek() >= 0)                        
                    {
                       line = sr.ReadLine();
                        if (line.Trim().StartsWith("#") || line.Trim() == "")
                        {
                            //read next nextline
                        }
                        else
                        {
                            tableAr =  line.Trim().Split(new Char[] { '=' });
                            tableIdList.Add(tableAr[0].Trim(),tableAr[1].Trim());

                        }

                       
                    }               
                   

                }
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("table browser error : (" + ex.Message + ") ");
                }));
            }
        }

        //to load table columns and OIds from XML file

        public async void tableBrowserNext(string tHost, int tPort, string tCommunity, string tOID, int columnNumber, DataGrid dg )
        {
            

            
            // SNMP community name
            OctetString community = new OctetString(tCommunity);

            // Define agent parameters class
            AgentParameters param = new AgentParameters(community);
            // Set SNMP version to 1
            param.Version = SnmpVersion.Ver1;
            // Construct the agent address object
            // IpAddress class is easy to use here because
            //  it will try to resolve constructor parameter if it doesn't
            //  parse to an IP address
            IpAddress agent = new IpAddress(tHost);

            // Construct target
            UdpTarget target = new UdpTarget((IPAddress)agent, tPort, SNMPtimeout, SNMPretry);

            // Define Oid that is the root of the MIB
            //  tree you wish to retrieve
            Oid rootOid = new Oid(tOID); // ifDescr

            // This Oid represents last Oid returned by
            //  the SNMP agent
            Oid lastOid = (Oid)rootOid.Clone();


            //list of variables in each column 
            List<string> columnList = new List<string>();
            DataTable dataTable = new DataTable();






            List<Oid> lastOidList = new List<Oid>();

            DataRow[] dr;
           // dr = columnProvider()


            for (int i = 1; i <= columnNumber; i++)
            {
                lastOid.Add(i);
                lastOidList.Add(lastOid);
                lastOid = (Oid)rootOid.Clone();


                dataTable.Columns.Add("column" + i.ToString());
            }
            //clean table 
            await Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                dg.ItemsSource = null;
                dg.IsEnabled = false;
                dg.ItemsSource = dataTable.DefaultView;
            }
            ));

            // Pdu class used for all requests
            Pdu pdu = new Pdu(PduType.GetNext);
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                AppendTextToOutput("Start getting table for :  " + tHost);
            }));

            try
            {
                // Loop through results
                while (lastOid != null)
                {
                    // When Pdu class is first constructed, RequestId is set to a random value
                    // that needs to be incremented on subsequent requests made using the
                    // same instance of the Pdu class.
                    if (pdu.RequestId != 0)
                    {
                        pdu.RequestId += 1;
                    }
                    // Clear Oids from the Pdu class.
                    pdu.VbList.Clear();
                    // Initialize request PDU with the last retrieved Oid


                    pdu.VbList.Add(lastOidList);

                    // Make SNMP request
                    SnmpV1Packet result = null;

                   

                        result = (SnmpV1Packet)target.Request(pdu, param);

                   
                   


                    // If result is null then agent didn't reply or we couldn't parse the reply.
                    if (result != null)
                    {



                        // ErrorStatus other then 0 is an error returned by 
                        // the Agent - see SnmpConstants for error definitions
                        if (result.Pdu.ErrorStatus != 0)
                        {
                            await
                                                    // agent reported an error with the request


                                                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                                                    {
                                                        AppendTextToOutput("table browser : Error in SNMP reply.Error  (" + tHost + ") " + result.Pdu.ErrorStatus + " : " + result.Pdu.ErrorIndex);
                                                    }));

                            lastOid = null;
                            break;
                        }
                        else
                        {
                            // Walk through returned variable bindings
                            lastOidList.Clear();
                            columnList.Clear();
                            foreach (Vb v in result.Pdu.VbList)
                            {
                                // Check that retrieved Oid is "child" of the root OID
                                if (rootOid.IsRootOf(v.Oid))
                                {





                                    columnList.Add(v.Value.ToString());
                                    lastOid = v.Oid;
                                    lastOidList.Add(lastOid);
                                }
                                else
                                {
                                    // we have reached the end of the requested
                                    // MIB tree. Set lastOid to null and exit loop



                                    lastOid = null;
                                }

                            }

                            if (lastOid != null)
                            {
                                dataTable.Rows.Add(columnList.ToArray());
                                
                               
                                    await Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                                    {
                                        dg.ItemsSource = null;
                                        dg.ItemsSource = dataTable.DefaultView;

                                    }));


                                





                            }

                        }
                    }
                    else
                    {
                        await Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                         {
                             AppendTextToOutput("table browser : No response received from SNMP agent. (" + tHost + ") ");
                         }));
                    }

                }

                 Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                      {
                          AppendTextToOutput("we have reached the end of SNMP requested MIB tree. ");
                      }));
                 Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    dg.IsEnabled = true;
                    

                }));


                //



                target.Close();
            }
            catch (Exception e)
            {
                await Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("table browser ((in)) : (" + tHost + ") " + e.Message);
                }));
                lastOid = null;
                return;
            }
            finally
            {
                target.Close();
               // Thread.CurrentThread.Abort();
            }

        }


        public  void tableBrowserNext(ezEdgeSystems ez ,string tableId, DataGrid dg)
        {
           

            //clean table 
             Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                dg.ItemsSource = null;
                dg.IsEnabled = false;
            }
            ));
            // SNMP community name
            OctetString community = new OctetString(ez.Read_Community);

            // Define agent parameters class
            AgentParameters param = new AgentParameters(community);
            // Set SNMP version to 1
            param.Version = SnmpVersion.Ver1;
            // Construct the agent address object
            // IpAddress class is easy to use here because
            //  it will try to resolve constructor parameter if it doesn't
            //  parse to an IP address
            IpAddress agent = new IpAddress(ez.Host);

            // Construct target
            UdpTarget target = new UdpTarget((IPAddress)agent, ez.Port, SNMPtimeout, SNMPretry);

            // Define Oid that is the root of the MIB
            //  tree you wish to retrieve
            Oid rootOid = new Oid(prifixOID); // ifDescr

            // This Oid represents last Oid returned by
            //  the SNMP agent
            Oid lastOid = (Oid)rootOid.Clone();


            //list of variables in each column 
            List<string> columnList = new List<string>();
            DataTable dataTable = new DataTable();






            List<Oid> lastOidList = new List<Oid>();

            DataRow[] dr;
            dr = columnProvider(tableId, ez.mibAddress);

            foreach(DataRow r in dr)
            {
                lastOid.Add(r["oid"].ToString().Remove(0, 2));
                lastOidList.Add(lastOid);
              
                lastOid = (Oid)rootOid.Clone();
                dataTable.Columns.Add(r["name"].ToString());

            }
            rootOid = new Oid(lastOidList[0].ToString().Remove(lastOidList[0].ToString().Length - 2));

           

            // Pdu class used for all requests
            Pdu pdu = new Pdu(PduType.GetNext);
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                AppendTextToOutput("Start getting table for :  " + ez.Host);
            }));

            try
            {
                // Loop through results
                while (lastOid != null)
                {
                    // When Pdu class is first constructed, RequestId is set to a random value
                    // that needs to be incremented on subsequent requests made using the
                    // same instance of the Pdu class.
                    if (pdu.RequestId != 0)
                    {
                        pdu.RequestId += 1;
                    }
                    // Clear Oids from the Pdu class.
                    pdu.VbList.Clear();
                    // Initialize request PDU with the last retrieved Oid


                    pdu.VbList.Add(lastOidList);

                    // Make SNMP request
                    SnmpV1Packet result = null;



                    result = (SnmpV1Packet)target.Request(pdu, param);





                    // If result is null then agent didn't reply or we couldn't parse the reply.
                    if (result != null)
                    {



                        // ErrorStatus other then 0 is an error returned by 
                        // the Agent - see SnmpConstants for error definitions
                        if (result.Pdu.ErrorStatus != 0)
                        {
                            // agent reported an error with the request
                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                            {
                              AppendTextToOutput("table browser : Error in SNMP reply.Error  (" + ez.Host + ") " + result.Pdu.ErrorStatus + " : " + result.Pdu.ErrorIndex);
                             }));

                            lastOid = null;
                            break;
                        }
                        else
                        {
                            // Walk through returned variable bindings
                            lastOidList.Clear();
                            columnList.Clear();
                            foreach (Vb v in result.Pdu.VbList)
                            {
                                // Check that retrieved Oid is "child" of the root OID
                                if (rootOid.IsRootOf(v.Oid))
                                {

                                    columnList.Add(v.Value.ToString());
                                    lastOid = v.Oid;
                                    lastOidList.Add(lastOid);
                                }
                                else
                                {
                                    // we have reached the end of the requested
                                    // MIB tree. Set lastOid to null and exit loop

                                    lastOid = null;
                                }

                            }

                            if (lastOid != null)
                            {
                                dataTable.Rows.Add(columnList.ToArray());
                                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                                {
                                   dg.ItemsSource = null;
                                   dg.ItemsSource = dataTable.DefaultView;

                                }));


                               





                            }

                        }
                    }
                    else
                    {
                         Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            AppendTextToOutput("table browser : No response received from SNMP agent. (" + ez.Host + ") ");
                        }));
                    }

                }

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("we have reached the end of SNMP requested MIB tree. ");
                }));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    dg.IsEnabled = true;


                }));


                //



                target.Close();
            }
            catch (Exception e)
            {
                 Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("table browser ((in)) : (" + ez.Host + ") " + e.Message);
                }));
                lastOid = null;
                return;
            }
            finally
            {
                target.Close();
                // Thread.CurrentThread.Abort();
            }

        }






        private void loadCrossTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = treeView.SelectedItem as TreeViewItem;

                string sHost = item.Header.ToString();
                string tableName = tableIdList["sniConnTable"];
                ezEdgeSystems ez = getEzSystems(sHost);
                xcListLable.Content = "System: " + sHost;

                Task tableTask = new Task(() => { tableBrowserNext(ez, tableName, tableBrowserGrid); });
                tableTask.Start();
            }
            catch (Exception)
            {

                pleaseSelectEz pl = new pleaseSelectEz();
                pl.Show();
            }



        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            
            try
            {
                var item = treeView.SelectedItem as TreeViewItem;

                string sHost = item.Header.ToString();
                string tableName = tableComboBox.SelectedValue.ToString();
                ezEdgeSystems ez = getEzSystems(sHost);
                tablesListLable.Content = "System: " + sHost;

                Task tableTask = new Task(() => { tableBrowserNext(ez , tableName, xmlTestGrid); });
                tableTask.Start();
            }
            catch (Exception)
            {

                pleaseSelectEz pl = new pleaseSelectEz();
                pl.Show();
            }



        }

        private void loadXcTableMethod()
        {
            var item = treeView.SelectedItem as TreeViewItem;
            string ipAdress = item.Header.ToString();
            if (xcTableThread != null )
            {
                //xcTableThread.Abort();
                 Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("****Thread abort ");
                }));
            }
            xcTableThread = new Thread(new ThreadStart(() => { tableBrowserNext(ipAdress, 161, "public", "1.3.6.1.4.1.4987.1.1.1", 16, tableBrowserGrid); }));

            xcTableThread.IsBackground = true;
            xcTableThread.Start();
        }

        private void loadAlarmTableMethod()
        {
            var item = treeView.SelectedItem as TreeViewItem;

            string ipAdress = item.Header.ToString();
            //if (systemAlarmThread != null && systemAlarmThread.IsAlive) systemAlarmThread.Abort();
            systemAlarmThread = new Thread(new ThreadStart(() => { tableBrowserNext(ipAdress, 161, "public", "1.3.6.1.4.1.4987.1.11.4.1", 6, tableBrowserAlarm); }));
            systemAlarmThread.IsBackground = true;
            systemAlarmThread.Start();
        }

        private void loadAlarmTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = treeView.SelectedItem as TreeViewItem;

                string sHost = item.Header.ToString();
                string tableName = tableIdList["sniActiveAlarmTable"];
                ezEdgeSystems ez = getEzSystems(sHost);
                alarmListLable.Content = "System: " + sHost;

                Task tableTask = new Task(() => { tableBrowserNext(ez, tableName, tableBrowserAlarm); });
                tableTask.Start();
                

            }

            catch (Exception)
            {

                pleaseSelectEz pl = new pleaseSelectEz();
                pl.Show();
            }
        }

        private Thread xcTableThread , systemAlarmThread;
        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            //selectionChangedMethod();

        }

        private async void selectionChangedMethod()
        {
            try
            {


                if ((XCtab.IsSelected == true && xcAutoRefreshCheckBox.IsChecked == true))
                {
                    loadXcTableMethod();
                    

                }
                else
                {

                    //if (xcTableThread != null && xcTableThread.IsAlive) xcTableThread.Abort();
                    await Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        tableBrowserGrid.ItemsSource = null;

                    }));
                }

                if (systemAlarmTab.IsSelected == true && alarmAutoRefreshCheckBox.IsChecked == true)
                {
                    loadAlarmTableMethod();

                }
                else
                {
                    //if (systemAlarmThread != null && systemAlarmThread.IsAlive)  systemAlarmThread.Abort();
                    await Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        tableBrowserAlarm.ItemsSource = null;
                        


                    }));

                }



            }
            catch (System.NullReferenceException ne)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("table browser : " + ne.Message);
                }));
                return;
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("table browser : " + ex.Message);
                }));
                pleaseSelectEz pl = new pleaseSelectEz();
                pl.Show();
            }
        }

        public string getIpAddress()
        {
            TreeViewItem item32;
            string ipAdress;
            item32 = treeView.SelectedItem as TreeViewItem;
            ipAdress = item32.Header.ToString();
            return ipAdress;
        }
       
        public  void alarmAutoRefreshMethod()
        {

            

            while ( alarmAutoRefreshCheckBox.IsChecked == true)
            {
                try
                {

                    TreeViewItem item32;
                    string ipAdress;
                    item32 = treeView.SelectedItem as TreeViewItem;
                    ipAdress = item32.Header.ToString();

                    Task task = new Task(() => { tableBrowserNext(ipAdress, 161, "public", "1.3.6.1.4.1.4987.1.11.4.1", 6, tableBrowserAlarm); });
                    task.Start();
                    task.Wait();

                    var t = Task.Run(async delegate
                    {
                        await Task.Delay(5000);
                        
                    });
                    t.Wait();
                    

                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        AppendTextToOutput(" AlarmAutoRefreshCheckBox : " + ex.Message);
                    }));

                }

            }

        }

        public void xcAutoRefreshMethod()
        {


            while (xcAutoRefreshCheckBox.IsChecked == true)
            {
                try
                {
                    var item = treeView.SelectedItem as TreeViewItem;

                    string ipAdress = item.Header.ToString();

                    Task task = new Task(() => { tableBrowserNext(ipAdress, 161, "public", "1.3.6.1.4.1.4987.1.1.1", 16, tableBrowserGrid); });
                    task.Start();
                    task.Wait();

                    var t = Task.Run(async delegate
                    {
                        await Task.Delay(5000);

                    });
                    t.Wait();


                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        AppendTextToOutput(" AlarmAutoRefreshCheckBox : " + ex.Message);
                    }));

                }

            }
            

            

        }









        #endregion

        private void fullTestButton_Click(object sender, RoutedEventArgs e)
        {
            var item = treeView.SelectedItem as TreeViewItem;
            string ipAdress = item.Header.ToString();
            bool isOnline = false;

            //check if system is online
            isOnline = isOnlinePingTest(ipAdress);
           
            if (isOnline)
            {
                ezEdgeSystems ez = new ezEdgeSystems();
                ez = getEzSystems(ipAdress);

                Thread fullTestThread = new Thread(new ThreadStart(() => { fullTestMethod(ez); }));
                fullTestThread.IsBackground = true;
                fullTestThread.Start();

            }
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput(ipAdress + " system is not on-line Ping result:  " + isOnline);
                    
                }));
            }
            
        }

        public void fullTestMethod(ezEdgeSystems ez)
        {
            DataTable ShelfTable, CardTable;
            string tableName = tableIdList["sniConnTable"];
            ShelfTable = tableBrowserNext(ez, tableIdList["sniEntityShelfTable"]);
            CardTable = tableBrowserNext(ez, tableIdList["sniEntityCardTable"]);
            string outputPath = @"C:\logs\syslog\test1.csv";
            outputCsvRow ocv = new outputCsvRow(outputPath, CardTable);
            ocv.addTable();

            /*
            /* Store the syslog using a new thread 
            new Thread(new outputCsvRow(outputPath + source + "_" + timestamp + "_syslog.csv", new string[] { source, log }).addRow).Start();
            //for (int i = 0; i < emailTriggers.Count(); i++) { if (log.Contains(emailTriggers[i])) { emailEvent(); } }
            /* Search for trigger strings and send email if found */

            return;


        }

        public bool isOnlinePingTest(string sHost)
        {
            bool result = false;            
            Ping pingSender = new Ping();
            IpAddress address = new IpAddress(sHost);                       
            PingReply reply = pingSender.Send((IPAddress)address);
                        
            if (reply.Status == IPStatus.Success)
            {
                result = true;
            }
            return result;
        }

        public IPAddress localIpAddress(string sHost)
        {

            IPAddress host = IPAddress.None;
            IPAddress ezIP = IPAddress.None;

            IpAddress address = new IpAddress(sHost);
            ezIP = (IPAddress)address;

            foreach (IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                host = ip;
                if (ip.AddressFamily == ezIP.AddressFamily)
                    break;
            }
            return host;
        }

        public DataTable tableBrowserNext(ezEdgeSystems ez, string tableId)
        {


            OctetString community = new OctetString(ez.Read_Community);

            // Define agent parameters class
            AgentParameters param = new AgentParameters(community);
            // Set SNMP version to 1
            param.Version = SnmpVersion.Ver1;
            // Construct the agent address object
            // IpAddress class is easy to use here because
            //  it will try to resolve constructor parameter if it doesn't
            //  parse to an IP address
            IpAddress agent = new IpAddress(ez.Host);

            // Construct target
            UdpTarget target = new UdpTarget((IPAddress)agent, ez.Port, SNMPtimeout, SNMPretry);

            // Define Oid that is the root of the MIB
            //  tree you wish to retrieve
            Oid rootOid = new Oid(prifixOID); // ifDescr

            // This Oid represents last Oid returned by
            //  the SNMP agent
            Oid lastOid = (Oid)rootOid.Clone();


            //list of variables in each column 
            List<string> columnList = new List<string>();
            DataTable dataTable = new DataTable();






            List<Oid> lastOidList = new List<Oid>();

            DataRow[] dr;
            dr = columnProvider(tableId, ez.mibAddress);

            foreach (DataRow r in dr)
            {
                lastOid.Add(r["oid"].ToString().Remove(0, 2));
                lastOidList.Add(lastOid);

                lastOid = (Oid)rootOid.Clone();
                dataTable.Columns.Add(r["name"].ToString());

            }
            rootOid = new Oid(lastOidList[0].ToString().Remove(lastOidList[0].ToString().Length - 2));



            // Pdu class used for all requests
            Pdu pdu = new Pdu(PduType.GetNext);
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                AppendTextToOutput("Start getting XC table for :  " + ez.Host);
            }));

            try
            {
                // Loop through results
                while (lastOid != null)
                {
                    // When Pdu class is first constructed, RequestId is set to a random value
                    // that needs to be incremented on subsequent requests made using the
                    // same instance of the Pdu class.
                    if (pdu.RequestId != 0)
                    {
                        pdu.RequestId += 1;
                    }
                    // Clear Oids from the Pdu class.
                    pdu.VbList.Clear();
                    // Initialize request PDU with the last retrieved Oid


                    pdu.VbList.Add(lastOidList);

                    // Make SNMP request
                    SnmpV1Packet result = null;



                    result = (SnmpV1Packet)target.Request(pdu, param);





                    // If result is null then agent didn't reply or we couldn't parse the reply.
                    if (result != null)
                    {



                        // ErrorStatus other then 0 is an error returned by 
                        // the Agent - see SnmpConstants for error definitions
                        if (result.Pdu.ErrorStatus != 0)
                        {
                            // agent reported an error with the request
                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                AppendTextToOutput("table browser : Error in SNMP reply.Error  (" + ez.Host + ") " + result.Pdu.ErrorStatus + " : " + result.Pdu.ErrorIndex);
                            }));

                            lastOid = null;
                            break;
                        }
                        else
                        {
                            // Walk through returned variable bindings
                            lastOidList.Clear();
                            columnList.Clear();
                            foreach (Vb v in result.Pdu.VbList)
                            {
                                // Check that retrieved Oid is "child" of the root OID
                                if (rootOid.IsRootOf(v.Oid))
                                {

                                    columnList.Add(v.Value.ToString());
                                    lastOid = v.Oid;
                                    lastOidList.Add(lastOid);
                                }
                                else
                                {
                                    // we have reached the end of the requested
                                    // MIB tree. Set lastOid to null and exit loop

                                    lastOid = null;
                                }

                            }

                            if (lastOid != null)
                            {
                                dataTable.Rows.Add(columnList.ToArray());
                             }

                        }
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            AppendTextToOutput("table browser : No response received from SNMP agent. (" + ez.Host + ") ");
                        }));
                    }

                }

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("we have reached the end of SNMP requested MIB tree. ");
                }));
               


                //



                target.Close();
                return dataTable;

            }
            catch (Exception e)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextToOutput("table browser ((in)) : (" + ez.Host + ") " + e.Message);
                }));
                lastOid = null;
                return dataTable;
            }
            finally
            {
                target.Close();
                // Thread.CurrentThread.Abort();
            }

        }

        private void SetTapButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                string originCard = fromCardTapTextBox.Text;
                string originPort = fromPortTapTextBox.Text;
                string destCard = toCardTapTextBox.Text;
                string destPort = toPortTapTextBox.Text;
                string tapOut = tapOutTapTextBox.Text;
                string tapIn = tapInTapTextBox.Text;

                

                ezEdgeSystems ez;
                ez = currentEzEdgeSystem();


                Task task = new Task(() => { tapConnection( ez.Host,  ez.Port, originCard, originPort,  destCard,  destPort, ConnectionTable,  ez.Write_Community, tapOut, tapIn); });
                task.Start();
            }
            catch (Exception)
            {


                pleaseSelectEz pl = new pleaseSelectEz();
                pl.Show();
            }
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

        
    } //main class

    class logHandler
    {
        /* Phrases within the syslog that will trigger an email notification */
        private string[] emailTriggers = new string[] { "link loss", "help please" };
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        private string outputPath = @"C:\logs\syslog\"; /* Location to store events */
        private string source; private string log;

        public logHandler(string sourceIP, string logData) /* Initialize object and clean up the raw data */
        {
            source = sourceIP.Trim(); /* Client IP */
            log = logData.Replace(Environment.NewLine, "").Trim(); /* Syslog data */
        }

        

        public void handleLog() /* Store the syslog and determine whether to trigger an email notification */
        {
            /* Store the syslog using a new thread */
            new Thread(new outputCsvRow(outputPath+source +"_" + timestamp + "_syslog.csv", new string[] { source, log }).addRow).Start();
            //for (int i = 0; i < emailTriggers.Count(); i++) { if (log.Contains(emailTriggers[i])) { emailEvent(); } }
            /* Search for trigger strings and send email if found */

            return;
        }

        private void emailEvent() /* Send email notification */
        {
            try
            {
                MailMessage notificationEmail = new MailMessage();
                notificationEmail.Subject = "SysLog Event";
                notificationEmail.IsBodyHtml = true;
                notificationEmail.Body = "<b>SysLog Event Triggered:<br/><br/>Time: </b><br/>" +
                    DateTime.Now.ToString() + "<br/><b>Source IP: </b><br/>" +
                    source + "< br />< b > Event: </ b >< br /> " + log; /* Throw in some basic HTML for readability */
                notificationEmail.From = new MailAddress("homayoon@simplernetworks.com", "SysLog Server"); /* From Address */
                notificationEmail.To.Add(new MailAddress("homayoon@simplernetworks.com", "Homayoon")); /* To Address */
                SmtpClient emailClient = new SmtpClient("10.10.10.10"); /* Address of your SMTP server of choice */
                                                                        //emailClient.UseDefaultCredentials = false; /* If your SMTP server requires credentials to send email */
                                                                        //emailClient.Credentials = new NetworkCredential(“username”, “password”); /* Supply User Name and Password */
                emailClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                emailClient.Send(notificationEmail); /* Send the email */
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            return;
        }
    }

    public class outputCsvRow 
    {
        private string formattedRow = null;
        private string outputPath = null;
        DataTable dataTable = null;

        public outputCsvRow(string filePath, string[] columns) /* Initialize object */
        {
            outputPath = filePath;
            formattedRow = (char)34 + DateTime.Now.ToString() + (char)34; /* Construct csv row starting with the timestamp */
            for (int i = 0; i < columns.Count(); i++) { formattedRow += "," + (char)34 + columns[i] + (char)34; }
        }

        public outputCsvRow(string filePath, DataTable dt) /* Initialize object */
        {
            dataTable = dt;
            outputPath = filePath;
            formattedRow = (char)34 + DateTime.Now.ToString() + (char)34; /* Construct csv row starting with the timestamp */
            for (int i = 0; i < dataTable.Columns.Count; i++) { formattedRow += "," + (char)34 + dataTable.Columns[i].ColumnName + (char)34; }
        }

        public void addRow(List<Tuple<string, string>> syslogList)
        {
            int attempts = 0;
            bool canAccess = false;
            StreamWriter logWriter = null;



            if (!File.Exists(outputPath)) /* If the file doesn't exist, give it some column headers */
            {
                logWriter = new StreamWriter(outputPath, true);
                logWriter.WriteLine((char)34 + "Event_Time" + (char)34 + "," +
                  (char)34 + "Device_IP" + (char)34 + "," + (char)34 + "SysLog" + (char)34);
                logWriter.Close();
            }
            /* Thread safety first! This is a poor man's SpinLock */
            while (true)
            {
                try
                {
                    logWriter = new StreamWriter(outputPath, true); /* Try to open the file for writing */
                    canAccess = true; /* Success! */
                    break;
                }
                catch (IOException ex)
                {
                    if (attempts < 15) { attempts++; Thread.Sleep(50); }
                    else { Console.WriteLine(ex.ToString()); break; } /* Give up after 15 attempts */
                }
            }
            if (canAccess) /* Write the line if the file is accessible */
            {
                logWriter.WriteLine(formattedRow);
                logWriter.Close();
                /* Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                 {
                     AppendTextTosyslogLogger(formattedRow);
                 })); */

            }
            return;
        }

        public void addRow()
        {
            int attempts = 0;
            bool canAccess = false;
            StreamWriter logWriter = null;
            


            if (!File.Exists(outputPath)) /* If the file doesn't exist, give it some column headers */
            {
                try
                {
                    logWriter = new StreamWriter(outputPath, true);
                    logWriter.WriteLine((char)34 + "Event_Time" + (char)34 + "," +
                      (char)34 + "Device_IP" + (char)34 + "," + (char)34 + "SysLog" + (char)34);
                    logWriter.Close();
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.ToString());
                }
            }
            /* Thread safety first! This is a poor man's SpinLock */
            while (true)
            {
                try
                {
                    logWriter = new StreamWriter(outputPath, true); /* Try to open the file for writing */
                    canAccess = true; /* Success! */
                    break;
                }
                catch (IOException ex)
                {

                    if (attempts < 15) {
                        Random random = new Random();
                        int randomNumber = random.Next(0, 200);
                        attempts++;
                        Thread.Sleep(randomNumber); }
                    else { Console.WriteLine(ex.ToString()); break; } /* Give up after 15 attempts */
                }
            }
            if (canAccess) /* Write the line if the file is accessible */
            {
                logWriter.WriteLine(formattedRow);
                logWriter.Close();
               /* Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    AppendTextTosyslogLogger(formattedRow);
                })); */

            }
            return;
        }

        public void addTable()
        {
            int attempts = 0;
            bool canAccess = false;
            StreamWriter logWriter = null;

                      
            /* Thread safety first! This is a poor man's SpinLock */
            while (true)
            {
                try
                {
                    logWriter = new StreamWriter(outputPath, true); /* Try to open the file for writing */
                    canAccess = true; /* Success! */
                    break;
                }
                catch (IOException ex)
                {

                    if (attempts < 15)
                    {
                        Random random = new Random();
                        int randomNumber = random.Next(0, 200);
                        attempts++;
                        Thread.Sleep(randomNumber);
                    }
                    else { Console.WriteLine(ex.ToString()); break; } /* Give up after 15 attempts */
                }
            }
            if (canAccess) /* Write the line if the file is accessible */
            {
                logWriter.WriteLine(formattedRow);
                for (int i = 0; i< dataTable.Rows.Count; i++)
                {
                    formattedRow = "";
                    for (int ii = 0; ii < dataTable.Columns.Count; ii++) { formattedRow += "," + (char)34 + dataTable.Rows[i][ii].ToString() + (char)34; }
                    logWriter.WriteLine(formattedRow);
                }
                logWriter.Close();
                

            }
            return;
        }


    }
}//name space
