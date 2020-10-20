using System;
using FractalMachine.Code;
using FractalMachine.Code.Langs;

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

            var test = "machine";
            switch (test)
            {
                case "short":
                    light = new Light();
                    light.Parse("IO.test(\"ciao\")");
                    break;

                case "test":               
                    machine.EntryPoint = assetsDir + "/test.light";
                    break;

                case "machine":
                    machine.EntryPoint = assetsDir + "/machine.light";
                    break;
            }

            machine.Compile();

            Debug.Print("leggi qui");
        }
    }
}
