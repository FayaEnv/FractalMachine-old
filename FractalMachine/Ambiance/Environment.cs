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

using FractalMachine.Code;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace FractalMachine.Ambiance
{
    public abstract class Environment
    {
        #region Static

        static Environment current;

        public static Environment GetEnvironment
        {
            get
            {
                if (current == null)
                {
                    var Platform = System.Environment.OSVersion.Platform;

                    if (Platform == PlatformID.Win32NT)
                        current = new Environments.Windows.Windows();
                    else
                        current = new Environments.Unix.Unix();
                }

                return current;
            }
        }

        public static string ExecutionDirectory
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().Location;
            }
        }

        #endregion

        #region Dynamic

        internal string sysPath, shell;
        internal string exeFormat = "";
        internal string binPath = "";
        internal Environment parent;
        internal Compiler compiler;
        internal Repository repository;

        public Environment()
        {
            //Arch = ExecCmd("arch").Split('\n')[0];
            //Repository.Update();
        }

        #region Subsystems

        internal Dictionary<string, Environment> subsystems = new Dictionary<string, Environment>();
        internal Environment subsystem;

        public Environment SelectSubsystem(string name)
        {
            Environment ret;
            if (subsystems.TryGetValue(name, out ret))
            {
                subsystem = ret;
                ret.init();
            }

            return ret;
        }

        internal virtual void init() { }

        #endregion

        #region Properties

        // Path.DirectorySeparatorChar... e vabbè, ma PathChar è più bella
        abstract public char PathChar { get; }

        public Compiler Compiler { 
            get 
            {
                return subsystem?.Compiler ?? compiler;
            } 
        }

        public Repository Repository
        {
            get
            {
                return subsystem?.Repository ?? repository;
            }
        }

        public Environment Top
        {
            get
            {
                return parent?.Top ?? this;
            }
        }

        #endregion

        #region Methods


        /* 4 LINUX
         * Set temporary dynamic linking dir: https://unix.stackexchange.com/questions/24811/changing-linked-library-for-a-given-executable-centos-6
         */

        public Command NewCommand(string command = "")
        {
            var cmd = new Command(this);
            cmd.Cmd = command;
            return cmd;
        }

        public string ExecCmd(string cmd)
        {
            var comm = NewCommand(cmd);
            comm.Run();

            string res = "";
            foreach (string s in comm.OutLines) res += s + "\n";
            foreach (string s in comm.OutErrors) res += "ERR! " + s + "\n";
            return res;
        }

        public virtual string Path(string Path)
        {
            return subsystem?.Path(Path) ?? Path;
        }

        #endregion

        #endregion
    }
}
