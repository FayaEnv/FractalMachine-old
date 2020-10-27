using FractalMachine.Classes;
using FractalMachine.Code;
using FractalMachine.Ambiance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FractalMachine
{
    public class Project
    {
        string outName;
        string entryPoint;
        string directory;

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
                directory = System.IO.Path.GetDirectoryName(Path);
                outName = System.IO.Path.GetFileNameWithoutExtension(entryPoint);
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

            var cppOutPath = Properties.TempDir + Misc.DirectoryNameToFile(entryPoint) + ".cpp";
            if (Resources.FilesWriteTimeCompare(entryPoint, cppOutPath) >= 0)
            {
                var comp = context.ExtractComponent(entryPoint);
                var output = comp.WriteToCpp();
                File.WriteAllText(cppOutPath, output);
            }

            // Compile
            var env = Ambiance.Environment.GetEnvironment;

            var exeOutPath = directory+"/"+outName+env.exeFormat;

            // Pay attention to the case of an updated library but not the entry point
            if (Resources.FilesWriteTimeCompare(cppOutPath, exeOutPath) >= 0)
            {
                var compiler = env.Compiler;
                compiler.Compile(cppOutPath, exeOutPath);
            }


            string read = "";
        }
    }
}
