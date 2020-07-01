﻿using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine
{
    public class Light
    {
        #region Static

        public static void OpenScript(string FileName)
        {

        }

        #endregion

        #region Parse

        

        /*class WordsDispatcher
        {
            public class Word
            {
                public string String;
                public bool IsAlphanumeric;
            }

            public delegate void OnDispatchDelegate(Word Word);
            public OnDispatchDelegate OnDispatch;

            public CharType prevCharType;
            List<string> strings = new List<string>();
            string curString = "";

            public void Push(char Char)
            {
                var charType = new CharType(Char);

                if(prevCharType != null)
                {
                    if(charType.IsAlphanumeric != prevCharType.IsAlphanumeric)
                    {
                        Flush();
                    }
                }

                curString += Char;
                prevCharType = charType;

                // check for corrispondece
            }

            public void Flush()
            {
                var word = new Word();
                word.String = curString;
                word.IsAlphanumeric = prevCharType.IsAlphanumeric;

                OnDispatch(word);
                curString = "";
            }

        }*/

        public class AST
        {
            private AST parent, current;
            private List<AST> childs = new List<AST>();

            #region Constructor

            public AST()
            {

            }

            public AST(AST Parent)
            {
                parent = Parent;
            }

            #endregion

            public class Amanuensis
            {
                private AST current;
                private bool lastWasSymbol = false;
                private string strBuffer;

                public void Push(char Char)
                {
                    var charType = new CharType(Char);

                    if (charType.CharacterType == CharType.CharTypeEnum.Symbol)
                    {
                        if (!lastWasSymbol)
                        {
                            //todo: flush string
                            flushText();
                        }

                        lastWasSymbol = true;
                    }
                    else
                    {
                        if (!lastWasSymbol)
                        {

                        }

                        strBuffer += Char;
                    }
                }

                private void flushText()
                {
                    //todo: continue here
                }
            }
        }

        void Parse(string Script)
        {
            ///
            /// Cycle string
            ///
            var amanuensis = new AST.Amanuensis();

            foreach (char ch in Script)
            {
                amanuensis.Push(ch);
            } 

        }

        #endregion
    }
}
