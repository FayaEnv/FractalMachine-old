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

namespace FractalMachine.Code
{
    public class Type
    {
        #region Static

        public static Dictionary<string, Type> Types = new Dictionary<string, Type>();
        public static Dictionary<AttributeType, Type> DefaultTypes = new Dictionary<AttributeType, Type>();
        static bool inited = false;

        private static Type AddType(string Name)
        {
            var t = new Type();
            Types.Add(Name, t);
            t._name = Name;
            return t;
        }

        private static void SetAsDefault(Type t, AttributeType attrType = AttributeType.Invalid)
        {
            DefaultTypes[attrType == AttributeType.Invalid ? t._attributeType : attrType] = t;
        }

        static void initTypes()
        {
            if (inited)
                return;

            /// char
            var _char = AddType("char");
            _char._bytes = 1;
            _char._signed = true;
            _char._attributeType = AttributeType.String | AttributeType.Number;

            /// uchar
            var _uchar = AddType("uchar");
            _uchar._bytes = 1;
            _uchar._attributeType = AttributeType.String | AttributeType.Number;

            /// short
            var _short = AddType("short");
            _short._bytes = 2;
            _short._signed = true;
            _short._attributeType = AttributeType.Number;

            /// ushort
            var _ushort = AddType("ushort");
            _ushort._bytes = 2;
            _ushort._attributeType = AttributeType.Number;

            /// int
            var _int = AddType("int");
            _int._bytes = 4;
            _int._signed = true;
            _int._attributeType = AttributeType.Number;
            SetAsDefault(_int);

            /// uint
            var _uint = AddType("uint");
            _uint._bytes = 4;
            _uint._attributeType = AttributeType.Number;

            /// long
            var _long = AddType("long");
            _long._bytes = 8;
            _long._signed = true;
            _long._attributeType = AttributeType.Number;

            /// ulong
            var _ulong = AddType("ulong");
            _ulong._bytes = 8;
            _ulong._attributeType = AttributeType.Number;

            /// float
            var _float = AddType("float");
            _float._bytes = 4;
            _float._floating = true;
            _float._attributeType = AttributeType.Float;
            SetAsDefault(_float);

            /// double
            var _double = AddType("double");
            _double._bytes = 8;
            _double._floating = true;
            _double._attributeType = AttributeType.Double;
            SetAsDefault(_double);

            /// double
            var _decimal = AddType("decimal");
            _decimal._bytes = 12;
            _decimal._floating = true;
            _decimal._attributeType = AttributeType.Float;

            /// string
            var _string = AddType("string");
            _string._base = _char;
            _string._array = true;
            _string._attributeType = AttributeType.String;
            SetAsDefault(_string);

            inited = true;
        }

        public static Type Get(string TypeName)
        {
            initTypes();

            Type o;
            if(!Types.TryGetValue(TypeName, out o))
            {
                o = new Type(TypeName);
            }

            return o;
        }

        #region AttributeType

        public enum AttributeType
        {
            Number,
            Float,
            Double,
            String,
            Name,
            Invalid
        }

        public static AttributeType GetAttributeType(string Name)
        {
            if (Name.HasStringMark())
            {
                return AttributeType.String;
            }
            else if (Char.IsDigit(Name[0]))
            {
                var numb = AttributeType.Number;

                for (int c = 0; c < Name.Length; c++)
                {
                    if (!Char.IsLetter(Name[c]))
                        return AttributeType.Invalid;

                    if (Name[c] == '.')
                        numb = AttributeType.Double;

                    if (c == Name.Length - 1 && Name[c] == 'f')
                        return AttributeType.Float;
                }

                return numb;
            }

            return AttributeType.Name;
        }

        public static Type AttributeTypeToType(AttributeType attrType)
        {
            initTypes();

            switch (attrType)
            {
                case AttributeType.Number:
                    return Types["int"];
                case AttributeType.Float:
                    return Types["float"];
                case AttributeType.Double:
                    return Types["double"];
                case AttributeType.String:
                    return Types["string"];
            }

            return new Type();
        }

        public static string Convert(string cont, Type to)
        {
            var from = GetAttributeType(cont);

            if (to._class)
            {
                //todo
            }

            switch (from)
            {
                case AttributeType.String:
                    return cont.NoMark();

                default:
                    if (to._attributeType == AttributeType.String)
                        return Properties.StringMark + cont;
                    break;
            }

            return cont;
        }

        #endregion

        #endregion

        #region Dynamic

        Type _base;
        AttributeType _attributeType;
        string _name;
        int _bytes;
        bool _floating = false;
        bool _signed = false;
        bool _array = false;
        bool _class = false;

        public Type()
        {
            _name = "var";
        }

        public Type(string Name, int Bytes, bool Floating, bool Signed)
        {
            _name = Name;
            _bytes = Bytes;
            _floating = Floating;
            _signed = Signed;
        }

        public Type(Type Base)
        {
            _base = Base;
            _name = Base._name + "[]";
        }

        public Type(string Name)
        {
            _name = Name;
            _class = true;
        }

        #region Properties

        public Type Base
        {
            get { return _base; }
        }

        public AttributeType MyAttributeType
        {
            get { return _attributeType; }
        }

        public string Name
        {
            get { return _name; }
        }

        public int Bytes
        {
            get { return _bytes; }
        }

        public bool Floating
        {
            get { return _floating; }
        }

        public bool Signed
        {
            get { return _signed; }
        }

        public bool Array
        {
            get { return _array; }
        }

        public bool Class
        {
            get { return _class; }
        }

        #endregion

        public void Solve(Component comp)
        {
            if (_class)
            {
                //todo
            }
        }

        /*public Converter GetConverter
        {
            get
            {
                switch (_name)
                {
                    case "string":
                        return new Converter.String();
                }

                return null;
            }
        }*/

        #endregion

        #region Converters

        /*public class Converter
        {
            
        }*/

        #endregion
    }
}
