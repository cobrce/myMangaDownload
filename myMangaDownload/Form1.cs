using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using pixtract;
using System.IO; 

namespace myMangaDownload
{    
    public partial class Form1 : Form
    {
        Thread _thread;
        Thread thread
        {
            set
            {
                if (_thread != null && _thread.IsAlive)
                    _thread.Abort();
                _thread = value;
            }
            get { return _thread; }
        }

        private struct UrlData
        {
            public string Category;
            public string Manga;
            public string Chapter;
            public string Page;
        }
        string LocalDir { get { return Path.GetDirectoryName(typeof(Form1).Assembly.Location); } }

        static string regexp = @"(https?:\/\/)?(www\.)?mymanga.me\/(?<categ>[\w]*)\/(?<manga>[\w]*)\/((?<chapter>[\w]{1,}))($|\/(?<page>[\d]*))";
        // explaining : (https?:\/\/)+(www.)?mymanga.me\/(?<categ>[\w]*)\/(?<manga>[\w]*)\/(?<chapter>[\d]*)($|\/(?<page>[\d]*))
        // (https?:\/\/) : - the string must start with http(s):// 
        //                 - the ? means that the last group/char is optional, in this case it's th char 's'
        // (www\.)? : (optional) followed by www.
        //
        // then the string mymanga.me/
        //
        // \/(?<categ>[\w]*)\/ : means that the word between the two forward slash is a group calld "categ"
        //  ...and so on for the following groups (chapter and page are numbers)
        // {1,} means at least one char
        //
        // ($|\/(?<page>[\d]*)) : means end of string or a forward slash followed by the  group "page"
        //
        // P.S : the groups can be found in Match.Groups["groupName"], so they are used to extract data

        Regex regex = new Regex(regexp);
        private string url;

        UrlData FirstUrl = new UrlData();
        UrlData LastUrl =new UrlData();
        private bool hasStopURL;

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string text = textBox1.Text;
            if (Match(text, ref FirstUrl))
            {
                button1.Enabled = true;
                textBox2.Text = FirstUrl.Manga;
                label3.Text = string.Format("::First::\nChapter : {0} / Page : {1}", FirstUrl.Chapter, FirstUrl.Page);
            }
            else
            {
                label3.Text = "::First::";
                button1.Enabled = false;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            url = textBox1.Text;
            if (!Match(url, ref FirstUrl)) return;
            if (FirstUrl.Page == "")
            {
                InitTo1();
            }
            hasStopURL = (textBox3.Text != "" && Match(textBox3.Text, ref LastUrl));
            if (hasStopURL && LastUrl.Page == "")
                LastUrl.Page = "1";
                
            label3.Text = string.Format("::First::\nChapter : {0} / Page : {1}",FirstUrl.Chapter, FirstUrl.Page);
            panel1.Enabled = false;
            thread = new Thread(new ThreadStart(Download));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        private void InitTo1()
        {
            if (!url.EndsWith("/"))
                url += "/";
            url += "1";
            FirstUrl.Page = "1";
        }

        private void Download()
        {
            string localURL = url;
            string hRef = "";
            string directory = Path.Combine(LocalDir, textBox2.Text);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            while (true)
            {
                Match(localURL,ref FirstUrl);
                if (FirstUrl.Page == "")
                {
                    InitTo1();
                }
                label4.Text = string.Format("::Current::\nChapter : {0} / Page : {1}", FirstUrl.Chapter, FirstUrl.Page);
                string fileName = string.Format("{0:D4} - {1:D2}.png", int.Parse(FirstUrl.Chapter), int.Parse(FirstUrl.Page));
                string file = Path.Combine(directory, fileName);
                if (!Pxtractor.DownloadLargestImage(localURL, file, out hRef))
                    break;               

                if (hasStopURL && FirstUrl.Equals(LastUrl))
                    break;
                localURL = hRef;
            }
            panel1.Enabled = true;
            MessageBox.Show("Done!");
            Thread.CurrentThread.Abort();
        }
        private bool Match(string url,ref UrlData urlData)
        {
            Match match = regex.Match(url);
            if (match.Success)
            {
                urlData.Category = match.Groups["categ"].Value;
                urlData.Manga= match.Groups["manga"].Value;
                urlData.Chapter = match.Groups["chapter"].Value;
                urlData.Page = match.Groups["page"].Value;
            }
            return match.Success;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (thread != null) thread.Abort();
            panel1.Enabled = true;
        }

        private void label3_MouseHover(object sender, EventArgs e)
        {
            Control control = (Control) sender;
            toolTip1.Show(control.Text, control);
        }
    }
}
