using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;


namespace KuWoMusicDownload
{
    public partial class MainWindow : Window
    {
        private string apiUrl = "http://www.kuwo.cn";
        private string searchUrl = "/api/www/search/searchMusicBykeyWord?";
        private string playUrl = "/api/v1/www/music/playUrl?mid={0}&type=convert_url3&httpsStatus=1&reqId={1}";
        private Dictionary<string, string> headers;

        public MainWindow()
        {
            InitializeComponent();
            headers = new Dictionary<string, string>
            {
                //{ "accept", "application/json, text/plain, */*"},
                { "accept-encoding", "gzip, deflate" },
                { "accept-language", "zh-CN,zh;q=0.9" },
                { "cache-control", "no-cache" },
                { "Connection", "keep-alive" },
                { "csrf", "HH3GHIQ0RYM" },
                { "Referer", apiUrl + "/search/list?key=%E5%91%A8%E6%9D%B0%E4%BC%A6" },
                { "User-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.67 Safari/537.36" },
                { "Cookie", "_ga=GA1.2.218753071.1648798611; _gid=GA1.2.144187149.1648798611; _gat=1; Hm_lvt_cdb524f42f0ce19b169a8071123a4797=1648798611; Hm_lpvt_cdb524f42f0ce19b169a8071123a4797=1648798611; kw_token=HH3GHIQ0RYM" }
            };
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            List<Song> songList = new List<Song>();
            string searchText = SearchInput.Text;
            if (string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("请输入要搜索的歌曲或歌手");
                return;
            }

            string searchRequestUrl = apiUrl + searchUrl + $"key={searchText}&pn=1&rn=80&httpsStatus=1&reqId={Guid.NewGuid().ToString()}";
            HttpWebRequest request = HttpWebRequest.CreateHttp(searchRequestUrl);
            foreach (var header in headers)
            {
                //request.Headers.Add(header.Key, header.Value);
                HttpWebRequest requests = (HttpWebRequest)WebRequest.Create("http://www.kuwo.cn");
                request.Accept = "application/json, text/plain, */*, text/html";
                //WebResponse response = request.GetResponse();
                //requests.Headers.Add(HttpRequestHeader.Accept, "text/html");
            }
            request.Timeout = 20000;

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string responseString = reader.ReadToEnd();
                    JObject responseData = JObject.Parse(responseString);
                    int totalSongs = int.Parse(responseData["data"]["total"].ToString());
                    if (totalSongs <= 0)
                    {
                        MessageBox.Show($"找不到歌曲：{searchText}");
                        return;
                    }

                    JArray songsData = (JArray)responseData["data"]["list"];
                    for (int i = 0; i < songsData.Count; i++)
                    {
                        JObject songData = (JObject)songsData[i];
                        songList.Add(new Song
                        {
                            Index = i + 1,
                            Artist = songData["artist"].ToString(),
                            Title = songData["name"].ToString(),
                            Album = songData["album"].ToString(),
                            Rid = songData["rid"].ToString()
                        });
                    }

                    SearchResult.ItemsSource = songList;
                }
            }
            catch (WebException ex)
            {
                MessageBox.Show($"请求超时，请重新搜索。错误详情：{ex.Message}");
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchResult.SelectedItem == null)
            {
                MessageBox.Show("请选择要下载的歌曲");
                return;
            }

            Song song = (Song)SearchResult.SelectedItem;
            string playRequestUrl = apiUrl + string.Format(playUrl, song.Rid, Guid.NewGuid().ToString());

            HttpWebRequest request = HttpWebRequest.CreateHttp(playRequestUrl);
            foreach (var header in headers)
            {
                request.Headers.Set(header.Key, header.Value);
            }

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string responseString = reader.ReadToEnd();
                    JObject responseData = JObject.Parse(responseString);
                    string musicUrl = responseData["data"]["url"].ToString();

                    if (string.IsNullOrEmpty(musicUrl))
                    {
                        MessageBox.Show("歌曲下载地址为空");
                        return;
                    }

                    string fileName = $"{song.Title}--{song.Artist}.mp3";
                    string savePath = System.IO.Path.Combine("./kuwo_music", fileName);
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.DownloadFile(musicUrl, savePath);
                    }

                    MessageBox.Show($"已下载歌曲：{song.Title}--{song.Artist}");
                }
            }
            catch (WebException ex)
            {
                MessageBox.Show($"请求超时，请重新下载。错误详情：{ex.Message}");
            }
        }
    }

    public class Song
    {
        public int Index { get; set; }
        public string Artist { get; set; }
        public string Title { get; set; }
        public string Album { get; set; }
        public string Rid { get; set; }
    }
}
