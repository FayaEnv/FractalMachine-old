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

namespace FractalMachine
{
    public class Project : Component
    {
        internal Ambiance.Environment env;

        string outName;
        string entryPoint;
        string directory;

        internal string exeOutPath;

        /// Context
        internal string assetsDir, libsDir, tempDir;
        internal Dictionary<string, Component> imports = new Dictionary<string, Component>();
        internal Dictionary<string, Component> importLink = new Dictionary<string, Component>();

        public Project(string ProjectPath) : base(null, null)
        {
            env = Ambiance.Environment.GetEnvironment;

            FileAttributes attr;
            try { attr = File.GetAttributes(ProjectPath); }
            catch { throw new Exception("Path " + ProjectPath + " not found"); }

            if(attr == FileAttributes.Directory)
            {
                if (!ProjectPath.EndsWith(env.PathChar))
                    ProjectPath += env.PathChar;
                
                directory = ProjectPath;
                entryPoint = ProjectPath + "Main.light";

                var spl = ProjectPath.Replace('\\','/').Split('/');
                outName = spl[spl.Length-2];

                if (!File.Exists(entryPoint))
                    throw new Exception("Unable to open project directory " + ProjectPath);
            }
            else
            {
                entryPoint = ProjectPath;
                directory = Path.GetDirectoryName(ProjectPath); //tocheck: ends with /?
                outName = Path.GetFileNameWithoutExtension(entryPoint);
            }

            /// Pre calculates
            var outPath = directory + outName;
            exeOutPath = outPath + env.exeFormat;

            /// Assets and dirs
            assetsDir = Resources.Solve("Assets");
            libsDir = assetsDir + "/libs";
            tempDir = Properties.TempDir;
        }

        public void Compile(string Out = null)
        {
            string name;
            if(Out == null)
            {
                name = Path.GetFileNameWithoutExtension(entryPoint);
                if (name == "main") name = Path.GetDirectoryName(entryPoint);
            }

            var cpp = new CPP();

            var cppOutPath = Properties.TempDir + Misc.DirectoryNameToFile(entryPoint) + ".cpp";
            if (Resources.FilesWriteTimeCompare(entryPoint, cppOutPath) >= 0)
            {
                var comp = ExtractComponent(entryPoint);
                var output = comp.WriteTo(cpp.GetSettings);
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

        #region Context

        internal Component ExtractComponent(string FileName)
        {
            Component comp;

            FileName = Path.GetFullPath(FileName);

            if (imports.TryGetValue(FileName, out comp))
                return comp;

            //todo: as
            Lang script = null;
            Linear linear = null;

            var ext = Path.GetExtension(FileName);

            switch (ext)
            {
                case ".light":
                    script = Light.OpenFile(FileName);
                    linear = script.GetLinear();
                    break;

                case ".h":
                case ".hpp":
                    script = CPP.OpenFile(FileName);
                    linear = script.GetLinear();
                    break;

                default:
                    throw new Exception("Todo");
            }

            if (linear != null) // why linear should be null?
            {
                comp = new Code.Components.File(this, linear)
                {
                    script = script,
                    FileName = FileName
                };

                comp.ReadLinear();

                imports.Add(FileName, comp);

                return comp;
            }

            return null;
        }

        #endregion

        #region Import

        public void Import(string ToImport, Dictionary<string, string> Parameters)
        {
            if (ToImport.HasMark())
            {
                // Is file
                //todo: ToImport.HasStringMark() || (angularBrackets = ToImport.HasAngularBracketMark())
                var fname = ToImport.NoMark();
                var dir = libsDir + "/" + fname;
                var c = importFileIntoComponent(dir, Parameters);
                importLink.Add(fname, c);
                //todo: importLink.Add(ResultingNamespace, dir);
            }
            else
            {
                // Is namespace
                var fname = findNamespaceDirectory(ToImport);
                var dir = libsDir + fname;

                if (Directory.Exists(dir))
                {
                    var c = importDirectoryIntoComponent(dir);
                    importLink.Add(ToImport, c);
                }

                dir += ".light";
                if (File.Exists(dir))
                {
                    var c = importFileIntoComponent(dir, Parameters);
                    importLink.Add(ToImport, c);
                }
            }
        }

        internal Component importFileIntoComponent(string file, Dictionary<string, string> parameters)
        {
            var comp = ExtractComponent(file);
            //comp.parent = this; // ???

            foreach (var c in comp.components)
            {
                //todo: file name yet exists
                this.components.Add(c.Key, c.Value);
            }

            comp.parameters = parameters;

            return comp;
        }

        internal Component importDirectoryIntoComponent(string dir)
        {
            if (File.Exists(dir + "/" + "Main.light"))
            {
                //it's a project
                return new Project(dir);
            }

            // else, maybe, it should list all files and add as subcomponent

            return null;
        }

        string findNamespaceDirectory(string ns)
        {
            var dir = "";
            var split = ns.Split('.');

            bool dirExists = false;

            int s = 0;
            for (; s < split.Length; s++)
            {
                var ss = split[s];
                dir += "/" + ss;

                if (!(dirExists = Directory.Exists(libsDir + dir)))
                {
                    break;
                }
            }

            if (dirExists)
            {
                return dir;
            }
            else
            {
                while (!File.Exists(libsDir + dir + ".light") && s >= 0)
                {
                    dir = dir.Substring(0, dir.Length - (split[s].Length + 1));
                    s--;
                }
            }

            return dir;
        }

        #endregion
    }
}
