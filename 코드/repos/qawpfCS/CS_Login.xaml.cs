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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace qawpfCS
{
    /// <summary>
    /// CS_Login.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CS_Login : Page
    {
        string serverIp = "10.10.21.115";
        int serverPort = 12345;
        public static NetworkStream stream;


        public CS_Login()
        {
            InitializeComponent();
            stream = Network.CS.Conncection(serverIp, serverPort);
        }

        private void Login_btn_Click(object sender, RoutedEventArgs e)
        {
            JObject temp;
            temp = Network.CS.LoginObject(ID_txtbox.Text, Password_txtbox.Text);
            CS.DataWrite(stream, temp);
            string response = CS.DataRecive(stream);
            var jsonResponse = JsonConvert.DeserializeObject<dynamic>(response);
            bool login = jsonResponse.login_success;
            if (login)
            {
                NavigationService.Navigate(
                    new Uri("/Menu.xaml", UriKind.Relative)
                    );
            }
            else
            {
                MessageBox.Show("로그인 실패");
            }
        }
    }
}
