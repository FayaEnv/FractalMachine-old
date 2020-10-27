using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FractalMachine.Ambiance.Compilers
{
    public class GCC : Compiler
    {
        Environment env;

        public GCC(Environment Env)
        {
            env = Env;
        }

        public override void Compile(string FileName, string Out)
        {
            FileName = Path.GetFullPath(FileName);
            FileName = env.AssertPath(FileName);
            Out = env.AssertPath(Out);

            var cmd = env.NewCommand("g++");
            //cmd.DirectCall = true;
            cmd.UseStdWrapper = true;
            cmd.AddArgument(FileName);
            cmd.AddArgument("-o", Out);

            // Std libs
            cmd.AddArgument("-std=c++11");

            cmd.Run();

            string read = "";
        }
    }
}
