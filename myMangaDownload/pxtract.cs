using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace pixtract
{
    static class Pxtractor
    {
        public class MyWebClient : WebClient
        {
            //protected override WebRequest GetWebRequest(Uri address)
            //{
            //    HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            //   // request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //    return request;
            //}
        }
        private static MyWebClient _client;

        private static MyWebClient client
        {
            get
            {
                if (_client == null)
                {
                    _client = new MyWebClient();
                   // _client.Headers[HttpRequestHeader.AcceptEncoding] = "gzip";
                }
                return _client;
            }
        }
        internal static string DownLoadPage(string src)
        {
            //string content = new StreamReader(new GZipStream(client.OpenRead(src), CompressionMode.Decompress)).ReadToEnd();
            for (int i = 0; i < 100; i++)
            {
                string content = new StreamReader(client.OpenRead(src)).ReadToEnd();
                if (!string.IsNullOrEmpty(content))
                    return content;
            }
            return null;
        }
        internal static bool Extract(HtmlDocument htmlDocument, string src,string file, out string hRef)
        {
            string host = "http://" + new Uri(src).Host;

            foreach (HtmlElement elt in htmlDocument.Links)
            {
                mshtml.HTMLAnchorElement link = (mshtml.HTMLAnchorElement)elt.DomElement;
                foreach (HtmlElement subelt in elt.Children)
                {
                    if (subelt.GetAttribute("className") == "img-responsive")
                        if (subelt.DomElement is mshtml.HTMLImg)
                        {
                            mshtml.HTMLImg img = (mshtml.HTMLImg)subelt.DomElement;
                            string imgsrc = img.src.Replace("about:", host);
                            hRef = link.href.Replace("about:", host);
                            return DownloadImage(imgsrc,file);
                        }
                }

            }
            hRef = "";
            return false;
        }

        private static bool DownloadImage(string src,string file)
        {
            try
            {
                byte[] data;
                for (int i = 0; i < 100; i++)
                {
                     data= client.DownloadData(src);
                    if (data != null)
                    {
                        File.WriteAllBytes(file,data);
                        return true;
                    }
                }
                return false;
            }
            catch { }
            return false;
        }

        internal static bool DownloadLargestImage(string src,string file, out string hRef)
        {
            try
            {
                string data = DownLoadPage(src);
                if (!String.IsNullOrEmpty(data))
                {
                    HtmlDocument doc = GetDocument(data);
                    return Extract(doc, src, file, out hRef);
                }
            }
            catch { }
            hRef = ""; return false; 
        }

        private static HtmlDocument GetDocument(string data)
        {
            WebBrowser browser = new WebBrowser();
            browser.ScriptErrorsSuppressed = true;
            browser.DocumentText = data;

            browser.Document.OpenNew(true);
            browser.Document.Write(data);
            browser.Refresh();
            return browser.Document;
        }
    }
}
