using System;
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

        class WordsDispatcher
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

        }

        void Parse(string Script)
        {
            WordsDispatcher dispatcher = new WordsDispatcher();

            dispatcher.OnDispatch = delegate (WordsDispatcher.Word Word)
            {

            };


            ///
            /// Cycle string
            ///
            foreach (char ch in Script)
            {
                dispatcher.Push(ch);
            }

            dispatcher.Flush();

        }

        #endregion
    }
}
