using System;
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
using System.Windows.Shapes;

namespace MessengerPR7
{
    public partial class AdminWindow : Window
    {

        private CancellationTokenSource isWorking;
        private bool isPageOpen = false;
        DateTime date = DateTime.Now;
        public AdminWindow()
        {
            InitializeComponent();

            TcpServer.Server();
            isWorking = new CancellationTokenSource();
            UpdateUsers();
            ListenToClients(isWorking.Token);
        }

        private async Task ListenToClients(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var client = await TcpServer.socket.AcceptAsync();
                    _ = RecieveMessage(client, token);
                
                
            }
        }

        private async Task RecieveMessage(Socket client, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    byte[] bytes = new byte[1024];
                    int bytesRead = await client.ReceiveAsync(bytes, SocketFlags.None);
                    if (bytesRead == 0)
                    {
                        HandleClientDisconnection(client); 
                        return;
                    }

                    string message = Encoding.UTF8.GetString(bytes, 0, bytesRead);
                    int action = Convert.ToInt32(message.Substring(0, 1));
                    message = message.Substring(1);

                    switch (action)
                    {
                        case 0:
                            TcpServer.clients.Add(client, message);
                            TcpServer.logList.Add($"[{DateTime.Now}] \nНовый юзер: [{message}] ");
                            UpdateUsers();
                            BroadcastUsersList();
                            break;
                        case 1:
                            MessageLbx.Items.Add(message);
                            BroadcastMessage(message);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                HandleClientDisconnection(client); 
                
            }
        }

        private void HandleClientDisconnection(Socket client)
        {
            if (TcpServer.clients.ContainsKey(client))
            {
                string username = TcpServer.clients[client];
                TcpServer.clients.Remove(client);
                UpdateUsers();
                BroadcastUsersList();
                TcpServer.logList.Add($"[{DateTime.Now}] \nЮзер {username} покинул чат");
                client.Close();
            }
        }

        private void BroadcastMessage(string message)
        {
            foreach (var item in TcpServer.clients)
            {
                TcpServer.SendMessage(item.Key, message);
            }
        }

        private void BroadcastUsersList()
        {
            string allUsers = string.Join(";", TcpServer.clients.Values);
            foreach (var item in TcpServer.clients)
            {
                TcpServer.SendUsers(item.Key, allUsers);
            }
        }


        private void ShowLogs_Click(object sender, RoutedEventArgs e)
        {
            if (!isPageOpen)
            {
                Frame.Content = new ServerLogsPage();
                isPageOpen = true;
                ShowLogs.Content = "Посмотреть пользователей чата";
            }
            else
            {
                Frame.Content = null;
                isPageOpen = false;
                ShowLogs.Content = "Посмотреть логи чата";
            }
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            if (Name.Text == "/disconnect")
            {
                ExitAction();
            }
            else
            {
                if (Name.Text != "")
                {
                    MessageLbx.Items.Add($"[{date}] [{TcpServer.usersname}]: {Name.Text}");

                    foreach (var item in TcpServer.clients)
                    {
                        TcpServer.SendMessage(item.Key, $"[{date}] [{TcpServer.usersname}]: {Name.Text}");
                    }
                    Name.Text = "";
                }
                else
                {
                    MessageBox.Show("Сообщение пустое!");
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            ExitAction();
        }
        private void UpdateUsers()
        {
            UsersLbx.Items.Clear();
            foreach (var item in TcpServer.clients)
            {
                UsersLbx.Items.Add(item.Value);
            }
        }

        private void ExitAction()
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            isWorking.Cancel();
            TcpServer.socket.Close();
            TcpServer.clients = new Dictionary<Socket, string>();
            TcpServer.logList = new List<string>();
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            isWorking.Cancel();
            TcpServer.socket.Close();
            TcpServer.clients = new Dictionary<Socket, string>();
            TcpServer.logList = new List<string>();
        }
    }
}
