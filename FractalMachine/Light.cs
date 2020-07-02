using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        class Switches
        {
            Dictionary<string, bool> _abc = new Dictionary<string, bool>();

            public delegate void OnSwitchChangedDelegate(bool Value);
            Dictionary<string, OnSwitchChangedDelegate> _delegates = new Dictionary<string, OnSwitchChangedDelegate>();            

            public bool this[string property]
            {
                get
                {
                    bool o;
                    if (_abc.TryGetValue(property, out o))
                    {
                        return o;
                    }

                    return default;
                }

                set
                {
                    bool o;
                    if (_abc.TryGetValue(property, out o) && o != value)
                    {
                        OnSwitchChangedDelegate del;
                        if (_delegates.TryGetValue(property, out del))
                        {
                            del(o);
                        }
                    }

                    _abc[property] = value;
                }
            }

            public void OnSwitchChanged(string Switch, OnSwitchChangedDelegate Delegate)
            {
                _delegates.Add(Switch, Delegate);
            }
        }

        class Switch
        {
            public delegate void OnSwitchChangedDelegate();
            public OnSwitchChangedDelegate OnSwitchChanged;

            bool val;

            public bool Value
            {
                get
                {
                    return val;
                }

                set
                {
                    if (val != value)
                        OnSwitchChanged?.Invoke();

                    val = value;
                }
            }
        }

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

            internal void Eat(string value)
            {
                // generate new child and put it inside
            }

            public class Amanuensis
            {                
                private Switches switches;
                private AST current;
                private string strBuffer;

                private Triggers triggersSymbols = new Triggers();

                private Switch isSymbol = new Switch();

                public Amanuensis()
                {
                    current = new AST();
                    //initCallbacks();

                    var trgString = triggersSymbols.Add(new Triggers.Trigger { Delimiter = "\"" });

                    isSymbol.OnSwitchChanged = delegate
                    {
                        if (!isSymbol.Value)
                        {
                            current.Eat(strBuffer);
                        }
                        else
                        {
                            if (strBuffer.Length > 0)
                            {
                                throw new Exception("Symbol not recognized");
                            }
                        }

                        strBuffer = "";
                    };
                }


                public void Push(char Char)
                {
                    var charType = new CharType(Char);
                    isSymbol.Value = charType.CharacterType == CharType.CharTypeEnum.Symbol;

                    strBuffer += Char;

                    // and if it's a string?

                    if (isSymbol.Value)
                    {

                    } 
                }

                /*private void initCallbacks()
                {
                    switches = new Switches();

                    switches.OnSwitchChanged("isSymbol", delegate (bool Value)
                    {
                        if (Value == false)
                        {
                            // Is text
                            current.Eat(strBuffer);
                        }
                        else
                        {
                            // Is symbol

                        }

                        strBuffer = "";
                    });
                }*/

                public class Triggers
                {
                    List<Trigger> triggers = new List<Trigger>();

                    public Trigger Add(Trigger Trigger)
                    {
                        triggers.Add(Trigger);
                        return Trigger;
                    }

                    public class Trigger
                    {
                        public string Delimiter;
                    }
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
