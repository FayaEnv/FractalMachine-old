using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FractalMachine.Ambiance
{
    public class GCC
    {
        Environment env;

        public GCC(Environment Env)
        {
            env = Env;
        }

        public void Compile(string FileName, string Out)
        {
            FileName = Path.GetFullPath(FileName);
            FileName = env.AssertPath(FileName);
            Out = env.AssertPath(Out);

            var cmd = env.NewCommand("gcc");
            cmd.UseStdWrapper = false;
            cmd.AddArgument(FileName);
            cmd.AddArgument("-o", Out);

            cmd.Run();

            string read = "";
        }
    }
}
