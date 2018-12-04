using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string IP { get; set; }
        public int Port { get; set; } = 6969;
        public MainWindow()
        {
            InitializeComponent();
            IP = GetLocalIPAddress();
            labelIP.DataContext = this;
            labelPort.DataContext = this;
        }
        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
        private void OpenConnection()
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(IP);
                TcpListener tcpListener = new TcpListener(ipAddress, Port);
                tcpListener.Start();
                Socket socket = tcpListener.AcceptSocket();
                
                Dispatcher.Invoke(new Action(() => {
                    Table table = new Table(tcpListener, socket);
                    this.Content = table;
                }));
                //this.Content = table;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error..... " + e.StackTrace);
            }
        }

        

        private void ButtonOpenConnection_Click(object sender, RoutedEventArgs e)
        {
            buttonOpenConnection.Visibility = Visibility.Collapsed;
            stackPanelWaiting.Visibility = Visibility.Visible;
            new Thread(OpenConnection).Start();
        }
    }
}
