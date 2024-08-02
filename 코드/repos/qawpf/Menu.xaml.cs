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

namespace qawpf
{
    /// <summary>
    /// Menu.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Menu : Page
    {
        private bool logFlag = false;
        public Menu()
        {
            InitializeComponent();
        }

        private void Login_btn_Click(object sender, RoutedEventArgs e)
        {
            //if (MainWindow.login_check)
            //{
                MainWindow.login_check = true;
                NavigationService.Navigate(
                    //new Uri("/Direct_Chat.xaml", UriKind.Relative) 
                    new Uri("/User_Login.xaml", UriKind.Relative)   // Menu.xaml ( 페이지 ) 로 이동
                    );
            //}
            //else
            //{
            //    MessageBox.Show("로그인을 하고 이용해주세요");
            //}
        }

        private void API_btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(
                new Uri("/Search.xaml", UriKind.Relative)
                );
        }

        private void QA_btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/QNA.xaml", UriKind.Relative));
        }

        private Window Direct_Chat;
        private void DirectChat_btn_Click(object sender, RoutedEventArgs e)
        {
            if(MainWindow.login_check == true)
            {
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
            else
            {
                MessageBox.Show("로그인후 진행해주세요");
            }
            
        }
    }
}
