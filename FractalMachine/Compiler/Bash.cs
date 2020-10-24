using FractalMachine.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace FractalMachine.Compiler
{
    public class Bash
    {
        public enum InstanceType
        {
            Top,
            Helper
        }

        Bash helper;
        Environment env;        
        Process process;
        Thread thResponse, thError;
        int defaultModules = 0;

        public List<string> Lines = new List<string>();
        public List<string> LinesError = new List<string>();

        public Bash(Environment Environment = null, InstanceType instanceType = InstanceType.Top)
        {
            if (Environment == null)
                Environment = Environment.Current;

            env = Environment;

            if (instanceType == InstanceType.Top)
                helper = new Bash(Environment, InstanceType.Helper);

            Start();

            Thread.Sleep(1000);
            ExecuteCommand("#! /bin/bash && set -m");
        }

        public void Start()
        {
            process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = env.Path,
                    Arguments = "--login -i",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                }
            };

            /*if(env.Platform == PlatformID.Win32NT)
            {
                process.StartInfo.Arguments = "/C " + env.Path + " " + process.StartInfo.Arguments;
                process.StartInfo.FileName = "cmd.exe";
            }*/

            process.Start();
            defaultModules = process.Modules.Count;

            thResponse = new Thread(AsyncResponse);
            thResponse.Start();

            thError = new Thread(AsyncError);
            thError.Start();

            Thread.Sleep(1000);

            //string result = process.StandardOutput.ReadToEnd();
            //process.WaitForExit();

        }

        public void Stop()
        {
            process.WaitForExit();
            thResponse.Abort();
        }

        public string ExecuteCommand(string cmd)
        {
            process.StandardInput.WriteLine(cmd);

            string res = "";

            var l = 0;
            while (Lines.Count == 0 || Lines.Count > l)
            {
                l = Lines.Count;
                Thread.Sleep(10);
            }

            while (Lines.Count > 0) res += Lines.Pull() + "\n";

            return res;
        }

        public void Clear()
        {
            Lines.Clear();
            LinesError.Clear();
        }

        public Execution NewExecution(string cmd = "")
        {
            var exe = new Execution(this);
            return exe;
        }

        public void AsyncResponse()
        {
            while (true)
            {
                var s = process.StandardOutput.ReadLine();
                if (!String.IsNullOrEmpty(s))
                    Lines.Add(s);
            }
        }

        public void AsyncError()
        {
            while (true)
            {
                var s = process.StandardError.ReadLine();
                if (!String.IsNullOrEmpty(s))
                    LinesError.Add(s);
            }
        }

        public class Execution
        {
            Bash bash;

            public string Command;
            public string[] OutLines, OutErrors;

            public Execution(Bash Bash)
            {
                bash = Bash;
            }

            public void Run()
            {
                bash.process.StandardInput.WriteLine(Command);
                //var test = bash.GetProcessChildren();
                //while (bash.GetProcessChildren().Length > 0) ;
                while (bash.process.Modules.Count > bash.defaultModules) ;

                OutLines = bash.Lines.ToArray();
                OutErrors = bash.LinesError.ToArray();
                bash.Clear();
            }
        }

        #region Helper

        public int[] GetProcessChildren()
        {
            List<int> children = new List<int>();
            var pid = process.Id.ToString();

            var res = helper.ExecuteCommand("ps -f");
            var lines = res.Split('\n'); // yes, this is a little stupid
            for(int i=1; i<lines.Length-1; i++)
            {
                var tpos = 0;
                string prev = "";
                var tt = lines[i].Split(" ");
                foreach(var t in tt)
                {
                    if (!String.IsNullOrEmpty(t))
                    {
                        if(tpos == 2 && t == pid)
                        {
                            children.Add(Convert.ToInt32(prev));
                        }
                        prev = t;
                        tpos++;
                    }
                }
            }

            return children.ToArray();
        }

        #endregion

        #region Static

        public static string ExecuteWindowsCmd(string Cmd)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C "+Cmd;
            process.StartInfo = startInfo;
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }

        #endregion
    }
}
