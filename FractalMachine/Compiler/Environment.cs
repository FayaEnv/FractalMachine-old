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
                var res = Resources.SearchFile("msys2_shell.cmd", 2); //with 2 search just in the root
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

        EnvironmentType type;
        string path;

        public Environment(string Path)
        {
            path = Path;

            var name = System.IO.Path.GetFileName(path);
            switch (name)
            {
                case "bash":
                    type = EnvironmentType.Bash;
                    break;

                case "Cygwin.bat":
                    type = EnvironmentType.Cygwin;
                    break;

                case "msys2_shell.cmd":
                    type = EnvironmentType.MSYS2;
                    break;
            }

            SetCurrent();
        }

        public void SetCurrent()
        {
            current = this;
        }
    }
}
