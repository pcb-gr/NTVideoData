using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NTVideoData.Util
{
    class CMDUtil
    {
        

        public static void startMysql()
        {
            var process = new Process();
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "batch\\startDb.bat";
            process.StartInfo = startInfo;
            process.Start();
        }

       
        public static bool callDownloadByFFmpeg(string src, string path, string fileName)
        {
            string doneFile = "done.txt";
            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.Start();
            path = path + fileName;
            string downloadCommandLine = "ffmpeg -i \"" + src + "\" \"" + path + fileName + "\">done.txt";
            process.StandardInput.WriteLine(downloadCommandLine);
            process.WaitForExit();
            while(!File.Exists(doneFile))
            {
                Thread.Sleep(1000);
            }
            File.Delete(doneFile);
            process.Kill();
            return true;
        }

        public static void callDownLoadByIDM(string src, string path, string fileName)
        {
            //src = "http://localhost:85/tools/fb/chunk2.mp4";
            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.Start();
            string s = "idman.exe /n /d \"" + src + "\" /p \"" + path + "\" /f \"" + fileName + "\"";
            process.StandardInput.WriteLine(s);
            while (!File.Exists(path + "/" + fileName));
        }

        public static bool checkPort(int port)
        {
            var checkPortCommandLine = @"netstat -o -n -a | findstr 0.0:" + port;
            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.Start();
            process.StandardInput.WriteLine(checkPortCommandLine);
            process.StandardInput.WriteLine("exit");
            var output = process.StandardOutput.ReadToEnd();
            return (output.IndexOf("LISTENING") != -1);
        }

        public static void startApache()
        {
            var pathApache = @"D:\wamp2.4-32\bin\apache\Apache2.4.4\bin\httpd.exe";
            var process = new Process();
            process.StartInfo.FileName = pathApache;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            //process.Start();
        }
    }
}
