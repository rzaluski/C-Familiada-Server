﻿using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Server
{
    /// <summary>
    /// Interaction logic for Table.xaml
    /// </summary>
    public partial class Table : Page
    {
        private TcpListener tcpListener;
        private Socket socket;
        public Table(TcpListener _tcpListener, Socket _socket)
        {
            InitializeComponent();
            tcpListener = _tcpListener;
            socket = _socket;

            Task task = new Task(ListenToClient);
            task.Start();

            //StopListening();
        }

        private void ListenToClient()
        {
            byte[] b = new byte[100];
            while(true)
            {
                int k = socket.Receive(b);
                for (int i = 0; i < k; i++)
                    Console.Write(Convert.ToChar(b[i]));
            }
        }

        private void StopListening()
        {
            socket.Close();
            tcpListener.Stop();
        }
    }
}
