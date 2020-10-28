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

        public Project(string ProjectPath) 
        {
            var env = Ambiance.Environment.GetEnvironment;

            FileAttributes attr;
            try { attr = File.GetAttributes(ProjectPath); }
            catch { throw new Exception("Path " + ProjectPath + " not found"); }

            if(attr == FileAttributes.Directory)
            {
                if (!ProjectPath.EndsWith(env.PathChar))
                    ProjectPath += env.PathChar;
                
                directory = ProjectPath;
                entryPoint = ProjectPath + "Main.light";
                outName = ProjectPath + Path.GetFileName(ProjectPath) + env.exeFormat;

                if (!File.Exists(entryPoint))
                    throw new Exception("Unable to open project directory " + ProjectPath);
            }
            else
            {
                entryPoint = ProjectPath;
                directory = Path.GetDirectoryName(ProjectPath);
                outName = Path.GetFileNameWithoutExtension(entryPoint);
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
