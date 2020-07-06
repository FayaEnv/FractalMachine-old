using System;

namespace FractalMachine
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello fractal!");

            var assetsDir = Resources.Solve("Assets");
            Light light;

            var test = "test.light";
            switch (test)
            {
                case "short":
                    light = new Light();
                    light.Parse("IO.test(\"ciao\")");
                    break;

                case "test.light":
                    
                    light = Light.OpenScript(assetsDir + "/test.light");
                    break;
            }

            Debug.Print("leggi qui");
        }
    }
}
