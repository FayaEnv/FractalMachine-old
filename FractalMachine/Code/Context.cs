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
            
            var ext = Path.GetExtension(FileName);

            switch (ext)
            {
                case ".light":
                    script = Langs.Light.OpenFile(FileName);
                    linear = script.GetLinear();
                    break;

                case ".h":
                case ".hpp":
                    script = Langs.CPP.OpenFile(FileName);
                    linear = script.GetLinear();
                    break;

            }

            if (linear != null) // why linear should be null?
            {
                comp = new Component(this, linear);
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
