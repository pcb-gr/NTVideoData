using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace OpenLoadGetLink
{
    class Program
    {
        const string UserAgent = "Mozilla / 5.0(Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1";
        const string NumbersJs = "https://openload.co/assets/js/obfuscator/n.js";

        static void Main(string[] args)
        {
            Console.WriteLine("OpenLoad Stream URL Extractor by gdkchan");
            Console.WriteLine("This is basically just a small test and not guaranteed to work");

            Console.Write(Environment.NewLine);

            Console.WriteLine("Type or paste the URL of the file below:");

            string URL = Console.ReadLine();

            URL = GetStreamURL(URL);

            Console.Write(Environment.NewLine);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Download URL:");
            Console.ResetColor();

            Console.WriteLine(URL);

            Console.Write(Environment.NewLine);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static string GetStreamURL(string URL)
        {
            string HTML = HttpGet(URL);
            string NJs = HttpGet(NumbersJs);

            string LinkImg = Regex.Match(HTML, "src=\"data:image/png;base64,([A-Za-z0-9+/=]+?)\"").Groups[1].Value;
            string SigNums = Regex.Match(NJs, "window\\.signatureNumbers='([a-z]+?)'").Groups[1].Value;

            byte[] ImgData = Convert.FromBase64String(LinkImg);

            string ImgNums = string.Empty;

            using (MemoryStream MS = new MemoryStream(ImgData))
            {
                using (Bitmap Img = new Bitmap(MS))
                {
                    for (int Y = 0; Y < Img.Height; Y++)
                    {
                        for (int X = 0; X < Img.Width; X++)
                        {
                            Color Col = Img.GetPixel(X, Y);

                            if (Col == Color.FromArgb(0, 0, 0))
                            {
                                //Black color = end of data
                                Y = Img.Height;
                                break;
                            }

                            ImgNums += (char)Col.R;
                            ImgNums += (char)Col.G;
                            ImgNums += (char)Col.B;
                        }
                    }
                }
            }

            string[,] ImgStr = new string[10, ImgNums.Length / 200];
            string[,] SigStr = new string[10, SigNums.Length / 260];

            for (int i = 0; i < 10; i++)
            {
                //Fill Array of Image String
                for (int j = 0; j < ImgStr.GetLength(1); j++)
                {
                    ImgStr[i, j] = ImgNums.Substring(i * ImgStr.GetLength(1) * 20 + j * 20, 20);
                }

                //Fill Array of Signature Numbers
                for (int j = 0; j < SigStr.GetLength(1); j++)
                {
                    SigStr[i, j] = SigNums.Substring(i * SigStr.GetLength(1) * 26 + j * 26, 26);
                }
            }

            List<string> Parts = new List<string>();

            int[] Primes = { 2, 3, 5, 7 };

            foreach (int i in Primes)
            {
                string Str = string.Empty;
                float Sum = 99f; //c

                for (int j = 0; j < SigStr.GetLength(1); j++)
                {
                    for (int ChrIdx = 0; ChrIdx < ImgStr[i, j].Length; ChrIdx++)
                    {
                        if (Sum > 122f) Sum = 98f; //b

                        char Chr = (char)((int)Math.Floor(Sum));

                        if (SigStr[i, j][ChrIdx] == Chr && j >= Str.Length)
                        {
                            Str += ImgStr[i, j][ChrIdx];
                            Sum += 2.5f;
                        }
                    }
                }

                Parts.Add(Str.Replace(",", string.Empty));
            }

            string StreamURL = "https://openload.co/stream/";

            StreamURL += Parts[3] + "~";
            StreamURL += Parts[1] + "~";
            StreamURL += Parts[2] + "~";
            StreamURL += Parts[0];

            return StreamURL;
        }

        private static string HttpGet(string URL)
        {
            WebRequest Request = WebRequest.Create(URL);
            ((HttpWebRequest)Request).UserAgent = UserAgent;

            WebResponse Response = Request.GetResponse();
            StreamReader Reader = new StreamReader(Response.GetResponseStream());

            return Reader.ReadToEnd();
        }
    }
}
