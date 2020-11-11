/*
   Copyright 2020 (c) Riccardo Cecchini
   
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

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

            var assetsDir = Resources.Solve("Assets");
            Light light = null;

            var test = "toAchieve";
            switch (test)
            {
                case "short":
                    light = new Light();
                    light.Parse("IO.test(\"ciao\")");
                    break;

                case "test":
                    var proj = new Project(assetsDir + "/test.light");
                    proj.Compile();
                    break;

                case "machine":
                    proj = new Project(assetsDir + "/test.light");
                    proj.Compile();
                    break;

                case "bashTest":
                    var env = Ambiance.Environment.GetEnvironment;
                    var cmd = env.NewCommand("which gcc");
                    cmd.Run();

                    /*var res = env.Repository.Install("git");

                    var gcc = new Ambiance.Compilers.GCC(env);
                    //gcc.Compile("test.c");

                    */

                    string read = "";

                    break;

                case "project":
                    proj = new Project(assetsDir+"/proj_example");
                    proj.Compile();

                    read = "";
                    break;

                case "linearAssert":
                    var assert = new Develop.LinearAssert();
                    assert.Execute();

                    break;

                case "toAchieve":
                    var toAchieve = new Develop.ToAchieve();
                    toAchieve.Achieve();
                    break;
            }

            

            Debug.Print("leggi qui");
        }
    }
}
