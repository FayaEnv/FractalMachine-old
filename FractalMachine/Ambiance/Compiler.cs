using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Ambiance
{
    public abstract class Compiler
    {
        public abstract void Compile(string FileName, string Out);
    }
}
