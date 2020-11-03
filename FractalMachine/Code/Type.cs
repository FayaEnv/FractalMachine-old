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
using System.ComponentModel.Design.Serialization;

// think about it https://en.wikipedia.org/wiki/IEC_61131-3
namespace FractalMachine.Code
{
    public abstract class TypesSet
    {
        public Dictionary<string, Type> Types;

        public TypesSet()
        {
            init();
        }

        internal abstract void init();
        public abstract AttributeType SolveAttribute(string Name);
        public abstract string ConvertAttributeTo(string Attribute, Type To, AttributeType AttributeType=null);

        internal Type AddType(string Name)
        {
            var t = new Type();
            Types.Add(Name, t);
            t.Name = Name;
            return t;
        }

        public Type Get(string name)
        {
            return Types[name]; //todo better
        }
      
    }

    public class AttributeType
    {
        public TypesSet TypesSet;
        public Types Type;
        public string TypeRef;

        public AttributeType(TypesSet typesSet)
        {
            TypesSet = typesSet;
        }

        public enum Types
        {
            Type,
            Name,
            Invalid
        }
    }

    public class Type
    { 
        public Type Base;
        public string Name;
        public int Bytes;
        public bool Floating = false;
        public bool Signed = false;
        public bool Array = false;
        public bool Class = false;

        public Type()
        {
            Name = "var"; // means generic type
        }

        public Type(string Name, int Bytes, bool Floating, bool Signed)
        {
            this.Name = Name;
            this.Bytes = Bytes;
            this.Floating = Floating;
            this.Signed = Signed;
        }

        public Type(Type Base)
        {
            this.Base = Base;
        }

        public Type(string Name)
        {
            this.Name = Name;
            Class = true;
        }

        public void Solve(Component comp)
        {
            if (Class)
            {
                //todo
                throw new Exception("todo");
            }
        }

        public string AttributeReference
        {
            get
            {
                if (Base != null)
                    return Base.AttributeReference;

                return Name;
            }

        }
    }
}
