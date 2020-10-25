using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

                }

                Path = "cygwin64-light" + Path;
            }
        }

        public Command ExecuteCommand(string command)
        {
            var cmd = new Command(this);

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
