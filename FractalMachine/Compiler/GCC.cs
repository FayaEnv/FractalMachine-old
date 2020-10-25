using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Compiler
{
    public class GCC
    {
        Environment env;

        public GCC(Environment Env)
        {
            env = Env;
        }

        public void Compile(string FileName)
        {
            var cmd = env.NewCommand("gcc");
            string re = "ea";
        }
    }
}
