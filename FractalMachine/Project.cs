using FractalMachine.Classes;
using FractalMachine.Code;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FractalMachine
{
    public class Project
    {
        string entryPoint;

        public Project(string Path) 
        {
            FileAttributes attr;
            try { attr = File.GetAttributes(Path); }
            catch { throw new Exception("Path " + Path + " not found"); }

            if(attr == FileAttributes.Directory)
            {
                //todo
            }
            else
            {
                entryPoint = Path;
            }
        }

        public void Compile(string Out = null)
        {
            string name;
            if(Out == null)
            {
                name = Path.GetFileNameWithoutExtension(entryPoint);
                if (name == "main") name = Path.GetDirectoryName(entryPoint);
            }

            var context = new Context();

            var comp = context.ExtractComponent(entryPoint);
            var output = comp.WriteToCpp();
            var outPath = Properties.TempDir + Misc.DirectoryToFile(entryPoint) + ".cpp";
            File.WriteAllText(outPath, output);

            string read = "";
        }
    }
}
