using FractalMachine.Code;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace FractalMachine.Compiler
{
    public class Environment
    {
        #region Static

        static Environment current;
        public static Environment GetEnvironment
        {
            get
            {
                if (current == null)
                    current = new Environment();

                return current;
            }
        }

        #endregion

        PlatformID Platform;
        public string Path = "/usr/bin/";

        public Environment()
        {
            var osVersion = System.Environment.OSVersion;
            Platform = osVersion.Platform;

            if (Platform == PlatformID.Win32NT)
            {
                if (!Directory.Exists("cygwin64-light"))
                {
                    Console.WriteLine("Preparing cygwin64-light environment, just few minutes");

                    if(!File.Exists(cygwinDownloadZipPath))
                        startCygwinDownload();
                }

                Path = "cygwin64-light" + Path;
            }
        }

        #region DownloadCygwin
        string cygwinDownloadZipPath = Properties.TempDir + "cygwin64-light.zip";
        int cygwinDownloadLastPercentage;
        bool cygwinDownloadEnded;
        private void startCygwinDownload()
        {
            cygwinDownloadEnded = false;
            cygwinDownloadLastPercentage = -1;

            WebClient client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
            client.DownloadFileAsync(new Uri(Properties.CygwinDownloadUrl), cygwinDownloadZipPath);

            while (!cygwinDownloadEnded) ;
        }
        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            int percentage = (int)(bytesIn / totalBytes * 100);
            if (cygwinDownloadLastPercentage != percentage)
            {
                Console.Write((int)percentage + "% ");
                cygwinDownloadLastPercentage = percentage;
            }
        }
        void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        { 
            cygwinDownloadEnded = true;
            Console.WriteLine("\r\nCompleted!");
        }
        #endregion

        public Command ExecuteCommand(string command)
        {
            var cmd = new Command(this);

            var splitCmd = command.Split(' ');

            cmd.Process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path,
                    Arguments = $"",                    
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                }
            };

            return cmd;
        }
    }

    public class Command
    {
        public Process Process;
        public Environment Environment;
        public string[] OutLines, OutErrors;

        public Command(Environment environment)
        {
            Environment = environment;
        }

        public void Run()
        {
            Process.Start();

            while (!Process.HasExited) ;

            List<string> lines = new List<string>(), err = new List<string>();
            string s;
            while ((s = Process.StandardOutput.ReadLine()) != null) lines.Add(s);
            while ((s = Process.StandardError.ReadLine()) != null) err.Add(s);
            OutLines = lines.ToArray();
            OutErrors = lines.ToArray();
        }
    }
}
