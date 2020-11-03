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

using FractalMachine.Classes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static FractalMachine.Code.AST;

namespace FractalMachine.Code
{
    public class Linear
    {
        internal Component component;
        internal Linear parent;
        internal AST ast;

        public List<Linear> Instructions = new List<Linear>();
        public Dictionary<string, Linear> Settings = new Dictionary<string, Linear>();
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();

        public List<string> Attributes = new List<string>();
        public string Op;
        public string Type;
        public string Name;
        public string Return;

        public bool Continuous = false;

        internal int DebugLine = -1;

        public Linear(AST ast)
        {
            this.ast = ast;
        }

        public Linear(Linear Parent, AST orderedAst) : this(orderedAst)
        {
            parent = Parent;
            //parent.Instructions.Add(this);
        }

        public Linear SetSettings(string Name, AST ast)
        {
            var lin = new Linear(ast);
            lin.parent = this;
            lin.Op = Name;
            Settings[Name] = lin;
            return lin;
        }

        /*public Linear Clone(AST ast)
        {
            Linear lin = new Linear(ast);
            lin.parent = parent;
            lin.Op = Op;
            lin.Name = Name;
            lin.Return = Return;
            lin.Attributes = Attributes.Clone();

            // Add automatically to parent
            parent.Instructions.Add(lin);

            return lin;
        }*/

        #region Parent

        bool listed = false;
        public void List()
        {
            if (!listed)
                parent.Instructions.Add(this);
            listed = true;
        }

        #endregion

        public Linear LastInstruction
        {
            get
            {
                var c = Instructions.Count;
                if (c > 0)
                    return Instructions[c - 1];
                return null;
            }
        }

        public Linear this[int index]
        {
            get
            {
                return Instructions[index];
            }
        }
    }
}
