using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Ambiance
{
    public abstract class Repository
    {
        Environment env;

        public Repository()
        {

        }

        abstract public void Search(string query);
        abstract public void Info(string query);
        abstract public void List(string query);
        abstract public void Upgrade(string query);
        abstract public void Update();
        abstract public InstallationResult Install(string Package, bool Dependency = false);

        #region Enums

        public enum InstallationResult
        {
            PackageNotFound,
            Success,
            PackageYetInstalled,
            Error
        }

        #endregion

        #region Structs
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
    }
}
