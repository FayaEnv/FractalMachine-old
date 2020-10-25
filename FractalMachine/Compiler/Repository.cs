using FractalMachine.Code;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FractalMachine.Compiler
{
    abstract public class Repository
    {
        public abstract string Dir { get; }

        public Environment Env { get; set; }
        public Repository(Environment env)
        {
            Env = env;
        }

        abstract public void Search(string query);
        abstract public void Info(string query);
        abstract public void Install(string query);
        abstract public void List(string query);
        abstract public void Upgrade(string query);
        abstract public void Update();


        #region Classes
        public class Cygwin : Repository
        {
            public string defaultMirror = "https://ftp-stud.hs-esslingen.de/pub/Mirrors/sources.redhat.com/cygwin/";
            string setupName = "setup.xz";

            public Cygwin(Environment Env) : base(Env) 
            {

            }

            public override string Dir 
            { 
                get { return "cygwin64-light"; } 
            }

            public override void Search(string query)
            {

            }

            public override void Info(string query)
            {

            }

            public override void Install(string query)
            {

            }

            public override void List(string query)
            {

            }

            public override void Upgrade(string query)
            {

            }

            public override void Update()
            {
                bool download = true;
                if (File.Exists(setupName))
                {
                    var setupLastWrite = File.GetLastWriteTime(setupName);
                    var diff = DateTime.Now.Subtract(setupLastWrite).TotalDays;
                    if (diff < Properties.MaxRepositoryDays) download = false;
                }

                if(download)
                {
                    Console.WriteLine("Updating cygwin repository...");
                    var downloaded = Properties.TempDir + "setup.xz";
                    WebClient webClient = new WebClient();
                    webClient.DownloadFile(defaultMirror + Env.Arch + "/"+ setupName, downloaded);
                    Console.WriteLine("Download completed");

                    //todo read
                    string re = "";
                }
            }

            #region Additional

            public static string PathToCygdrive(string path)
            {
                path = Path.GetFullPath(path);
                var split = path.Split(":");
                string ret = "/cygdrive/" + split[0] + split[1].Replace('\\', '/');
                return ret;
            }

            #endregion
        }

        // https://wiki.archlinux.org/index.php/Official_repositories_web_interface
        public class ArchLinux : Repository
        {
            public ArchLinux(Environment Env) : base(Env) { }

            public override string Dir
            {
                get { return "arch"; }
            }

            public override void Search(string query)
            {

            }

            public override void Info(string query)
            {

            }

            public override void Install(string query)
            {

            }

            public override void List(string query)
            {

            }

            public override void Upgrade(string query)
            {

            }

            public override void Update()
            {

            }
        }

        #endregion

        #region Structs

        #endregion

    }
}
