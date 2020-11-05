using FractalMachine.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class File : Container
    {
        internal string FileName, outFileName;
        internal Component parent;
        internal Lang script;

        List<string> usings;

        public File(Project Project, Linear Linear) : base(Project, Linear)
        {
            usings.Add("namespace std");
            containerType = ContainerTypes.File;
        }

        public Project Project
        {
            get { return (Project)parent; }
        }

        #region ReadLinear



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
