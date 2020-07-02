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

                    ///
                    /// Enables
                    ///

                    isSymbol.EnableInvoke = delegate
                    {
                        return statusDefault.Enabled;
                    };

                    statusSwitcher.OnTriggered = delegate
                    {

                    };
                }


                public void Push(char Char)
                {
                    var charType = new CharType(Char);
                    isSymbol.Value = charType.CharacterType == CharType.CharTypeEnum.Symbol;

                    strBuffer += Char;

                    // mainStatus: invoke here
                    statusSwitcher.Ping(ref strBuffer);
                }


                public class StatusSwitcher
                {
                    public delegate void OnTriggeredDelegate(Triggers.Trigger Trigger);

                    public OnTriggeredDelegate OnTriggered;
                    public Triggers CurrentStatus;
                    public Dictionary<string, Triggers> statuses = new Dictionary<string, Triggers>();

                    public Triggers Define(string Name)
                    {
                        var status = new Triggers();

                        if (Name == "default")
                        {
                            CurrentStatus = status;
                            status.Enabled = true;
                        }

                        statuses.Add(Name, status);
                        return status;
                    }

                    public void Ping(ref string buffer)
                    {
                        var trigger = CurrentStatus.CheckString(buffer);

                        if(trigger != null)
                        {
                            OnTriggered?.Invoke(trigger);
                            buffer = "";
                        }
                    }
                }

                public class Triggers
                {
                    public bool Enabled = false;
                    List<Trigger> triggers = new List<Trigger>();

                    public Trigger Add(Trigger Trigger)
                    {
                        triggers.Add(Trigger);
                        return Trigger;
                    }

                    public Trigger CheckString(string str)
                    {
                        foreach(Trigger t in triggers)
                        {
                            if (t.Delimiter == str)
                                return t;
                        }

                        return null;
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
