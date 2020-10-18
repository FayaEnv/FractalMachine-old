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
        
        public Machine()
        {
         
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
            bool angularBrackets = false;
            if (ToImport.HasStringMark() || (angularBrackets = ToImport.HasAngularBracketMark()))
            {
                // Is file
            }
            else
            {
                // Is namespace
            }

        }

        string findNamespaceDirectory(string ns)
        {

            return "";
        }

    }
}
