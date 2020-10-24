using System;
using FractalMachine.Code;
using FractalMachine.Code.Langs;
using FractalMachine.Compiler;

namespace FractalMachine
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello fractal!");

            Machine machine = new Machine();

            var assetsDir = Resources.Solve("Assets");
            Light light = null;

            var test = "bash-test";
            switch (test)
            {
                case "short":
                    light = new Light();
                    light.Parse("IO.test(\"ciao\")");
                    break;

                case "test":               
                    machine.EntryPoint = assetsDir + "/test.light";
                    machine.Compile();
                    break;

                case "machine":
                    machine.EntryPoint = assetsDir + "/machine.light";
                    machine.Compile();
                    break;

                case "bash-test":
                    var bash = new Bash();
                    System.Threading.Thread.Sleep(1000);
                    //var s1 = bash.ExecuteCommand("echo ciao");
                    //var s2 = bash.ExecuteCommand("pacman -h");
                   
                    var gcc = new GCC(bash);
                    //gcc.Compile("test.c");

                    string read = "";

                    break;
            }

            

            Debug.Print("leggi qui");
        }
    }
}
