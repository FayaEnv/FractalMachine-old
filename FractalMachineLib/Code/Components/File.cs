using FractalMachineLib.Classes;
using FractalMachineLib.Code.Langs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FractalMachineLib.Code.Components
{
    public class File : Container
    {
        internal bool loaded = false, isMain = false;
        internal string outFileName;
        internal Component parent;
        internal Lang script;

        List<string> includes = new List<string>();
        List<string> usings = new List<string>();

        public File(Component Parent, string Name, Linear Linear, string FileName) : base(Parent, Name, Linear)
        {
            usings.Add("namespace std"); //todo: create script files for C++ for automatic namespace including
            containerType = ContainerTypes.File;
            _fileName = FileName;

            loadFileFamily();
        }

        internal void Load()
        {
            if (loaded) return;

            var ext = Path.GetExtension(FileName);

            switch (ext)
            {
                case ".light":
                    script = Light.OpenFile(FileName);
                    _linear = script.GetLinear();
                    break;

                case ".h":
                case ".hpp":
                    script = CPP.OpenFile(FileName);
                    _linear = script.GetLinear();
                    break;

                default:
                    throw new Exception("Todo");
            }

            if (_linear == null)
                throw new Exception("Dunno, Linear not loaded");

            ReadLinear();

            loaded = true;
        }

        #region FileName

        internal string _fileName;
        public string FileName
        {
            get { return _fileName; }
        }

        internal void loadFileFamily()
        {
            string myDir = FileName;
            if (myDir == null) return;

            var ft = Resources.GetFileType(myDir);

            if (ft == Resources.FileType.DontExists)
                throw new Exception("What?");

            if (ft == Resources.FileType.File)
            {
                myDir = myDir.Substring(0, myDir.Length - Path.GetExtension(myDir).Length);
                if (!Directory.Exists(myDir)) return;
            }

            var dirInfo = new DirectoryInfo(myDir);

            var files = dirInfo.GetFiles();
            foreach (var file in files)
            {
                if (file.Extension == Properties.LightExtension)
                {
                    string name = Path.GetFileNameWithoutExtension(file.Name);
                    var comp = new File(this, name, null, file.FullName);
                }
            }

            var dirs = dirInfo.GetDirectories();
            foreach (var dir in dirs)
            {
                var comp = new File(this, dir.Name, null, dir.FullName);
            }
        }

        #endregion

        #region Types

        List<Type> usedTypes = new List<Type>();
        public void UsedType(Type type)
        {
            if (type == null)
                return;

            if (!usedTypes.Contains(type))
                usedTypes.Add(type);
        }

        #endregion

        #region Properties

        public override File TopFile
        {
            get
            {
                if (String.IsNullOrEmpty(FileName))
                    return parent.TopFile;
                else
                    return this;
            }
        }

        #endregion

        #region Writer 

        int writtenLines = 1;
        override internal int writeNewLine(Linear instr, bool isBase = true)
        {
            if(isBase) writeToCont("\n");
            // todo: handle linears line number where from different files
            if(instr != null) instr.DebugLine = writtenLines++;
            return writtenLines;
        }

        public override string WriteTo(Lang Lang)
        {
            Load();

            var ts = Lang.GetTypesSet;

            /// Check default libraries
            foreach (var libName in includeDefaults)
                writeToIncludeDefault(Lang, libName);            

            /// Check requested libraries by used types
            foreach(var type in usedTypes)
            {
                var convType = ts.Convert(type);

                if (!String.IsNullOrEmpty(convType.lib))
                    writeToIncludeDefault(Lang, convType.lib);

                if (!String.IsNullOrEmpty(convType.ns))
                    writeToUsingNamespace(convType.ns);
            }

            /// Begin to write
            base.WriteTo(Lang, true);
            return writeReturn();
        }

        void writeToIncludeDefault(Lang Lang, string libName)
        {
            writeToCont("#include");
            writeToCont(" ");
            writeToCont("<");
            writeToCont(libName);
            writeToCont(">");
            writeNewLine(null);
        }

        void writeToUsingNamespace(string Namespace)
        {
            writeToCont("using namespace ");
            writeToCont(Namespace);
            writeToCont(";");
            writeNewLine(null);
        }

        public string WriteLibrary(Lang Lang)
        {
            if (outFileName == null)
            {
                if (script.Language == Language.Light)
                {
                    /// outFileName
                    outFileName = Misc.DirectoryNameToFile(FileName) + ".hpp";
                    if(Lang.InstanceSettings.Project != null)
                    {
                        // to improve, for avoiding same project name conflicts
                        if (Lang.InstanceSettings.Project != GetProject)
                            outFileName = GetProject.name + "_" + outFileName; // Set project perspective
                    }
                    outFileName = GetProject.tempDir + outFileName;
                    outFileName = Path.GetFullPath(outFileName);

                    // comparing disabled, for the moment, because it doesn't ensure a change of perspective
                    if (true || Resources.FilesWriteTimeCompare(FileName, outFileName) >= 0) 
                    {
                        var output = WriteTo(Lang);
                        System.IO.File.WriteAllText(outFileName, output);
                    }
                }
                else
                    outFileName = FileName;
            }

            // non so se l'AssertPath metterlo qui o direttamente in WriteCPP
            return GetProject.env.Path(outFileName);
        }


        List<string> includedLibraries = new List<string>();
        public string Include(Lang lang, Component comp)
        {
            var ts = lang.GetTypesSet;

            var ofn = GetProject.Include(lang, comp); // retrieve path from Project
            if (!includedLibraries.Contains(ofn)) // if not yet included
            {
                writeToCont("#include");
                writeToCont(" ");
                writeToCont("\"");
                writeToCont(ts.StringFormat(ofn)); //handle formattation
                writeToCont("\"");
                writeNewLine(null);

                includedLibraries.Add(ofn);
            }

            return ofn;
        }

        List<string> includeDefaults = new List<string>();
        public void IncludeDefault(string libName)
        {
            includeDefaults.Add(libName);
        }

        #endregion

    }
}
