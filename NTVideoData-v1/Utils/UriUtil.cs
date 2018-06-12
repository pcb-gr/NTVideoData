using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTVideoData.Util
{
    class UriUtil
    {
        public static string[] domainExtension = { ".com", ".vn", ".net", ".info", ".co", ".org" };
        public static string getDomain(string url)
        {
            Uri uri = new Uri(url);
            return uri.Host;
        }

        public static bool hasDomain(string url)
        {
            foreach (string ext in domainExtension)
            {
                if(url.IndexOf(ext) != -1)
                {
                    return true;
                }
            }
            return false;
        }

        public static string checkAndAddDomain(string url, string domain, string protocol)
        {
            if (url.IndexOf(domain) == -1)
            {
                url = protocol + domain + ((!url[0].Equals('/')) ? ("/" + url) : url);
            }
            return url;
        }

        public static string getUrlNoDomain(string url)
        {
            List<string> temp = new List<string>();
            var parts = url.Replace("http://", "").Replace("https://", "").Split('/');
            for(int i = 1; i <  parts.Length; i ++)
            {
                if (parts[i] != "")
                {
                    temp.Add(parts[i]);
                }
            }
            return "/" + string.Join("/", temp.ToArray());
        }
    }
}
