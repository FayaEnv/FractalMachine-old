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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FractalMachine.Ambiance;
using FractalMachine.Classes;
using FractalMachine.Code.Langs;

namespace FractalMachine.Code
{
    /// <summary>
    /// Used for compiling and source code preparation
    /// </summary>
    public class Context
    {
        internal Ambiance.Environment env;
        internal string assetsDir, libsDir, tempDir;
        internal Dictionary<string, Component> imports = new Dictionary<string, Component>();

        public Context()
        {
            assetsDir = Resources.Solve("Assets");
            libsDir = assetsDir + "/libs";
            tempDir = Properties.TempDir;
            env = Ambiance.Environment.GetEnvironment;
        }

        internal Component ExtractComponent(string FileName)
        {
            Component comp;

            FileName = Path.GetFullPath(FileName); 

            if (imports.TryGetValue(FileName, out comp))
                return comp;

            //todo: as
            Lang script = null;
            Linear linear = null;

            Language lang;
            var ext = Path.GetExtension(FileName);

            switch (ext)
            {
                case ".light":
                    script = Light.OpenFile(FileName);
                    linear = script.GetLinear();
                    lang = Language.Light;
                    break;

                case ".h":
                case ".hpp":
                    script = CPP.OpenFile(FileName);
                    linear = script.GetLinear();
                    lang = Language.CPP;
                    break;

                default:
                    throw new Exception("Todo");
            }

            if (linear != null) // why linear should be null?
            {
                comp = new Component(this, linear);
                comp.lang = lang;
                comp.Type = Component.Types.File;
                comp.script = script;
                comp.FileName = FileName;

                comp.ReadLinear();

                imports.Add(FileName, comp);

                return comp;
            }

            return null;
        }
    }
}
