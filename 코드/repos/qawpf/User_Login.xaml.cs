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
using Network;   // Network 라이브러리 
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


namespace qawpf
{
    /// <summary>
    /// User_Login.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class User_Login : Page
    {
        string serverIp = "10.10.21.115";  // 서버 ip설정
        int serverPort = 12345;            // 포트번호
        public static NetworkStream stream;
        public User_Login()
        {
            InitializeComponent();
            stream = User.Conncection(serverIp, serverPort);
        }

        private void ID_txtbox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Password_txtbox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Login_btn_Click(object sender, RoutedEventArgs e)
        {
            JObject temp;                                                             // 임시 JSON 인스턴스 생성
            temp = User.LoginObject(ID_txtbox.Text, Password_txtbox.Text);            // LoginObject, Jobject 보낼 오브젝트 생성 후 return
            User.DataWrite(stream, temp);                                             // Jobject 보냄
            string response = User.DataRecive(stream);                                // 받아서 string 타입으로 변환 후 돌려줌
            var jsonResponse = JsonConvert.DeserializeObject<dynamic>(response);      // 형이 사용하셨던, json 타입으로 형변환 하는 문장
            bool login = jsonResponse.login_success;                                  // bool 타입 객체 선언 후 값을 받음
            if (login)                                                                // login이 true 일시
            {
                MainWindow.login_check = true;
                NavigationService.Navigate(
                    new Uri("/Menu.xaml", UriKind.Relative)                           // Menu.xaml ( 페이지 ) 로 이동
                    );
            }
            else                                                                      // 로그인 실패할시
            {
                MessageBox.Show("로그인 실패");                                       // 그냥 메시지 박스
            }
        }
    }
}
