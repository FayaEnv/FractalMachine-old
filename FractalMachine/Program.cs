using System;

namespace FractalMachine
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello fractal!");

            Machine machine = new Machine();
            var assetsDir = Resources.Solve("Assets");
            Light light;

            var test = "machine.light";
            switch (test)
            {
                case "short":
                    light = new Light();
                    light.Parse("IO.test(\"ciao\")");
                    break;

                case "test.light":               
                    light = Light.OpenScript(assetsDir + "/test.light");
                    break;

                case "machine.light":
                    light = Light.OpenScript(assetsDir + "/machine.light");
                    machine.Execute(light);
                    break;
            }

            Debug.Print("leggi qui");
        }
    }
}
