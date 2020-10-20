using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FractalMachine.Classes;

namespace FractalMachine.Code
{
    /// <summary>
    /// Used for compiling and source code preparation
    /// </summary>
    public class Machine
    {
        Component main;
        Langs.Lang lastScript;
        Linear lastLinear;

        internal string assetsDir;
        internal string libsDir;

        public Machine()
        {
            assetsDir = Resources.Solve("Assets");
            libsDir = assetsDir + "/libs";
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
            //todo: as
            var ext = Path.GetExtension(FileName);

            switch (ext)
            {
                case ".light":
                    lastScript = Langs.Light.OpenFile(FileName);
                    lastLinear = lastScript.GetLinear();
                    break;

                case ".h":
                    lastScript = Langs.CPP.OpenFile(FileName);
                    lastLinear = lastScript.GetLinear();
                    break;

            }

            var comp = new Component(this, lastLinear);
            return comp;
        }
    }
}
