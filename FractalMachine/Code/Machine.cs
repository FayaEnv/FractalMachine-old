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
        Langs.Light lastLight;
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
        }

        internal Component Compile(string FileName)
        {
            lastLight = Langs.Light.OpenScript(FileName);
            //var oast = lastLight.GetOrderedAst();
            lastLinear = lastLight.GetLinear();

            var comp = new Component(this, lastLinear);
            comp.ReadLinear();
            return comp;
        }
    }
}
