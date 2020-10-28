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

namespace FractalMachine.Ambiance.Repositories
{
    /// <summary>
    /// FractalRepo will be the only repository not linked to an Environment but to the Compiler
    /// </summary>
    public class FractalRepo : Repository
    {
        public FractalRepo(Environment Env) : base(Env)
        {
        }

        public override void Search(string query)
        {
            //todo
        }

        public override void Info(string query)
        {
            //todo
        }

        public override InstallationResult Install(string Package, bool Depedency = false)
        {
            //todo
            return InstallationResult.PackageNotFound;
        }

        public override void List(string query)
        {
            //todo
        }

        public override void Upgrade(string query)
        {
            //todo
        }

        public override void Update()
        {
            //todo
        }
    }
}
