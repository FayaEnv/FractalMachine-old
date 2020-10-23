using FractalMachine.Classes;
using System;
using System.Collections.Generic;

namespace FractalMachine.Code
{
    public class Type
    {
        #region Static

        public static Dictionary<string, Type> Types = new Dictionary<string, Type>();
        static bool inited = false;

        public static Type AddType(string Name)
        {
            var t = new Type();
            t._name = Name;
            return t;
        }

        static void initTypes()
        {
            if (inited)
                return;

            /// char
            var _char = AddType("char");
            _char._bytes = 1;
            _char._signed = true;

            /// uchar
            var _uchar = AddType("uchar");
            _uchar._bytes = 1;

            /// short
            var _short = AddType("short");
            _short._bytes = 2;
            _short._signed = true;

            /// ushort
            var _ushort = AddType("ushort");
            _ushort._bytes = 2;

            /// int
            var _int = AddType("int");
            _int._bytes = 4;
            _int._signed = true;

            /// uint
            var _uint = AddType("uint");
            _uint._bytes = 4;

            /// long
            var _long = AddType("long");
            _long._bytes = 8;
            _long._signed = true;

            /// ulong
            var _ulong = AddType("ulong");
            _ulong._bytes = 8;

            /// float
            var _float = AddType("float");
            _float._bytes = 4;
            _float._floating = true;

            /// double
            var _double = AddType("double");
            _double._bytes = 8;
            _double._floating = true;

            /// double
            var _decimal = AddType("decimal");
            _decimal._bytes = 12;
            _decimal._floating = true;

            /// string
            var _string = AddType("string");
            _string._base = _char;
            _string._array = true;

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

        #endregion

        #endregion

        #region Dynamic

        Type _base;
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

        #endregion

        #endregion
    }
}
