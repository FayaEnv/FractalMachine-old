using FractalMachine.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace FractalMachine.Ambiance
{
    abstract public class Repository
    {
        Dictionary<string, Package> packages = null;
        Dictionary<string, InstalledPackage> installedPackages = null;

        public abstract string Dir { get; }
        public abstract string Mirror { get; }

        internal virtual string setupDir { get { return Dir ; } }
        internal virtual string setupJsonName { get { return setupDir + "setup.json"; } }
        internal virtual string setupInstalled { get { return setupDir + "installed.json"; } }
        
        public Environment Env { get; set; }
        public Repository(Environment env)
        {
            Env = env;

            if (File.Exists(setupDir)) load();
        }

        abstract public void Search(string query);
        abstract public void Info(string query);
        abstract public void List(string query);
        abstract public void Upgrade(string query);
        abstract public void Update();

        #region Install

        List<string> installQueue;
        public virtual InstallationResult Install(string Package, bool Dependency = false)
        {
            if (!Dependency)
            {
                load();
                installQueue = new List<string>();
            }

            // Check if package is not yet installed
            InstalledPackage ipckg;
            if (installedPackages.TryGetValue(Package, out ipckg) || installQueue.Contains(Package))
                return InstallationResult.PackageYetInstalled;

            installQueue.Add(Package);

            Package package;
            if (!packages.TryGetValue(Package, out package))
                return InstallationResult.PackageNotFound;

            if (package.Dependencies != null)
            {
                foreach (var req in package.Dependencies)
                {
                    Console.WriteLine("Check for dependency " + req);
                    var rqIn = Install(req, true);
                    if (rqIn == InstallationResult.Error)
                    {
                        Console.WriteLine("Unable to install dependency " + req + "!");
                        Console.WriteLine("Aborted :(");
                        return InstallationResult.Error;
                    }
                }
            }

            var fn = Mirror + package.FileName;

            Console.WriteLine("Downloading " + Package + " package...");
            var installingPath = Properties.TempDir + "installing.tar.xz";
            Env.startCygwinDownload(fn, installingPath);

            var installPckgs = new InstalledPackage();
            installPckgs.Name = package.Name;
            if (!Dependency) installPckgs.DirectlyInstalled = true;
            installPckgs.Version = package.Version;

            // elaborate directory tree
            List<string> tree = new List<string>();
            var lines = tarListContent(installingPath);
            for (int l = 0; l < lines.Length; l++)
            {
                var line = lines[l];
                while (line.Contains("  ")) line = line.Replace("  ", " ");

                var parts = line.Split(' ');
                if (parts.Length >= 5)
                {
                    var path = Dir + parts[5];

                    if (!(Directory.Exists(path) || File.Exists(path)))
                        tree.Add(parts[5]);
                }
            }
            installPckgs.Tree = tree.ToArray();

            //extract files                             
            var res = tarExtract(installingPath, "/");

            installedPackages[installPckgs.Name] = installPckgs;
            updateInstalledPackages();

            if (!Dependency) installQueue = null;

            Console.WriteLine(Package + " installed!");

            return InstallationResult.Success;
        }

        #endregion

        #region Functions

        public virtual string AssertPath(string Path)
        {
            return Path;
        }

        internal virtual string[] tarListContent(string filename)
        {
            var res = Env.ExecCmd("tar -tvf " + filename);

            if (res.Contains("tar: Error"))
                throw new Exception(res);

            return res.Split("\n");
        }

        internal virtual string tarExtract(string filename, string directory = null)
        {
            var cmd = "tar -xf " + filename;
            if (directory != null) cmd += " --directory " + directory;

            var res = Env.ExecCmd(cmd);

            if (res.Contains("tar: Error"))
                throw new Exception(res);

            return res;
        }

        internal void load()
        {
            loadPackages();
            loadInstalledPackages();
        }

        void loadPackages()
        {
            if (packages == null && File.Exists(setupJsonName))
            {
                var json = File.ReadAllText(setupJsonName);
                packages = JsonConvert.DeserializeObject<Dictionary<string, Package>>(json);
            }
        }

        void loadInstalledPackages()
        {
            if (installedPackages == null && File.Exists(setupInstalled))
            {
                var json = File.ReadAllText(setupInstalled);
                installedPackages = JsonConvert.DeserializeObject<Dictionary<string, InstalledPackage>>(json);
            }
        }

        public void updateInstalledPackages()
        {
            if (installedPackages != null)
            {
                var json = JsonConvert.SerializeObject(installedPackages, Formatting.Indented);
                File.WriteAllText(setupInstalled, json);
            }
        }

        #endregion

        #region Struct

        public class Package
        {
            public string Name;
            /// <summary>
            /// Only Arch
            /// </summary>
            public string Base;
            public string Repo;
            public string Arch;
            public string Version;
            /// <summary>
            /// Only Cygwin
            /// </summary>
            public string Category;
            public string Description;
            /// <summary>
            /// Only Cygwin
            /// </summary>
            public string LongDescription;
            public string FileName;
            /// <summary>
            /// Only Cygwin
            /// </summary>
            public string Source;
            public int CompressedSize;
            public int SourceCompressedSize;
            /// <summary>
            /// Only Cygwin
            /// </summary>
            public string SHA = "";
            /// <summary>
            /// Only Cygwin
            /// </summary>
            public string SourceSHA = "";
            /// <summary>
            /// Only Arch
            /// </summary>
            public int InstalledSize;
            /// <summary>
            /// Only Arch
            /// </summary>
            public string BuildDate;
            /// <summary>
            /// Only Arch
            /// </summary>
            public string UpdateDate;
            /// <summary>
            /// Only Arch
            /// </summary>
            public string[] Provides;
            public string[] Replaces;
            /// <summary>
            /// Only Arch
            /// </summary>
            public string[] Licenses;
            public string[] Dependencies;
            public string[] OptionalDependencies;
            public string[] MakeDependencies;
            /// <summary>
            /// Only Arch
            /// </summary>
            public string[] CheckDependencies;
        }

        public class InstalledPackage
        {
            public string Name;
            public string Version;
            public string[] Tree;
            public bool DirectlyInstalled;
        }

        #endregion

        #region Enums

        public enum InstallationResult
        {
            PackageNotFound,
            Success,
            PackageYetInstalled,
            Error
        }

        #endregion

        #region Classes
        public class Cygwin : Repository
        {
            public override string Mirror { get { return "https://ftp-stud.hs-esslingen.de/pub/Mirrors/sources.redhat.com/cygwin/"; } }
            internal override string setupDir { get { return Dir + "etc/setup/"; } }

            string setupName = "setup.ini";
            string setupInstalledDb = "installed.db";

            public Cygwin(Environment Env) : base(Env) 
            {          
                
            }

            public override string Dir 
            { 
                get { return "cygwin64-light/"; } 
            }

            public override void Search(string query)
            {

            }

            public override void Info(string query)
            {
                //todo
            }        

            public override void List(string query = "")
            {

            }

            public override void Upgrade(string query)
            {

            }

            public override void Update()
            {
                bool download = true;
                if (File.Exists(setupJsonName))
                {
                    var setupLastWrite = File.GetLastWriteTime(setupJsonName);
                    var diff = DateTime.Now.Subtract(setupLastWrite).TotalDays;
                    if (diff < Properties.MaxRepositoryDays) download = false;
                }

                if(download)
                {
                    Console.WriteLine("Updating cygwin repository...");
                    //WebClient webClient = new WebClient(); webClient.DownloadFile();
                    Env.startCygwinDownload(Mirror + Env.Arch + "/"+ setupName, Properties.TempDir + setupName);
                    Console.WriteLine("\r\nDownload completed");

                    ConvertSetupXsToJson();
                    Console.WriteLine("Repository updated!");
                }

                checkInstalledDb();
                load();
            }

            #region Structs

            internal override string[] tarListContent(string filename)
            {
                var res = Env.ExecCmd("tar -tvf " + AssertPath(filename));

                if (res.Contains("tar: Error"))
                    throw new Exception(res);

                return res.Split("\n");
            }

            internal override string tarExtract(string filename, string directory = null)
            {
                var cmd = "tar -xf " + AssertPath(filename);
                if (directory != null) cmd += " --directory " + directory;

                var res = Env.ExecCmd(cmd);

                if (res.Contains("tar: Error"))
                    throw new Exception(res);

                return res;
            }

            #endregion

            #region Additional

            void checkInstalledDb()
            {
                string fn = setupDir + setupInstalledDb;
                if (!File.Exists(setupInstalled))
                {
                    Console.WriteLine("Making installed packages database...");
                    List<string> toDelete = new List<string>();
                    installedPackages = new Dictionary<string, InstalledPackage>();
                    var lines = File.ReadAllLines(fn);
                    for (int l = 1; l < lines.Length; l++)
                    {
                        var package = new InstalledPackage();
                        var spaces = lines[l].Split(' ');
                        var name = package.Name = spaces[0];
                        var version = package.Version = spaces[1].Substring(name.Length + 1);
                        version = version.Substring(0, version.Length - 8); //remove extension
                        package.DirectlyInstalled = spaces[2] == "0" ? false : true;

                        Console.WriteLine("Working on " + name);

                        var list = setupDir + name + ".lst";
                        if (File.Exists(list + ".gz"))
                            Env.ExecCmd("gzip -d " + AssertPath(list + ".gz"));

                        if (File.Exists(list))
                        {
                            var dirs = new List<string>();
                            var lstLines = File.ReadAllLines(list);
                            foreach (var lin in lstLines) dirs.Add(lin);
                            package.Tree = lstLines.ToArray();
                            toDelete.Add(list);
                        }

                        installedPackages[name] = package;
                    }

                    Console.WriteLine("Saving DB");

                    updateInstalledPackages();
             
                    if (Properties.Debugging)
                    {
                        Console.WriteLine("Removing old files");
                        foreach (var del in toDelete)
                            File.Delete(del);
                        File.Delete(fn);
                    }

                    Console.WriteLine("Completed");
                }
            }

            void ConvertSetupXsToJson()
            {
                Dictionary<string, string> nameList = new Dictionary<string, string>()
                {
                    ["sdesc"] = "Description",
                    ["ldesc"] = "LongDescription",
                    ["category"] = "Category",
                    ["requires"] = "Dependencies",
                    ["version"] = "Version",
                    ["build-depends"] = "MakeDependencies",
                    ["install"] = "FileName",
                    ["source"] = "Source",
                };

                string lastName;
                JObject main = new JObject(), currentPackage = null;

                main["$START_INFO"] = currentPackage = new JObject();
                JObject START_INFO = currentPackage;
                START_INFO["name"] = "$START_INFO";

                JArray currentDescriptor = null;
                string[] xs = File.ReadAllLines(Properties.TempDir + setupName);

                if (xs.Length == 0)
                    throw new Exception("Blank setup file!");

                bool dividing = true;
                foreach(var x in xs)
                {
                    if (x.Length > 0 && x[0] == '@')
                    {
                        if(currentPackage != null)
                        {
                            if (currentPackage["Source"] != null)
                            {
                                var jarr = (JArray)currentPackage["Source"];
                                currentPackage["SourceCompressedSize"] = (JValue)jarr[1];
                                currentPackage["SourceSHA"] = (JValue)jarr[2];
                                currentPackage["Source"] = (JValue)jarr[0];
                            }

                            if (currentPackage["FileName"] != null)
                            {
                                var jarr = (JArray)currentPackage["FileName"];
                                currentPackage["CompressedSize"] = (JValue)jarr[1];
                                currentPackage["SHA"] = (JValue)jarr[2];
                                currentPackage["FileName"] = (JValue)jarr[0];
                            }

                            if (currentPackage["LongDescription"] != null)
                            {
                                var ss = "";
                                foreach (JValue s in (JArray)currentPackage["LongDescription"]) ss += s.ToString();
                                currentPackage["LongDescription"] = ss;
                            }

                            if (currentPackage["Category"] != null) currentPackage["Category"] = (JValue)((JArray)currentPackage["Category"])[0];
                            if (currentPackage["Description"] != null) currentPackage["Description"] = (JValue)((JArray)currentPackage["Description"])[0];
                            if (currentPackage["Version"] != null) currentPackage["Version"] = (JValue)((JArray)currentPackage["Version"])[0];

                            currentPackage["Arch"] = (JValue)START_INFO["arch"];
                            currentPackage["Repo"] = (JValue)START_INFO["release"];
                        }

                        currentPackage = new JObject();
                        main[x.Substring(2)] = currentPackage;
                        currentPackage["Name"] = x.Substring(2);
                        currentDescriptor = null;
                    }
                    else
                    {
                        if (currentPackage != null)
                        {
                            var read = x;

                            if (x.Contains(":"))
                            {
                                var xx = x.Split(':');
                                if (!xx[0].Contains(" "))
                                {
                                    if (nameList.TryGetValue(xx[0], out lastName))
                                    {
                                        currentDescriptor = new JArray();
                                        currentPackage[lastName] = currentDescriptor;
                                        read = xx[1].Substring(1);
                                        dividing = true;
                                    }
                                }
                            }

                            if (currentDescriptor != null)
                            {
                                if (read.StartsWith("\""))
                                    dividing = false;

                                if (dividing)
                                {
                                    string[] divide;
                                    if (read.Contains(","))
                                        divide = read.Split(",");
                                    else
                                        divide = read.Split(" ");

                                    foreach (var div in divide)
                                        currentDescriptor.Add(div);
                                }
                                else
                                    currentDescriptor.Add(read);
                            }
                        }
                    }
                }

                // Removed by definition
                var val = main.Last.Value<JProperty>();
                main[val.Name] = null;

                var res = main.ToString();
                File.WriteAllText(setupJsonName, res);
            }

            public override string AssertPath(string path)
            {
                path = Path.GetFullPath(path);
                var fdir = Path.GetFullPath(Dir);
                if (path.StartsWith(fdir))
                {
                    return path.Substring(fdir.Length - 1).Replace('\\', '/');
                }
                else
                {
                    var split = path.Split(":");
                    string ret = "/cygdrive/" + split[0] + split[1].Replace('\\', '/');
                    return ret;
                }
            }

            #endregion
        }

        // https://wiki.archlinux.org/index.php/Official_repositories_web_interface
        public class ArchLinux : Repository
        {
            // https://wiki.archlinux.org/index.php/Official_repositories_web_interface
            public override string Mirror { get { return "todo"; } }

            public ArchLinux(Environment Env) : base(Env) 
            {
                Resources.CreateDirIfNotExists(Dir);
            }

            public override string Dir
            {
                get { return "arch/"; }
            }

            public override void Search(string query)
            {
                //todo
            }

            public override void Info(string query)
            {
                //todo
            }

            public override InstallationResult Install(string Package, bool Depedency = false)
            {
                //todo
                return InstallationResult.PackageNotFound;
            }

            public override void List(string query)
            {
                //todo
            }

            public override void Upgrade(string query)
            {
                //todo
            }

            public override void Update()
            {
                //todo
            }
        }

        /// <summary>
        /// Mac OS X repo
        /// </summary>
        public class Brew : Repository
        {
            // https://formulae.brew.sh/docs/api/
            public override string Mirror { get { return "todo"; } }

            public Brew(Environment Env) : base(Env)
            {
                Resources.CreateDirIfNotExists(Dir);
            }

            public override string Dir
            {
                get { return "brew/"; }
            }

            public override void Search(string query)
            {
                //todo
            }

            public override void Info(string query)
            {
                //todo
            }

            public override InstallationResult Install(string Package, bool Depedency = false)
            {
                //todo
                return InstallationResult.PackageNotFound;
            }

            public override void List(string query)
            {
                //todo
            }

            public override void Upgrade(string query)
            {
                //todo
            }

            public override void Update()
            {
                //todo
            }
        }

        #endregion

        #region Structs

        #endregion

    }
}
