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

        

        

        class Switch
        {
            public delegate void OnSwitchChangedDelegate();
            public delegate bool EnableInvokeDelegate();

            public OnSwitchChangedDelegate OnSwitchChanged;
            public EnableInvokeDelegate EnableInvoke;

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
                private StatusSwitcher statusSwitcher;
                private AST current;
                private string strBuffer;

                private Switch isSymbol = new Switch();
                private bool isString;

                public Amanuensis()
                {
                    current = new AST();

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

                    /*isSymbol.EnableInvoke = delegate
                    {
                        return !isString.Value;
                    };*/


                    ///
                    /// Define triggers
                    ///

                    /// Default
                    var statusDefault = statusSwitcher.Define("default");

                    var trgString = statusDefault.Add(new Triggers.Trigger { Delimiter = "\"", ActivateStatus = "inString" });
                    trgString.OnTriggered = delegate
                    {
                        isString = true;
                    };

                    /// InString
                    var statusInString = statusSwitcher.Define("inString");
                }


                public void Push(char Char)
                {
                    var charType = new CharType(Char);
                    isSymbol.Value = charType.CharacterType == CharType.CharTypeEnum.Symbol;

                    strBuffer += Char;

                    // mainStatus: invoke here
                    
                }


                public class StatusSwitcher
                {
                    public delegate void OnInvokeDelegate();

                    public OnInvokeDelegate OnInvoke;
                    public string Name;
                    public Dictionary<string, Triggers> statuses = new Dictionary<string, Triggers>();

                    public Triggers Define(string Name)
                    {
                        var status = new Triggers();
                        statuses.Add(Name, status);
                        return status;
                    }
                }

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
                        public delegate void OnTriggeredDelegate();

                        public OnTriggeredDelegate OnTriggered;
                        public string Delimiter;
                        public string ActivateStatus;
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
