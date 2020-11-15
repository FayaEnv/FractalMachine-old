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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FractalMachine.Ambiance.Compilers
{
    public class GCC : Compiler
    {
        public GCC(Environment Env):base(Env)
        {
        }

        public override void Compile(string FileName, string Out)
        {
            FileName = Path.GetFullPath(FileName);
            FileName = env.Path(FileName);
            Out = env.Path(Out);

            var cmd = env.NewCommand("g++");
            //cmd.DirectCall = true;
            cmd.UseStdWrapper = true;
            cmd.AddArgument(FileName);
            cmd.AddArgument("-o", Out);

            // Std libs
            cmd.AddArgument("-std=c++11");

            cmd.Run();

            string read = "";
        }
    }
}
