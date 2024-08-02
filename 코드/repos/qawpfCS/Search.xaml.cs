using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
using System.Xml;

namespace qawpfCS
{
    /// <summary>
    /// Search.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Search : Page
    {
        public Search()
        {
            InitializeComponent();
        }
        public static string SearchWord { get; set; }
        public static string SearchCode { get; set; }
        public static string CountryName { get; set; }
        public static string CountryEnName { get; set; }
        public static string Continent { get; set; }
        public static string Basic { get; set; }
        public static string Attention { get; set; }
        public static string AttentionNote { get; set; }
        public static string Control { get; set; }
        public static string ControlNote { get; set; }

        public static List<string> list = new List<string>();


        public static string SearchCountry(string word, string code)
        {
            string url = "http://apis.data.go.kr/1262000/CountryBasicService/getCountryBasicList";
            url += "?ServiceKey=" + "u4%2By4o6XqytXJOAkASOPL5aPZ%2Bd0xDLyEJYJgxeea6%2FzSsYPOThOm4y1wLR80mI%2BFiaLK5TxjlhsAixMOKK0Rw%3D%3D"; // Service Key
            url += "&numOfRows=10";
            url += "&pageNo=1";
            url += "&countryName=" + word;
            url += "&countryEnName=";
            url += "&isoCode1=" + code;

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";

            string results1 = string.Empty;
            HttpWebResponse response;
            using (response = request.GetResponse() as HttpWebResponse)
            {
                StreamReader reader = new StreamReader(response.GetResponseStream());
                results1 = reader.ReadToEnd();
            }

            return results1;
        }


        public static string Danger(string word, string code)
        {
            string url = "http://apis.data.go.kr/1262000/TravelWarningService/getTravelWarningList"; // URL
            url += "?ServiceKey=" + "u4%2By4o6XqytXJOAkASOPL5aPZ%2Bd0xDLyEJYJgxeea6%2FzSsYPOThOm4y1wLR80mI%2BFiaLK5TxjlhsAixMOKK0Rw%3D%3D"; // Service Key
            url += "&numOfRows=10";
            url += "&pageNo=1";
            url += "&countryName=" + word;
            url += "&countryEnName=";
            url += "&isoCode1=" + code;

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";

            string results2 = string.Empty;
            HttpWebResponse response;
            using (response = request.GetResponse() as HttpWebResponse)
            {
                StreamReader reader = new StreamReader(response.GetResponseStream());
                results2 = reader.ReadToEnd();
            }

            return results2;
        }


        public static List<string> InitValue1(string results1)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(results1);
            XmlNodeList itemList = doc.GetElementsByTagName("item");

            foreach (XmlNode item in itemList)
            {
                CountryName = item["countryName"]?.InnerText;
                CountryEnName = item["countryEnName"]?.InnerText;
                Continent = item["continent"]?.InnerText;
                Basic = item["basic"]?.InnerText;
                Attention = item["attention"]?.InnerText;
                AttentionNote = item["attentionNote"]?.InnerText;
                Control = item["control"]?.InnerText;
                ControlNote = item["controlNote"]?.InnerText;

                string jsonBasic = JsonConvert.SerializeObject(Basic);
                string jsonBasic1 = jsonBasic.Replace("<br>", "");
                string jsonBasic2 = jsonBasic1.Replace("<p style=", "");
                string jsonBasic3 = jsonBasic2.Replace("</p>", "");
                string jsonBasic4 = jsonBasic3.Replace("<div>", "");
                string jsonBasic5 = jsonBasic4.Replace("</div>", "");
                string jsonBasic6 = jsonBasic5.Replace("margin-left: 20px; margin-right: 20px;", "");
                string jsonBasic7 = jsonBasic6.Replace("margin-left: 15px; margin-right: 15px;", "");
                string jsonBasic8 = jsonBasic7.Replace("\"\">", "");

                string BasicStr = JsonConvert.DeserializeObject<string>(jsonBasic8);

                list.Add(CountryName);
                list.Add(CountryEnName);
                list.Add(Continent);
                list.Add(BasicStr);
                list.Add(Attention);
                list.Add(AttentionNote);
                list.Add(Control);
                list.Add(ControlNote);
            }

            return list;
        }


        private void btn_search_Click(object sender, RoutedEventArgs e)
        {
            SearchWord = tb_country.Text; // 검색어 설정
            SearchCode = tb_code.Text;
            string result1 = SearchCountry(SearchWord, SearchCode);
            string results2 = Danger(SearchWord, SearchCode);
            InitValue1(result1);
            InitValue1(results2);

            lv_print.ItemsSource = list;
        }
    }
}

