using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Compiler
{
    public class GCC
    {
        Bash bash;

        public GCC(Bash Bash)
        {
            bash = Bash;
        }

        public void Compile(string FileName)
        {
            //var exe = bash.NewExecution("gcc --help");
            //exe.Run();
            string re = "ea";
        }
    }
}
