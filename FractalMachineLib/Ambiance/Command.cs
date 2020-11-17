/*
   Copyright 2020 (c) Riccardo Cecchini
   
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace FractalMachineLib.Ambiance
{
    public class Command
    {
        /// <summary>
        /// Revelant in case of DirectCall == false. It works only in Unix
        /// </summary>
        public bool UseStdWrapper = true;
        public bool DirectCall = false;
        public Process Process;
        public Environment Env;
        public string[] OutLines, OutErrors;

        public string arguments = "";
        public string Cmd = "";

        public Command(Environment environment)
        {
            Env = environment;
        }

        void createProcess()
        {
            string call = "";
            string args = "";

            if (DirectCall)
            {
                var splitCmd = Cmd.Split(' ');

                call = Env.sysPath + Env.binPath + splitCmd[0];

                for (int c = 1; c < splitCmd.Length; c++)
                    args += splitCmd[c] + ' ';
                args += arguments + ' ';
            }
            else
            {
                call = Env.sysPath + Env.binPath + Env.shell;
                args = $"-login -c '" + Cmd + " " + arguments;
                if (UseStdWrapper) args += " > /home/out.txt 2>/home/err.txt"; // 2>&1 | tee out.txt
                args += "'";
            }

            Process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = call,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                }
            };
        }

        public void Run()
        {
            createProcess();

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

            Process.StandardInput.Flush();
            Process.StandardInput.Close();

            OutLines = OutErrors = new string[0];
            DateTime endProcess = DateTime.Now;

            if (UseStdWrapper && !DirectCall)
            {
                // Load file errors
                var fnOut = Env.sysPath + "/home/out.txt";
                var fnErr = Env.sysPath + "/home/err.txt";

                // Yes, this is a little ugly
                while (!Resources.IsFileReady(fnOut) || !Resources.IsFileReady(fnErr))
                {
                    if (DateTime.Now.Subtract(endProcess).TotalSeconds > 0)
                        break;

                    Thread.Sleep(10);
                }

                if (Resources.IsFileReady(fnOut))
                    OutLines = File.ReadAllLines(fnOut);

                if (Resources.IsFileReady(fnErr))
                    OutErrors = File.ReadAllLines(fnErr);
            }
            else
            {
                var taskStdOut = Process.StandardOutput.ReadToEndAsync();
                var taskStdErr = Process.StandardOutput.ReadToEndAsync();

                while (!taskStdOut.IsCompleted || !taskStdErr.IsCompleted)
                {
                    if (DateTime.Now.Subtract(endProcess).TotalSeconds > 0)
                        break;

                    Thread.Sleep(10);
                }

                if (taskStdOut.IsCompleted)
                    OutLines = taskStdOut.Result.Split("\n");

                if (taskStdErr.IsCompleted)
                    OutErrors = taskStdErr.Result.Split("\n");
            }
        }

        public void AddArgument(string arg, string ass = null)
        {
            if (ass == null)
            {
                arguments += " " + arg;
            }
            else
            {
                arguments += " " + arg + " " + ass;
            }
        }
    }
}
