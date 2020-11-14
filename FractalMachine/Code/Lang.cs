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

namespace FractalMachine.Code
{
    /// <summary>
    /// This abstract class makes a language Linear compatible
    /// </summary>
    public abstract class Lang
    {
        public abstract Linear GetLinear();
        public abstract Language Language { get; }
        public abstract TypesSet GetTypesSet { get; }
        public abstract Settings GetSettings { get; }

        public abstract class Settings
        {
            public abstract string EntryPointFunction { get; }
            public abstract string OpenBlock { get; }
            public abstract string CloseBlock { get; }
            public abstract string StructureImport { get; }

            public abstract string VarsDelimiter(Component c1, Component c2);

        }

        #region InstanceSettings

        public StructInstanceSettings InstanceSettings;
        public struct StructInstanceSettings
        {
            public Project Project;
        }

        #endregion
    }

    public enum Language
    {
        Light,
        CPP
    }
}
