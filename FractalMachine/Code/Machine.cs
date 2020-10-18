using System;
using System.Collections.Generic;
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
        string entryPoint;
        Component main;
        Langs.Light light;
        Linear linear;
        
        public Machine()
        {
         
        }

        public string EntryPoint
        {
            get
            {
                return entryPoint;
            }

            set
            {
                entryPoint = value;
            }
        }

        public void Compile()
        {
            light = Langs.Light.OpenScript(entryPoint);
            linear = light.GetLinear();

            main = new Component(this);
            main.ReadLinear(linear);
        }

    }
}
