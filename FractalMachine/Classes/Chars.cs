using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FractalMachine
{
    public class CharType
    {

        #region Properties

        public char Char { get; }
        public TypeEnum Type { get; }
        public CharTypeEnum CharacterType { get; } = CharTypeEnum.None;
        public AlphaTypeEnum AlphaType { get; } = AlphaTypeEnum.None;
        public SymbolBlockEnum SymbolBlock { get; } = SymbolBlockEnum.None;

        public bool IsAlphanumeric
        {
            get
            {
                return CharacterType == CharTypeEnum.Alpha || CharacterType == CharTypeEnum.Numeric;
            }
        }

        #endregion

        public CharType(char Char)
        {
            // http://www.asciitable.com/
            this.Char = Char;
            int i = (int)Char;

            // relax, re do it
            Type = (i >= 32 && i < 127) ? TypeEnum.Char : TypeEnum.Signal;

            if (Type == TypeEnum.Char)
            {
                if (i <= 57)
                {
                    if (i >= 48)
                    {
                        CharacterType = CharTypeEnum.Numeric;
                    }
                    else
                    {
                        CharacterType = CharTypeEnum.Symbol;
                        SymbolBlock = SymbolBlockEnum.First;
                    }

                }
                else if (i <= 64)
                {
                    CharacterType = CharTypeEnum.Symbol;
                    SymbolBlock = SymbolBlockEnum.Second;
                }
                else if (i <= 122)
                {
                    CharacterType = CharTypeEnum.Alpha;

                    if (i <= 96)
                    {
                        if (i >= 91)
                        {
                            CharacterType = CharTypeEnum.Symbol;
                            SymbolBlock = SymbolBlockEnum.Third;
                        }
                        else
                        {
                            AlphaType = AlphaTypeEnum.Uppercase;
                        }
                    }
                    else
                    {
                        AlphaType = AlphaTypeEnum.Lowercase;
                    }
                }
                else
                {
                    CharacterType = CharTypeEnum.Symbol;
                    SymbolBlock = SymbolBlockEnum.Fourth;
                }
            }
        }

        #region Enums

        public enum TypeEnum
        {
            Signal,
            Char
        }

        public enum CharTypeEnum
        {
            None,
            Alpha,
            Numeric,
            Symbol
        }

        public enum AlphaTypeEnum
        {
            None,
            Uppercase,
            Lowercase
        }

        public enum SymbolBlockEnum
        {
            None = -1,
            First,
            Second,
            Third,
            Fourth
        }

        #endregion
    }

    public class StringQueue
    {
        int maxLength = 0;
        Char last;

        public class Char
        {
            public char Value;
            public Char Previous;
        }

        public void Push(char ch)
        {
            last = new Char { Value = ch, Previous = last };

            if(maxLength > 0)
            {
                Char Ch = last;
                for(int i=0; i<maxLength; i++)
                {           
                    Ch = Ch.Previous;
                    if (Ch == null) goto End;
                }

                Ch.Previous = null;

                End:;
            }
        }

        public bool Check(string str)
        {
            if (str.Length > maxLength)
                maxLength = str.Length;

            Char Ch = null;
            for(int i=str.Length-1; i>=0; i--)
            {
                var ch = str[i];

                if (Ch == null)
                    Ch = last;
                else if ((Ch = Ch.Previous) == null)
                    return false;

                if (ch != Ch.Value)
                    return false;
            }

            return true;
        }
    }
}
