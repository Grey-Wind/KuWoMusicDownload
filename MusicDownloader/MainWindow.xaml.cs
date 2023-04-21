using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MusicDownloader
{
    public partial class MainWindow : Window
    {
        private string cookie = "_ga=GA1.2.218753071.1648798611; _gid=GA1.2.144187149.1648798611; _gat=1; Hm_lvt_cdb524f42f0ce19b169a8071123a4797=1648798611; Hm_lpvt_cdb524f42f0ce19b169a8071123a4797=1648798611; kw_token=HH3GHIQ0RYM";

        public MainWindow()
        {
            InitializeComponent();
        }

        // 搜索歌曲
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string keyword = txtSearch.Text.Trim();
            List<Song> songs = Search(keyword, cookie);
            dgSongs.ItemsSource = songs;
        }

        // 下载歌曲
        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            Song song = (sender as Button).DataContext as Song;
            DownloadSong(song, cookie);
        }

        // 搜索歌曲
        private List<Song> Search(string keyword, string cookie)
        {
            List<Song> songs = new List<Song>();
            string apiUrl = $"http://www.kuwo.cn/api/www/search/searchMusicBykeyWord?key={keyword}&pn=1&rn=30";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers["Cookie"] = cookie;
            request.Referer = "http://www.kuwo.cn/search/list";

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string result = reader.ReadToEnd();

                JObject json = JObject.Parse(result);
                JArray data = (JArray)json["data"]["list"];

                foreach (var item in data)
                {
                    Song song = new Song();
                    song.Id = item["musicrid"].ToString().Substring(6);
                    song.Name = item["name"].ToString();
                    song.Artist = item["artist"].ToString();
                    song.Duration = item["songTimeMinutes"].ToString();
                    song.Size = item["sizeflac"].ToString();
                    song.Url = item["url"].ToString();
                    songs.Add(song);
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
            }
            return songs;
        }

        // 下载歌曲
        private void DownloadSong(Song song, string cookie)
        {
            string apiUrl = $"http://www.kuwo.cn/url?format=mp3&rid={song.Id}&response=url&type=convert_url3&_={DateTime.Now.Ticks}";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers["Cookie"] = cookie;
            request.Referer = $"http://www.kuwo.cn/play_detail/{song.Id}";

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                FileStream fileStream = new FileStream($"{song.Name}.mp3", FileMode.Create);
                byte[] buffer = new byte[1024];
                int length = stream.Read(buffer, 0, buffer.Length);
                while (length > 0)
                {
                    fileStream.Write(buffer, 0, length);
                    length = stream.Read(buffer, 0, buffer.Length);
                }
                fileStream.Close();
                MessageBox.Show($"{song.Name} 下载完成！");
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // 歌曲信息
        public class Song
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Artist { get; set; }
            public string Duration { get; set; }
            public string Size { get; set; }
            public string Url { get; set; }
        }
    }
}
