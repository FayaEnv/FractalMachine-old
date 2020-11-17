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

using FractalMachineLib.Classes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static FractalMachineLib.Code.AST;

namespace FractalMachineLib.Code
{
    public class Linear
    {
        internal Lang Lang;
        internal Component component;
        internal Linear parent;
        internal AST ast;

        public List<Linear> Instructions = new List<Linear>();
        public Dictionary<string, Linear> Settings = new Dictionary<string, Linear>();
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();

        public int Pos;

        public List<string> Attributes = new List<string>();
        public string Op;
        public string Type;
        public string Name;
        public string Return;

        public bool Continuous = false;

        /// Compiler fields
        internal int DebugLine = -1;

        public Linear(Lang lang, AST ast)
        {
            this.ast = ast;
            this.Lang = lang;
        }

        public Linear(Linear Parent, AST orderedAst) : this(Parent.Lang, orderedAst)
        {
            parent = Parent;
            //parent.Instructions.Add(this);
        }

        public Linear SetSettings(string Name, AST ast)
        {
            var lin = new Linear(Lang, ast);
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

        #region Properties

        public bool HasOperator
        {
            get
            {
                return Type == "oprt";
            }
        }

        public bool IsOperation
        {
            get
            {
                return HasOperator || Op == "call";
            }
        }

        public bool IsCall
        {
            get
            {
                return Op == "call";
            }
        }

        public bool IsCast
        {
            get
            {
                return Op == "cast";
            }
        }

        public bool IsProperty
        {
            get
            {
                return Op == "declare" && Type == "property";
            }
        }

        #endregion

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
