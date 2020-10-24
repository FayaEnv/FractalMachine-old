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
                    //bash.ExecuteCommand("echo ciao");

                    break;
            }

            

            Debug.Print("leggi qui");
        }
    }
}
