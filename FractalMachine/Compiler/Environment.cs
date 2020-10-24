using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Compiler
{
    public class Environment
    {
        #region Static 

        public static string[] ListEnvironments()
        {
            var list = new List<string>();

            var osVersion = System.Environment.OSVersion;

            if(osVersion.Platform == PlatformID.Win32NT)
            {
                // Search msys
                var res = Resources.SearchFile("msys2.exe", 2); //with 2 search just in the root
                if (!String.IsNullOrEmpty(res)) list.Add(res);

                // Search cygwin
                res = Resources.SearchFile("Cygwin.bat", 2); 
                if (!String.IsNullOrEmpty(res)) list.Add(res);               
            }
            else
            {
                list.Add("/bin/bash");
            } 

            

            return list.ToArray();
        }

        static Environment current;

        public static Environment Current
        {
            get
            {
                if (current == null)
                {
                    var list = ListEnvironments();
                    new Environment(list[0]);
                }

                return current;
            }
        }

        #endregion

        public enum EnvironmentType
        {
            Bash,
            Cygwin,
            MSYS2
        }

        public EnvironmentType Type;
        public string Path;
        public PlatformID Platform;

        public Environment(string Path)
        {
            var name = System.IO.Path.GetFileName(Path);
            switch (name)
            {
                case "bash":
                    Type = EnvironmentType.Bash;
                    break;

                case "Cygwin.bat":
                    Type = EnvironmentType.Cygwin;
                    break;

                case "msys2.exe":
                    Type = EnvironmentType.MSYS2;
                    Path = Path.Substring(0, Path.Length - System.IO.Path.GetFileName(Path).Length);
                    Path += @"usr\bin\bash.exe";
                    break;
            }

            this.Path = Path;
            this.Platform = System.Environment.OSVersion.Platform;

            SetCurrent();
        }

        public void SetCurrent()
        {
            current = this;
        }
    }
}
