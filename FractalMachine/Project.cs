/*
   Copyright 2020 (c) Riccardo Cecchini
   
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using FractalMachine.Classes;
using FractalMachine.Code;
using FractalMachine.Ambiance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FractalMachine.Code.Langs;
using System.Linq;

namespace FractalMachine
{
    public class Project : Code.Components.File
    {
        internal Ambiance.Environment env;

        string outName;
        string entryPoint;
        string directory;

        internal string exeOutPath;

        /// Context
        internal Code.Components.File mainComp;
        internal string assetsDir, libsDir, tempDir;
        internal Dictionary<string, Component> imports = new Dictionary<string, Component>();
        internal Dictionary<string, Component> importLink = new Dictionary<string, Component>();

        public Project(string ProjectPath) : base(null, null, ProjectPath)
        { 
            env = Ambiance.Environment.GetEnvironment;
            containerType = ContainerTypes.Project;

            var ft = Resources.GetFileType(ProjectPath);
            if (ft == Resources.FileType.Directory)
            {
                if (!ProjectPath.EndsWith(env.PathChar))
                    ProjectPath += env.PathChar;
                
                directory = ProjectPath;
                entryPoint = ProjectPath +'/'+ Properties.ProjectMainFile;

                var spl = ProjectPath.Replace('\\','/').Split('/');
                outName = spl[spl.Length-2];

                mainComp = (Code.Components.File) Solve("Main", false);
                if (mainComp == null)
                    throw new Exception("Unable to open project directory " + ProjectPath+": Main entry point is missing");
            }
            else
            {
                entryPoint = ProjectPath;
                _fileName = directory = Path.GetDirectoryName(ProjectPath); //tocheck: ends with /?
                outName = Path.GetFileNameWithoutExtension(entryPoint);
                mainComp = this;
            }

            /// Assets and dirs
            assetsDir = Resources.Solve("Assets");
            libsDir = assetsDir + "/libs";
            tempDir = Properties.TempDir;
        }

        public void Compile(string Out = null)
        {
            // Name
            var outPath = directory + (Out==null ? outName : Out);
            exeOutPath = outPath + env.exeFormat;

            var cpp = new CPP();

            var cppOutPath = Properties.TempDir + Misc.DirectoryNameToFile(entryPoint) + ".cpp";
            if (Resources.FilesWriteTimeCompare(entryPoint, cppOutPath) >= 0)
            {
                //var comp = ExtractComponent(entryPoint);
                mainComp.Load();
                var output = mainComp.WriteTo(cpp);
                File.WriteAllText(cppOutPath, output);
            }

            // Compile
            // Pay attention to the case of an updated library but not the entry point
            if (Resources.FilesWriteTimeCompare(cppOutPath, exeOutPath) >= 0)
            {
                var compiler = env.Compiler;
                compiler.Compile(cppOutPath, exeOutPath);
            }
        }

    }
}
