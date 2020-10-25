using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FractalMachine.Classes;
using FractalMachine.Code.Langs;

namespace FractalMachine.Code
{
    /// <summary>
    /// Used for compiling and source code preparation
    /// </summary>
    public class Machine
    {
        Component main;
        Lang lastScript;
        Linear lastLinear;

        internal string assetsDir, libsDir, tempDir;
        internal Dictionary<string, Component> imports = new Dictionary<string, Component>();

        public Machine()
        {
            assetsDir = Resources.Solve("Assets");
            libsDir = assetsDir + "/libs";

            tempDir = Properties.TempDir;
        }

        public string EntryPoint;

        public void Compile()
        {
            main = Compile(EntryPoint);
            string output = main.WriteToCpp();
            var re = "ead";
        }

        internal Component Compile(string FileName)
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
                    lastScript = script = Langs.Light.OpenFile(FileName);
                    lastLinear = linear = lastScript.GetLinear();
                    break;

                case ".h":
                case ".hpp":
                    lastScript = script = Langs.CPP.OpenFile(FileName);
                    lastLinear = linear = lastScript.GetLinear();
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
