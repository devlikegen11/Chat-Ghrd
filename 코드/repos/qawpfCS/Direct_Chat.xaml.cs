using Network;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;

namespace qawpfCS
{
    /// <summary>
    /// Direct_Chat.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Direct_Chat : Window
    {
        private ObservableCollection<string> messageList = new ObservableCollection<string>();
        NetworkStream stream;
        public Direct_Chat()
        {
            InitializeComponent();
            stream = CS_Login.stream;
            Chat_List.ItemsSource = messageList;
            ReadMessagesAsync();
        }
        private async Task ReadMessagesAsync()
        {
            try
            {
                while (true)
                {
                    string receiveMessage = await CS.ReadAsync(stream);
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        messageList.Add("고객 채팅: " + receiveMessage);
                    }));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("서버와의 연결이 끊어졌습니다.", "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MessageBox.Show(e.Message);
                MessageBox.Show(e.StackTrace);
                Environment.Exit(1);
            }
        }
        private void messageAdd(string msg)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                messageList.Add(msg);
                Chat_List.Items.Add(msg);
            }));
        }

        private void Send_btn_Click(object sender, RoutedEventArgs e)
        {
            string message = chating.Text;
            CS.Write(stream, message);
            messageList.Add("나: " + message);
            chating.Clear();
            ScrollToBot();
        }

        private void chating_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string message = chating.Text;
                CS.Write(stream, message);
                messageList.Add("나: " + message);
                chating.Clear();
                ScrollToBot();
            }
        }
        private void ScrollToBot()
        {
            if (VisualTreeHelper.GetChildrenCount(Chat_List) > 0)
            {
                Border border = (Border)VisualTreeHelper.GetChild(Chat_List, 0);
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }
    }
}
