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

        
    }
    public class ezSystemProvider
    {
        public List<ezEdgeSystems> GetSystems(string XMLfile)
        {
            ///to do:
            //code is not complete yet, value will be loaded from XML file

            var eZsystems = new List<ezEdgeSystems>();

            ezEdgeSystems ez = new ezEdgeSystems();
            ez.Host = "192.168.3.110";
            ez.Port = 161;
            ez.Read_Community = "public";
            ez.Write_Community = "AdminPublic";
            eZsystems.Add(ez);


            return eZsystems;
        }
        public void setSystems(ezEdgeSystems ez , string fn)
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
                if (property.GetValue(ez, null)!= null)
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
            foundRows = dt.Select("Host Like '"+ rHost + "%'");
            

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

        public XMLtoDataset(string fn , string tn)
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

        public void xmlDumper(string fn , DataTable dt)
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

            sniTableId();

            //syslogLogger();


        }

        // add loges to UI
        // to do: loges should be added  to a text file 
        public void AppendTextToOutput(string text)
        {
            LogBox.Text = LogBox.Text + DateTime.Now.ToString() + ", " + text + "\r\n";
            LogBox.ScrollToEnd();
        }



        #region tree-view
        // add ez-edge systems to tree-view

        public bool IsSelected { get; set; }

        private void treeviewLoader(TreeView tv, string fn)
        {
            DataTable dt = new DataTable();
            DataSet ds = new DataSet();

            ezEdgeSystems eze = new ezEdgeSystems();

            Type type = eze.GetType();
            PropertyInfo[] properties = type.GetProperties();

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
                    treeItem.Header = dr[0];
                    treeItem.IsSelected = true;
                    tv.Items.Add(treeItem);


                }

                // ... Get TreeView reference and add both items.
                TreeViewItem tvi = tv.ItemContainerGenerator.Items[0] as TreeViewItem;
                tvi.IsSelected = true;

            }
            catch (IOException)
            {
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
            addRemoveTrapdestination(rHost, 161, GetLocalIPAddress(), "public", "AdminPublic", 6);
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
        public string crossConnection(string ipAdd, int sPort, string originCard, string originPort, string destCard, string destPort)
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
                AppendTextToOutput("create jumper :  (" + ipAdd + ") " + crossJumper);
            }));


            pdu.VbList.Add(new Oid(ConnectionTable + "16." + crossJumper + "4"), new Integer32(4));
            // Set a value to integer
            pdu.VbList.Add(new Oid(ConnectionTable + "6." + crossJumper + "4"), new Integer32(0));
            // Set a value to unsigned integer
            pdu.VbList.Add(new Oid(ConnectionTable + "7." + crossJumper + "4"), new Integer32(0));
            pdu.VbList.Add(new Oid(ConnectionTable + "12." + crossJumper + "4"), new OctetString("0"));

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

        private void createCrossConnection(string ipAdress, int sPort, string originCard, string originPort, string destCard, string destPort, string crossNumber)
        { /*
            string originCard = fromCard.Text;
            string originPort = fromPort.Text;
            string destCard = toCard.Text;
            string destPort = toPort.Text;
            */


            string result;
            for (int i = 0; i < Int16.Parse(crossNumber); i++)

            {

                result = crossConnection(ipAdress, sPort, originCard, originPort, destCard, destPort);
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
                var item = treeView.SelectedItem as TreeViewItem;

                string ipAdress = item.Header.ToString();
                int sPort = 161;

                Task task = new Task(() => { createCrossConnection(ipAdress, sPort, originCard, originPort, destCard, destPort, scrossNumber); });
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
            trapLoggerTextBox.AppendText(DateTime.Now.ToString() + ",   " + text + "\r\n");
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
                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                AppendTextToTrapLogger("*** Community: {0}" + pkt.Community.ToString());

                            }));

                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                AppendTextToTrapLogger("*** VarBind count: {0}" + pkt.Pdu.VbList.Count);

                            }));

                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                AppendTextToTrapLogger("*** VarBind content:");

                            }));

                            foreach (Vb v in pkt.Pdu.VbList)
                            {
                                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                                {
                                    AppendTextToTrapLogger("**** {0} {1}: {2}" +
                                   v.Oid.ToString() + SnmpConstants.GetTypeName(v.Value.Type) + v.Value.ToString());

                                }));
                            }
                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                AppendTextToTrapLogger("** End of SNMP Version 2 TRAP data.");
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
                syslogLoggerTextBox.AppendText(DateTime.Now.ToString() + ",   " + text + "\r\n");
                syslogLoggerTextBox.ScrollToEnd();
            }
        }

        public void syslogLogger()
        {
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
            UdpClient udpListener = new UdpClient(514);
            string tobelog;
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
                        AppendTextTosyslogLogger(tobelog);
                    }));



                    /* Start a new thread to handle received syslog event */
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        AppendTextTosyslogLogger(ex.ToString());
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
                            tableIdList.Add(tableAr[0],tableAr[1]);

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

        public void tableBrowserNext(string tHost, int tPort, string tCommunity, string tOID, int columnNumber, DataGrid dg)
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
                        AppendTextToOutput("table browser : (" + tHost + ") " + e.Message);
                    }));
                    lastOid = null;
                }


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
                        AppendTextToOutput("table browser : No response received from SNMP agent. (" + tHost + ") ");
                    }));
                }

            }



            //



            target.Close();

        }

        private void loadCrossTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = treeView.SelectedItem as TreeViewItem;

                string ipAdress = item.Header.ToString();
                Task task = new Task(() => { tableBrowserNext(ipAdress, 161, "public", "1.3.6.1.4.1.4987.1.1.1", 16, tableBrowserGrid); });
                task.Start();
            }
            catch (Exception)
            {

                pleaseSelectEz pl = new pleaseSelectEz();
                pl.Show();
            }



        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            sniTableId();
            XMLtoDataset xmlToDataset = new XMLtoDataset();
            DataTable dt = new DataTable();
            dt = xmlToDataset.xmlReader("simpler-mib.xml", "table");
            xmlTestGrid.ItemsSource = dt.DefaultView;
        }


        private void loadAlarmTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = treeView.SelectedItem as TreeViewItem;

                string ipAdress = item.Header.ToString();
                Task task = new Task(() => { tableBrowserNext(ipAdress, 161, "public", "1.3.6.1.4.1.4987.1.11.4.1", 6, tableBrowserAlarm); });
                task.Start();
            }
            catch (Exception)
            {

                pleaseSelectEz pl = new pleaseSelectEz();
                pl.Show();
            }
        }


        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (XCtab.IsSelected ==true && xcAutoRefreshCheckBox.IsChecked ==true)
            {
                try
                {
                    var item = treeView.SelectedItem as TreeViewItem;
                    string ipAdress = item.Header.ToString();
                    Task task = new Task(() => { tableBrowserNext(ipAdress, 161, "public", "1.3.6.1.4.1.4987.1.1.1", 16, tableBrowserGrid); });
                    task.Start();



                }
                catch (Exception)
                {

                    pleaseSelectEz pl = new pleaseSelectEz();
                    pl.Show();
                }
            }
            if (systemAlarmTab.IsSelected == true && alarmAutoRefreshCheckBox.IsChecked == true)
            {
                try
                {
                    var item = treeView.SelectedItem as TreeViewItem;

                    string ipAdress = item.Header.ToString();
                    Task task = new Task(() => { tableBrowserNext(ipAdress, 161, "public", "1.3.6.1.4.1.4987.1.11.4.1", 6, tableBrowserAlarm); });

                    task.Start();

                }
                catch (Exception)
                {

                    pleaseSelectEz pl = new pleaseSelectEz();
                    pl.Show();
                }

            }



        }

        public void AlarmAutoRefreshCheckBox(string ipAdress)
        {
            while (systemAlarmTab.IsSelected == true && alarmAutoRefreshCheckBox.IsChecked == true)
            {
                try
                {
                   
                    var t = Task.Run(async delegate
                    {
                        await Task.Delay(5000);
                        
                    });
                    t.Wait();

                }
                catch (Exception ex)
                {

                    
                }

            }

        }

        public void XcAutoRefreshCheckBox(string ipAdress)
        {
            
                try
                {
                    Task task = new Task(() => { tableBrowserNext(ipAdress, 161, "public", "1.3.6.1.4.1.4987.1.1.1", 16, tableBrowserGrid); });
                    task.Start();
                   

                }
                catch (Exception ex)
                {


                }

            

        }


        #endregion



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
        private string outputPath = @"C:\logs\syslog\syslog.csv"; /* Location to store events */
        private string source; private string log;

        public logHandler(string sourceIP, string logData) /* Initialize object and clean up the raw data */
        {
            source = sourceIP.Trim(); /* Client IP */
            log = logData.Replace(Environment.NewLine, "").Trim(); /* Syslog data */
        }

        public void handleLog() /* Store the syslog and determine whether to trigger an email notification */
        {
            /* Store the syslog using a new thread */
            new Thread(new outputCsvRow(outputPath, new string[] { source, log }).addRow).Start();
            for (int i = 0; i < emailTriggers.Count(); i++) { if (log.Contains(emailTriggers[i])) { emailEvent(); } }
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

        public outputCsvRow(string filePath, string[] columns) /* Initialize object */
        {
            outputPath = filePath;
            formattedRow = (char)34 + DateTime.Now.ToString() + (char)34; /* Construct csv row starting with the timestamp */
            for (int i = 0; i < columns.Count(); i++) { formattedRow += "," + (char)34 + columns[i] + (char)34; }
        }

        public void addRow()
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

       
    }
}//name space
