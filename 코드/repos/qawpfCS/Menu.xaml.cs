using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace qawpfCS
{
    /// <summary>
    /// Menu.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Menu : Page
    {
        private bool logFlag = false;
        private bool DBFlag = false;
        public Menu()
        {
            InitializeComponent();
        }

        private Window Direct_Chat;
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string message = "CHAT";
            byte[] data = Encoding.UTF8.GetBytes(message);
            await CS_Login.stream.WriteAsync(data, 0, data.Length);
            if (logFlag == false)
            {
                Direct_Chat = new Direct_Chat()
                {
                    Height = 450,
                    Width = 300
                };
                Direct_Chat.Show();
                logFlag = true;
            }
            else
            {
                Direct_Chat.Close();
                Direct_Chat = null;
                logFlag = false;
            }

        }

        private Window DBCHECK;
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (DBFlag == false)
            {
                DBCHECK = new DBCHECK()
                {
                    Height = 450,
                    Width = 650
                };
                DBCHECK.Show();
                DBFlag = true;
            }
            else
            {
                DBCHECK.Close();
                DBCHECK = null;
                DBFlag = false;
            }
        }

        private void API_btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(
                new Uri("/Search.xaml", UriKind.Relative)
                );
        }
    }
}
