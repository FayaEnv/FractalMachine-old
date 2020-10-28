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
using System.Text;

namespace FractalMachine.Ambiance.Environments.Windows
{
    public class Windows : Environment
    {
        public Windows()
        {
            compiler = new Compilers.MSVC(this);
            shell = "cmd";
            exeFormat = ".exe";

            checkMSYS2();

            if (SelectSubsystem("msys2") == null)
                throw new Exception("In this moment Windows environmnet is not supported without MSYS2");
        }

        public override char PathChar
        {
            get { return '\\'; }
        }

        #region Subsystems

        public void checkMSYS2()
        {         
            var path = Resources.SearchFile("msys2.exe", 3, Environment.ExecutionDirectory);

            if(String.IsNullOrEmpty(path)) 
                path = Resources.SearchFile("msys2.exe", 3);

            if (!String.IsNullOrEmpty(path))
            {
                var subsys = new MSYS2(this, System.IO.Path.GetDirectoryName(path)+"/");
                subsystems.Add("msys2", subsys);
            }
            else
            {
                //todo: do you want install it?
            }
        }

        // repo https://github.com/microsoft/vcpkg

        #endregion

    }
}
