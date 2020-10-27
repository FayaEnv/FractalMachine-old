using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Ambiance.Compilers
{
    public class MSVC : Compiler
    {
        public MSVC(Environment Env) : base(Env)
        {
        }

        public override void Compile(string FileName, string Out)
        {
            FileName = System.IO.Path.GetFullPath(FileName);
            FileName = env.Path(FileName);
            Out = env.Path(Out);

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
