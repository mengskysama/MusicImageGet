using FLAC_Comment_Editor;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace MusicImageGet
{
    class MusicFactory
    {
        public string album, artist, title, album_artist, filepath;

        public byte[] cover = null;

        public MusicHelper h = null;

        HTTPReq httpreq = new HTTPReq();

        public MusicFactory(string filepath)
        {
            string type = filepath.Substring(filepath.LastIndexOf('.') + 1).ToLower();
            switch (type)
            {
                case "flac":
                    h = new FlacHelper(filepath);
                    break;
                case "m4a":
                    h = new M4aHeplper(filepath);
                    break;
                default:
                    throw new Exception("Unsupport Format!");
            }
            this.filepath = filepath;
        }

        public int RemoveCovrMeta()
        {
            int ret = 1;
            if (h == null)
                return ret;

            ret = h.RemoveCovrMeta();
            if (ret == 0)
            {
                album = h.album;
                artist = h.artist;
                cover = h.cover;
                title = h.title;
                album_artist = h.album_artist;
            }
            return ret;
        }

        public int AddCovrMeta(byte[] pic_metadata)
        {
            int ret = 1;
            if (h == null)
                return ret;

            ret = h.AddCovrMeta(pic_metadata);
            if (ret == 0)
            {
                album = h.album;
                artist = h.artist;
                cover = h.cover;
                title = h.title;
                album_artist = h.album_artist;
            }
            return ret;
        }

        public int GetComment()
        {
            int ret = 1;
            if (h == null)
                return ret;

            ret = h.GetComment();
            if (ret == 0)
            {
                album = h.album;
                artist = h.artist;
                cover = h.cover;
                title = h.title;
                album_artist = h.album_artist;
            }
            return ret;
        }

        private string MakeDouBanSearchStr(int use_file_name = 0)
        {
            if (use_file_name == 1)
            {
                string filename = filepath.Substring(filepath.LastIndexOf("\\") + 1);
                int n = filename.LastIndexOf(".");
                if (n != -1)
                    filename = filename.Substring(0, n);
                int sep = filename.IndexOf(" - ");
                if (sep != -1)
                {
                    //从文件名猜测歌曲名
                    return filename.Replace(" - ", " ");
                }
                return null;
            }

            if (album != null && (artist != null || album_artist != null))
            {
                //专辑信息完整
                string ret = "";
                if (title != null)
                    ret += " " + title;
                if (album != null)
                    ret += " " + album;
                                if (artist != null)
                    ret += " " + artist;
                if (album_artist != null)
                    ret += " " + album_artist;
                return ret;
            }
            else
            {
                string filename = filepath.Substring(filepath.LastIndexOf("\\") + 1);
                int n = filename.LastIndexOf(".");
                if (n != -1)
                    filename = filename.Substring(0, n);
                int sep = filename.IndexOf(" - ");
                if (sep != -1)
                {
                    //从歌曲名猜测专辑名字
                    return filename.Replace(" - ", " ");
                }
            }
            return null;
        }

        private string MakeLastFmSearchStr(int use_file_name = 0)
        {
            if (use_file_name == 1)
            {
                string filename = filepath.Substring(filepath.LastIndexOf("\\") + 1);
                int n = filename.LastIndexOf(".");
                if (n != -1)
                    filename = filename.Substring(0, n);
                int sep = filename.IndexOf(" - ");
                if (sep != -1)
                {
                    //从文件名猜测歌曲名
                    return filename.Replace(" - ", " ");
                }
                return null;
            }

            //if (album != null && album_artist != null)
            //{
                //专辑信息完整
            //    return (album + " " + album_artist);
            //}
            if (album != null && artist != null)
            {
                //专辑信息完整
                return (album + " " + artist);
            }
            else if (title != null && artist != null)
            {
                //歌曲信息完整
                return (title + " " + artist);
            }
            else
            {
                string filename = filepath.Substring(filepath.LastIndexOf("\\") + 1);
                int n = filename.LastIndexOf(".");
                if (n != -1)
                    filename = filename.Substring(0, n);
                int sep = filename.IndexOf(" - ");
                if (sep != -1)
                {
                    //从歌曲名猜测专辑名字
                    return filename.Replace(" - ", " ");
                }
            }
            return null;
        }

        private int SearchVGMDB()
        {
            httpreq.GetHttp("http://vgmdb.net/forums/login.php?do=login", "POST", "vb_login_username=mengsky&vb_login_password=&s=&do=login&vb_login_md5password=953cdbd5747772fd609cfebf0a4384f9&vb_login_md5password_utf=953cdbd5747772fd609cfebf0a4384f9", "application/x-www-form-urlencoded");
            httpreq.GetHttp("http://vgmdb.net/search?q=" + MakeLastFmSearchStr());
            if (httpreq.ResponMain.IndexOf("0 album results") != -1)
                return 1;
            return 0;
        }

        public byte[] SearchDOUBAN(int index, int SerchStr=0)
        {
            string str_seacrh;

            if (SerchStr == 1)
                str_seacrh = MakeLastFmSearchStr();
            else
                str_seacrh = MakeDouBanSearchStr();

            if (str_seacrh == null)
                throw new Exception("专辑/单曲信息不全!放弃查找");

            //搜索专辑
            httpreq.GetHttp("http://music.douban.com/subject_search?search_text=" + str_seacrh);
            Regex rx = new Regex("href=\"(?<url>http://music.douban.com/subject/.*?)\"");
            Match matches = rx.Match(httpreq.ResponMain);

            string url = matches.Groups["url"].ToString();
            if (url == "")
                throw new Exception("DouBan找不到:[" + str_seacrh + "]");

            //进入专辑
            httpreq.GetHttp(url);
            rx = new Regex("<a class=\"nbg\" href=\"(?<url>.*?)\"");
            matches = rx.Match(httpreq.ResponMain);

            url = matches.Groups["url"].ToString();
            if (url == "")
                throw new Exception("DouBan找不到图片地址:[" + str_seacrh + "]");

            //下载图片
            Form1.WinForm.FListUpdateFunction("正在从DouBan下载图片...", index);
            httpreq.GetHttp(url);
            return httpreq.ResponBytes;
        }

        private byte[] SearchLASTFM(int index)
        {
            string str_seacrh = MakeLastFmSearchStr();

            if (str_seacrh == null)
                throw new Exception("专辑/单曲信息不全!放弃查找");

            //查找
            httpreq.GetHttp("http://cn.last.fm/search?q=" + str_seacrh);
            Regex rx = new Regex("/(?<url>music/(?!\\+free-music-downloads).*?)\"");
            Match matches = rx.Match(httpreq.ResponMain);

            string url = matches.Groups["url"].ToString();
            if (url == "")
                throw new Exception("LastFM找不到:[" + str_seacrh + "]");

            //详情
            httpreq.GetHttp("http://cn.last.fm/" + url);
            if (httpreq.ResponMain.IndexOf("rounded featured-album") != -1)
            {
                //还需要进入专辑页面
                rx = new Regex("/(?<url>music/(.*))\"( *)class=\"media-p");
                matches = rx.Match(httpreq.ResponMain);
                url = matches.Groups["url"].ToString();
                if (url == "")
                    throw new Exception("LastFM找不到专辑:[" + str_seacrh + "]");
                httpreq.GetHttp("http://cn.last.fm/" + url);
            }

            rx = new Regex("src=\"(?<url>(.*))\"( *)class=\"album-cover\"");
            matches = rx.Match(httpreq.ResponMain);
            url = matches.Groups["url"].ToString();
            if (url == "")
                throw new Exception("LastFM第二跳转找不到:[" + str_seacrh + "]");

            //下载图片
            Form1.WinForm.FListUpdateFunction("正在从LashFM下载图片...", index);
            httpreq.GetHttp(url);
            return httpreq.ResponBytes;
        }

        private byte[] ApiLASTFM(int index)
        {
            if (album == null || artist == null)
                throw new Exception("专辑/艺术家信息不全!放弃查找");

            //查找
            httpreq.GetHttp("http://ws.audioscrobbler.com/2.0/?album=" + album + "&api_key=380094eceeba38d7deb8b7da8075158a&artist=" + artist + "&lang=zh&method=album.getInfo");

            string url;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(httpreq.ResponMain);
                System.Xml.XmlElement el = doc["lfm"]["album"];
                XmlNodeList pics = doc.SelectNodes("/lfm/album/image");
                XmlNode node = pics.Item(pics.Count - 1);
                url = node.InnerText;
            }
            catch
            {
                throw new Exception("LastFM找不到专辑[" + album + " " + artist + "]");
            }

            Form1.WinForm.FListUpdateFunction("正在从LashFM下载图片...", index);
            httpreq.GetHttp(url);
            return httpreq.ResponBytes;
        }

        //更新歌曲内嵌图片
        public void UpdateMusicCovr(int index)
        {
            try
            {
                //获取歌曲信息
                GetComment();
                Form1.WinForm.FListUpdateFunction("开始在LashFM查找..", index);
                byte[] covr;
                try
                {
                    covr = ApiLASTFM(index);
                }
                catch (Exception ex)
                {
                    //throw new Exception("2222");
                    int tryCnt = 0;
                    retry:
                    //LashFM出错
                    Form1.WinForm.FListUpdateFunction(ex.Message, index);
                    try
                    {
                        Form1.WinForm.FListUpdateFunction("开始在DouBan查找..", index);
                        covr = SearchDOUBAN(index, tryCnt);
                    }
                    catch
                    {
                        if (tryCnt < 1)
                        {
                            //更换关键词再试
                            tryCnt++;
                            goto retry;
                        }
                        throw;
                    }
                }
                //byte[] covr = File.ReadAllBytes("D:\\照片\\cover.jpg");
                covr = ImageHelper.ProcessImg(covr);
                RemoveCovrMeta();
                AddCovrMeta(covr);
            }
            catch (Exception ex)
            {
                Form1.WinForm.FListUpdateFunction(ex.Message, index);
                throw;
            }
        }
    }

    class MusicHelper
    {
        public string album, artist, title, album_artist;

        public byte[] cover = null;

        public string filepath;

        public MusicHelper(string filepath)
        {
            this.filepath = filepath;
        }

        public virtual int RemoveCovrMeta()
        {
            return 0;
        }

        public virtual int AddCovrMeta(byte [] pic_metadata)
        {
            return 0;
        }
        
        public virtual int GetComment()
        {
            return 0;
        }
    }
}
