using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace MusicImageGet
{
    class HTTPReq
    {
        public HttpWebRequest get;
        CookieContainer cc = new CookieContainer();
        public string ResponMain;
        public byte [] ResponBytes;
        public string UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";
        public string AcceptEncoding = "deflate";
        public string Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";

        public int GetHttp(string HttpArg, string Method = "GET", string Content = "", string Referer = "", bool KeepAlive = true, int TimeOut = 10000)
        {
            get = (HttpWebRequest)System.Net.WebRequest.Create(HttpArg);
            get.CookieContainer = cc;
            get.Method = Method;
            get.Accept = Accept;
            get.KeepAlive = KeepAlive;
            //Time Out
            get.Timeout = TimeOut;
            get.ReadWriteTimeout = TimeOut;
            //302取不到cookie
            get.AllowAutoRedirect = false;

            if (UserAgent != "")
                get.UserAgent = UserAgent;

            get.ContentType = "text/html;charset=UTF-8";
            if (Referer != "")
                get.Referer = Referer;

            //POST数据
            if (Content != "")
            {
                byte[] bs = ASCIIEncoding.ASCII.GetBytes(Content);
                get.ContentLength = bs.Length;
                using (Stream reqStream = get.GetRequestStream())
                {
                    reqStream.Write(bs, 0, bs.Length);
                }
            }

            try
            {
                HttpWebResponse getRespone = (HttpWebResponse)get.GetResponse();
                if (getRespone != null && (getRespone.StatusCode == HttpStatusCode.OK || getRespone.StatusCode == HttpStatusCode.Found))
                {
                    System.IO.Stream resStream = getRespone.GetResponseStream();

                    if (getRespone.ContentEncoding == "gzip")
                        resStream = new GZipStream(resStream, CompressionMode.Decompress);
                    if (getRespone.CharacterSet.ToLower() == "utf-8")
                    {
                        System.IO.StreamReader sr = new System.IO.StreamReader(resStream, Encoding.GetEncoding("utf-8"));
                        ResponMain = sr.ReadToEnd();
                        sr.Close();
                    }
                    else
                    {
                        int len = (int)getRespone.ContentLength;
                        ResponBytes = new byte[len];
                        int offset = 0;
                        while (len > 0)
                        {
                            int n = resStream.Read(ResponBytes, offset, len);
                            offset += n;
                            len -= n;
                        }
                    }
                }
                getRespone.Close();
                return 1;
            }
            catch
            {
                throw;
            }
        }
    }
}
