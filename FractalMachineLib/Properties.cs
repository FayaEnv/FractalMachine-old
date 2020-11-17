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
using System.Diagnostics;

namespace FractalMachineLib
{
    public static class Properties
    {
        public static bool Debugging = Debugger.IsAttached;

        public static string TempDir = "temp/";
        public static string CygwinDownloadUrl = "https://srv-store2.gofile.io/download/iITx0L/cygwin64-light.zip";
        public static int MaxRepositoryDays = 7;

        public static string LightExtension = ".light";
        public static string EntryPointFunction = "Main";
        public static string ProjectMainFile = EntryPointFunction + LightExtension;
        
        public static string[] FileImportExtensions = new string[] { ".h", LightExtension };
        public static string InternalVariable = "£";

        public static string NativeFunctionPrefix = "___";

        // Marks should have just 2 chars
        public static string Mark = "*°";
        public static string StringMark = "$$" + Mark;
        public static string AngularBracketsMark = "<>" + Mark;
        public static string ReferenceMark = "rf" + Mark;

        /// <summary>
        /// Init things that depends on one of these properties
        /// </summary>
        public static void Init()
        {
            Resources.CreateDirIfNotExists(TempDir);
        }
    }
}
