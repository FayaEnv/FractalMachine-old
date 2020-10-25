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
            Properties.Init();

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
                    var env = Compiler.Environment.GetEnvironment;
                    var cmd = env.ExecuteCommand("which gcc");
                    cmd.Run();

                    var gcc = new GCC(env);
                    //gcc.Compile("test.c");

                    string read = "";

                    break;
            }

            

            Debug.Print("leggi qui");
        }
    }
}
