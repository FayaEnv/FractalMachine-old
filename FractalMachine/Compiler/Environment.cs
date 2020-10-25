using FractalMachine.Code;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;

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

        public PlatformID Platform;
        public string Path = "/bin/";
        public Repository Repository;
        public string Arch;

        public Environment()
        {
            if (!System.Environment.Is64BitProcess)
                throw new Exception("32 bit currently not supported. Update your self, upgrade to 64 bit!");

            Platform = System.Environment.OSVersion.Platform;

            if (Platform == PlatformID.Win32NT)
            {
                Repository = new Repository.Cygwin(this);

                if (!Directory.Exists(Repository.Dir))
                {
                    Console.WriteLine("First time? You are welcome!");
                    Console.WriteLine("Preparing cygwin64-light environment, just few minutes");

                    if(!File.Exists(cygwinDownloadZipPath)) //if dubbio
                        startCygwinDownload();

                    Console.WriteLine("Extracting zip archive...");
                    ZipFile.ExtractToDirectory(cygwinDownloadZipPath, "./");                   
                    Console.WriteLine("Completed!");
                }

                Path = "cygwin64-light" + Path;
            }
            else
            {
                Repository = new Repository.ArchLinux(this);
            }

            Arch = ExecCmd("arch").Split('\n')[0];
            Repository.Update();
        }

        #region DownloadCygwin
        string cygwinDownloadZipPath = Properties.TempDir + "cygwin64-light.zip";
        int cygwinDownloadLastPercentage;
        bool cygwinDownloadEnded;
        internal void startCygwinDownload(/* "hack" */ string url = null, string output = null) 
        {
            cygwinDownloadEnded = false;
            cygwinDownloadLastPercentage = -1;

            WebClient client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
            client.DownloadFileAsync(new Uri(url ?? Properties.CygwinDownloadUrl), output ?? cygwinDownloadZipPath);

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

        /* 4 LINUX
         * Set temporary dynamic linking dir: https://unix.stackexchange.com/questions/24811/changing-linked-library-for-a-given-executable-centos-6
         * 
         */

        public Command NewCommand(string command)
        {
            var cmd = new Command(this);

            cmd.Process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path + "bash",
                    Arguments = $"-login -c '"+ command, // 2>&1 | tee test.txt               
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                }
            };

            return cmd;
        }

        public string ExecCmd(string cmd)
        {
            var comm = NewCommand(cmd);
            comm.Run();

            string res = "";
            foreach (string s in comm.OutLines) res += s + "\n";
            foreach (string s in comm.OutErrors) res += s + "\n";
            return res;
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
            Process.StartInfo.Arguments += "' 2>&1 | tee test.txt";
            Process.Start();
            bool peak = false;
            int startModules = Process.Modules.Count, currentModules = 0, ticks = 0;

            while (!Process.HasExited && !(peak && currentModules <= startModules))
            {
                currentModules = Process.Modules.Count;
                if (currentModules > startModules) peak = true;
                Thread.Sleep(10);
                if (ticks++ > 100) Process.Start();
            }

            List<string> lines = new List<string>(), err = new List<string>();
            string s;
            while ((s = Process.StandardOutput.ReadLine()) != null) lines.Add(s);
            while ((s = Process.StandardError.ReadLine()) != null) err.Add(s);
            OutLines = lines.ToArray();
            OutErrors = err.ToArray();
        }

        public void AddArgument(string arg)
        {
            Process.StartInfo.Arguments += " " + arg;
        }
    }
}
