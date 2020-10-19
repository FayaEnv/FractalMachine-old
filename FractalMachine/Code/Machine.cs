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
        Langs.Light light;
        Linear linear;

        string assetsDir;
        string libsDir;

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
            light = Langs.Light.OpenScript(FileName);
            linear = light.GetLinear();

            var comp = new Component(this);
            comp.ReadLinear(linear);
            return comp;
        }

        public void Import(string ToImport)
        {
            if (ToImport.HasMark())
            {
                // Is file
                // ToImport.HasStringMark() || (angularBrackets = ToImport.HasAngularBracketMark())
            }
            else
            {
                // Is namespace
                var dir = findNamespaceDirectory(ToImport);
                var r = "ead";
            }

        }

        string findNamespaceDirectory(string ns)
        {
            var dir = "";
            var split = ns.Split('.');

            int s = 0;
            for (; s<split.Length; s++)
            {
                var ss = split[s];
                dir += "/" + ss;

                if (!Directory.Exists(libsDir+dir))
                {
                    break;
                }             
            }

            while(!File.Exists(libsDir + dir + ".light") && s >= 0)
            {
                dir = dir.Substring(0, dir.Length - (split[s].Length + 1));
                s--;
            }

            if (s >= 0)
                dir += ".light";

            return dir;
        }

    }
}
