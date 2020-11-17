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

using FractalMachineLib.Classes;
using FractalMachineLib.Code;
using FractalMachineLib.Ambiance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FractalMachineLib.Code.Langs;
using System.Linq;

namespace FractalMachineLib
{
    public class Project : Code.Components.File
    {
        internal Ambiance.Environment env;

        string entryPoint;
        string directory;

        internal string exeOutPath;

        /// Context
        internal Code.Components.File mainComp;
        internal string assetsDir, libsDir, tempDir;
        internal Dictionary<string, Component> imports = new Dictionary<string, Component>();
        internal Dictionary<string, Component> importLink = new Dictionary<string, Component>();

        public Project(string ProjectPath) : base(null, null, null, ProjectPath)
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
                name = spl[spl.Length-2];

                mainComp = (Code.Components.File) Solve("Main", false);
                if (mainComp == null)
                    throw new Exception("Unable to open project directory " + ProjectPath+": Main entry point is missing");
            }
            else
            {
                entryPoint = ProjectPath;
                _fileName = directory = Path.GetDirectoryName(ProjectPath); //tocheck: ends with /?
                name = Path.GetFileNameWithoutExtension(entryPoint);
                mainComp = this;
            }

            mainComp.isMain = true;

            /// Assets and dirs
            assetsDir = Resources.Solve("Assets");
            libsDir = assetsDir + "/libs";
            tempDir = Properties.TempDir;
        }

        public void Compile(string Out = null)
        {
            // Name
            var outPath = directory + (Out==null ? name : Out);
            exeOutPath = outPath + env.exeFormat;

            var cpp = new CPP();
            cpp.InstanceSettings.Project = this;

            var cppOutPath = Properties.TempDir + Misc.DirectoryNameToFile(entryPoint) + ".cpp";
            var output = mainComp.WriteTo(cpp);
            File.WriteAllText(cppOutPath, output);

            // Compile
            // Pay attention to the case of an updated library but not the entry point
            if (Properties.Debugging || Resources.FilesWriteTimeCompare(cppOutPath, exeOutPath) >= 0)
            {
                var compiler = env.Compiler;
                compiler.Compile(cppOutPath, exeOutPath);
            }
        }

        #region Import

        new public string Include(Lang Lang, Component Comp)
        {
            //todo: handle the case that comp comes from external project
            var cf = Comp.TopFile;

            if (String.IsNullOrEmpty(cf.outFileName))
                cf.WriteLibrary(Lang);            

            return cf.outFileName;
        }

        #endregion

        #region Properties

        public override Project GetProject
        {
            get
            {
                return this;
            }
        }

        #endregion

    }
}
