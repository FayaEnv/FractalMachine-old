using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Ambiance
{
    public abstract class Compiler
    {
        internal Environment env;

        public Compiler(Environment Environment)
        {
            env = Environment;
        }

        public abstract void Compile(string FileName, string Out);
    }
}
