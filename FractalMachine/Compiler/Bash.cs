using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace FractalMachine.Compiler
{
    public class Bash
    {
        Environment env;
        Thread response;
        Process process;
        List<string> lines;

        public Bash()
        {
            Start();
        }

        public void Start()
        {
            env = Environment.Current;

            process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"F:\msys64\usr\bin\bash.exe",
                    Arguments = $"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                }
            };

            process.Start();

            response = new Thread(AsyncResponse);
            response.Start();

            Thread.Sleep(1000);

            //string result = process.StandardOutput.ReadToEnd();
            //process.WaitForExit();

        }

        public void ExecuteCommand(string cmd)
        {
            process.StandardInput.WriteLine(cmd);
        }

        public void AsyncResponse()
        {
            while (true)
            {
                var s = process.StandardOutput.ReadLine();
                if (!String.IsNullOrEmpty(s))
                    lines.Add(s);
                Thread.Sleep(1);
            }
        }

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
