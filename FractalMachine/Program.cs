using System;
using FractalMachine.Code;
using FractalMachine.Code.Langs;
using FractalMachine.Ambiance;

namespace FractalMachine
{
    class Program
    {
        static void Main(string[] args)
        {
            Properties.Init();

            Console.WriteLine("Hello fractal!");

            Context machine = new Context();

            var assetsDir = Resources.Solve("Assets");
            Light light = null;

            var test = "project";
            switch (test)
            {
                case "short":
                    light = new Light();
                    light.Parse("IO.test(\"ciao\")");
                    break;

                case "test":               
                    machine.ExtractComponent(assetsDir + "/test.light");
                    break;

                case "machine":
                    machine.ExtractComponent(assetsDir + "/test.light");
                    break;

                case "bashTest":
                    var env = Ambiance.Environment.GetEnvironment;
                    var cmd = env.NewCommand("which gcc");
                    cmd.Run();

                    var res = env.Repository.Install("git");

                    var gcc = new GCC(env);
                    //gcc.Compile("test.c");

                    string read = "";

                    break;

                case "project":
                    var proj = new Project(assetsDir+"/machine.light");
                    proj.Compile();

                    read = "";

                    break;
            }

            

            Debug.Print("leggi qui");
        }
    }
}
