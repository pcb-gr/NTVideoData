using HtmlAgilityPack;
using NTVideoData.Victims;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NTVideoData.Util
{
    public class WebParserUtil
    {
        static HtmlWeb web = new HtmlWeb();
        static int[] random = { 1, 2, 3, 4, 5 };

        private static int getRandomNumber()
        {
            int SLEEP_TIME = new Random().Next(0, random.Count());
            return random[SLEEP_TIME] * 1000;
        }

        public static string getContentByUrl(string url)
        {
            var webRequest = WebRequest.Create(url);

            using (var response = webRequest.GetResponse())
            using (var content = response.GetResponseStream())
            using (var reader = new StreamReader(content))
            {
                var strContent = reader.ReadToEnd();
                if (url.IndexOf("phimmoi") != -1) Thread.Sleep(getRandomNumber());
                return strContent;
            }
        }

        public static HtmlNode convertDomStringToHtmlNode(string domString)
        {
            HtmlNode.ElementsFlags.Remove("\"");
            HtmlDocument html = new HtmlDocument();
            html.OptionOutputAsXml = false;
            html.LoadHtml(domString);
            return html.DocumentNode;
        }

        public static HtmlDocument getRootDocument(string url)
        {
            HtmlDocument rs= null;
            try
            {
                BaseVictim.instance().logForm.append("Begin loadURl: " + url);
                //url = "http://hdonline.vn/frontend/episode/xmlplay?ep=1&fid=14128&token=NmY1MjQyNDQ0YzMwNjM2YzMzNTg1NDZkNjI1ODRlNDc1NTMyNWE0YjYxNTg2MTRkMzM2YjU4MzU0NTY5NmY2YTM4NzczZDNk-1501811714&mirand=587c41a0cd52ad49975cf10ed87a9b02&_x=0.5166851091972489&format=json";
                //url = "http://hdonline.vn/";
                //getContentByUrl(url);
                rs = web.Load(url);
            } catch (Exception em)
            {
                BaseVictim.instance().logForm.append(em.Message);
            }
            if (url.IndexOf("phimmoi") != -1)
            {
                Thread.Sleep(getRandomNumber());
                BaseVictim.instance().logForm.append("Begin sleep for Phimmoi.net: " + getRandomNumber());
            } 
            if (rs != null && rs.DocumentNode.SelectSingleNode("//body") == null)
            {
                BaseVictim.instance().logForm.append("The ip is blocked by Phimmoi.net. Begin Reget content: " + getRandomNumber());
                getRootDocument(url);
            }
            return (rs == null) ? getRootDocument(url) : rs;
        }

        public static HtmlNode selectSingleNode(string url, string selector)
        {
            return getRootDocument(url).DocumentNode.SelectSingleNode(selector);
        }

        public static HtmlNodeCollection selectNodes(string url, string selector)
        {
            return getRootDocument(url).DocumentNode.SelectNodes(selector);
        }
    }
}
