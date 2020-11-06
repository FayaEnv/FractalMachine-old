using FractalMachine.Classes;
using FractalMachine.Code.Langs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class File : Container
    {
        internal bool loaded = false;
        internal string outFileName;
        internal Component parent;
        internal Lang script;

        List<string> usings;

        public File(Component Parent, Linear Linear, string FileName) : base(Parent, Linear)
        {
            usings.Add("namespace std");
            containerType = ContainerTypes.File;
            _fileName = FileName;

            loadFileFamily();
        }

        public Project Project
        {
            get 
            { 
                //todo: find project
                return (Project)parent;         
            }
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

        #region ReadLinear

        internal override void readLinear_import(Linear instr)
        {
            Import(instr.Name, instr.Parameters);
        }

        #endregion

        #region Types

        public void CheckType(string subject, string request, int linearPos)
        {
            var types = script.GetTypesSet;
            Type reqType = types.Get(request);
            Type subjType;

            var attrType = types.SolveAttribute(subject);

            if (attrType.Type == Code.AttributeType.Types.Invalid)
            {
                throw new Exception("Invalid type");
            }

            if (attrType.Type == Code.AttributeType.Types.Name)
            {
                // get component info    
                var comp = Solve(subject);
                subjType = types.Get(comp.Linear.Return);
                subjType.Solve(this); // or comp?

                if (subjType.Name != reqType.Name)
                {
                    //todo
                    throw new Exception("todo");
                }
            }
            else
            {
                if (attrType.TypeRef != reqType.AttributeReference)
                {
                    subject = types.ConvertAttributeTo(subject, reqType, attrType);
                    Linear[linearPos].Name = subject;
                }
            }

            string done = "";
        }

        #endregion

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
                    var comp = new File(this, null, file.FullName);
                    addComponent(name, comp);
                }
            }

            var dirs = dirInfo.GetDirectories();
            foreach (var dir in dirs)
            {
                var comp = new File(this, null, dir.FullName);
                addComponent(dir.Name, comp);
            }
        }

        #endregion

        #region Properties


        #endregion

        #region Writer 
        int newLines = 1;
        override internal int writeToNewLine()
        {
            return newLines++;
        }

        public string WriteLibrary(Lang.Settings LangSettings)
        {
            if (outFileName == null)
            {
                if (script.Language == Language.Light)
                {
                    outFileName = Project.tempDir + Misc.DirectoryNameToFile(FileName) + ".hpp";
                    outFileName = Path.GetFullPath(outFileName);
                    if (Resources.FilesWriteTimeCompare(FileName, outFileName) >= 0)
                    {
                        var output = WriteTo(LangSettings);
                        System.IO.File.WriteAllText(outFileName, output);
                    }
                }
                else
                    outFileName = FileName;
            }

            // non so se l'AssertPath metterlo qui o direttamente in WriteCPP
            return Project.env.Path(outFileName);
        }

        #endregion

    }
}
