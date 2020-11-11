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
using System.Linq;

// think about it https://en.wikipedia.org/wiki/IEC_61131-3
namespace FractalMachine.Code
{
    public abstract class TypesSet
    {
        public Dictionary<string, Type> Types = new Dictionary<string, Type>();

        public TypesSet()
        {
            init();
        }

        internal abstract void init();
        public abstract AttributeType GetAttributeType(string Name);
        public abstract string SolveAttributeType(AttributeType AttributeType);

        internal Type AddType(string Name)
        {
            var t = new Type(this);
            Types.Add(Name, t);
            t.Name = Name;
            t.LightType = Name;
            return t;
        }

        public Type Get(string name)
        {
            if (name.Contains("["))
                throw new Exception("todo"); //handle array types

            return Types[name]; //todo better
        }
      
        public AttributeType Convert(AttributeType attribute, bool recycle = true)
        {
            if (attribute.Type == AttributeType.Types.Name)
                return attribute;

            var from = attribute.TypesSet;

            if (from == this)
                return attribute;

            var fromType = from.Types[attribute.TypeRef];
            var toType = Convert(fromType);

            AttributeType toAttr = attribute;

            if (!recycle)
            {
                toAttr = new AttributeType(this);
                toAttr.AbsValue = attribute.AbsValue;
                toAttr.Type = AttributeType.Types.Type;
            }

            toAttr.TypeRef = toType.Name;

            return toAttr;
        }

        public Type Convert(Type from)
        {
            // Lazy method
            var ltype = from.LightType;
            var search = Types.Where(s => s.Value.LightType == ltype);

            if(ltype.Count() == 0)
            {
                // deep search
                throw new Exception("todo");
            }
            else 
                return Types.Where(s => s.Value.LightType == ltype).First().Value;
        }

        public virtual string GetTypeCodeName(Type type)
        {
            if (type.TypesSet != this)
                type = Convert(type);

            return type.Name;
        }

        public Type CompareTypeCast(Type t1, Type t2)
        {
            //todo: improve this, this is a temporary solution
            Type ideal;

            ideal = t1.Bytes > t2.Bytes ? t1 : t2;
            
            if(t1.Floating != t2.Floating)
                ideal = t1.Floating ? t1 : t2;

            if (t1.LightType == "string" || t2.LightType == "string")
                ideal = t1.LightType == "string" ? t1 : t2;

            return ideal;
        }
    }

    public class AttributeType
    {
        public TypesSet TypesSet;
        public Types Type;
        public string TypeRef, AbsValue;

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

        public Type GetLangType
        {
            get
            {
                if (Type != Types.Name) return null;
                return TypesSet.Get(TypeRef);
            }
        }
    }

    public class Type
    {
        public TypesSet TypesSet;

        public string LightType;
        public Type Base;
        public string Name;
        public int Bytes;
        public bool Floating = false;
        public bool Signed = false;
        public bool Array = false;
        public bool Class = false;

        public Type(TypesSet TypesSet)
        {
            this.TypesSet = TypesSet;
            Name = "var"; // means generic type
        }

        public Type(TypesSet TypesSet, string Name, int Bytes, bool Floating, bool Signed) : this(TypesSet)
        {
            this.Name = Name;
            this.Bytes = Bytes;
            this.Floating = Floating;
            this.Signed = Signed;
        }

        public Type(TypesSet TypesSet, Type Base) : this(TypesSet)
        {
            this.Base = Base;
            this.Name = "";
        }

        public Type(TypesSet TypesSet, string Name) : this(TypesSet)
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

        #region Properties

        public string AttributeReference
        {
            get
            {
                if (Base != null)
                    return Base.AttributeReference;

                return Name;
            }

        }

        public Type GetLightType
        {
            get
            {
                return Langs.Light.StaticGetTypesSet.Get(LightType);
            }
        }

        #endregion
    }
}
