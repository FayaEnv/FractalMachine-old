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
                    if (val != value && (EnableInvoke == null || EnableInvoke.Invoke()))
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

            internal AST Eat(string value)
            {
                // generate new child and put it inside
                return null;
            }

            public class Amanuensis
            {
                private StatusSwitcher statusSwitcher;
                private AST current;
                private string strBuffer;

                private Switch isSymbol = new Switch();

                internal int Cycle = 0;

                public Amanuensis()
                {
                    statusSwitcher = new StatusSwitcher(this);
                    current = new AST();

                    isSymbol.OnSwitchChanged = delegate
                    {
                        if (!isSymbol.Value)
                        {
                            eatBufferAndClear();
                        }
                        else
                        {
                            if (strBuffer.Length > 0)
                            {
                                throw new Exception("Symbol not recognized");
                            }
                        }
                    };

                   
                    ///
                    /// Define triggers
                    ///

                    /// Default
                    var statusDefault = statusSwitcher.Define("default");

                    var trgString = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { "\"", "\'" }, ActivateStatus = "inString" });

                    /// InString
                    var statusInString = statusSwitcher.Define("inString");
                    var trgEscapeString = statusInString.Add(new Triggers.Trigger { Delimiter = "\\" });
                    var trgExitString = statusInString.Add(new Triggers.Trigger { Delimiter = "€$activatorDelimiter" });

                    ///
                    /// Delegates
                    ///

                    /// StatusSwitcher
                    statusSwitcher.OnTriggered = delegate (Triggers.Trigger trigger)
                    {
                        Console.WriteLine("Trigger activated by " + trigger.activatorDelimiter);
                    };

                    /// Strings

                    bool onEscapeString = false;

                    trgEscapeString.OnTriggered = delegate
                    {
                        onEscapeString = true;

                        statusInString.OnNextCycleEnd = delegate
                        {
                            onEscapeString = false;
                        };
                    };

                    trgExitString.IsEnabled = delegate
                    {
                        return !onEscapeString;
                    };

                    trgExitString.OnTriggered = delegate
                    {
                        Console.WriteLine("here " + strBuffer);
                        eatBufferAndClear();
                    };

                    /// Symbols

                    isSymbol.EnableInvoke = delegate
                    {
                        return statusDefault.IsEnabled;
                    };
                }

                AST eatBufferAndClear()
                {
                    var ast = current.Eat(strBuffer);
                    strBuffer = "";
                    return ast;
                }

                public AST GetAST
                {
                    get
                    {
                        return current;
                    }
                }


                public void Push(char Char)
                {
                    var charType = new CharType(Char);
                    isSymbol.Value = charType.CharacterType == CharType.CharTypeEnum.Symbol;

                    if (!statusSwitcher.Ping(Char))
                    {
                        strBuffer += Char;
                    }

                    Cycle++;
                }


                public class StatusSwitcher
                {
                    public delegate void OnTriggeredDelegate(Triggers.Trigger Trigger);

                    public OnTriggeredDelegate OnTriggered;
                    public Triggers CurrentStatus;
                    public Dictionary<string, Triggers> statuses = new Dictionary<string, Triggers>();

                    internal Amanuensis Parent;

                    public StatusSwitcher(Amanuensis Parent)
                    {
                        this.Parent = Parent;
                    }

                    public Triggers Define(string Name)
                    {
                        var status = new Triggers(this);

                        if (Name == "default")
                        {
                            CurrentStatus = status;
                            status.IsEnabled = true;
                        }

                        statuses.Add(Name, status);
                        return status;
                    }

                    public bool Ping(char ch)
                    {
                        bool triggered = false;

                        var trigger = CurrentStatus.CheckString(ch);

                        if(trigger != null)
                        {
                            triggered = true;
                            OnTriggered?.Invoke(trigger);
                            trigger.OnTriggered?.Invoke();

                            if (trigger.ActivateStatus != null)
                            {
                                SwitchStatus(trigger.ActivateStatus);

                                if(!string.IsNullOrEmpty(trigger.activatorDelimiter))
                                    CurrentStatus.Abc["activatorDelimiter"] = trigger.activatorDelimiter;
                            }
                        }

                        UpdateCurrentStatus();

                        return triggered;
                    }

                    public void UpdateCurrentStatus()
                    {
                        CurrentStatus.OnCycleEnd?.Invoke();
                        CurrentStatus.OnNextCycleEnd?.Invoke();
                    }

                    public void SwitchStatus(string status)
                    {
                        CurrentStatus.IsEnabled = false;
                        CurrentStatus = statuses[status];
                        CurrentStatus.IsEnabled = true;
                        CurrentStatus.OnFirstCall?.Invoke();
                    }
                }

                public class Triggers
                {
                    public delegate void OnCycleDelegate();

                    public OnCycleDelegate OnCycleEnd, OnFirstCall;
                    public Dictionary<int, OnCycleDelegate> OnSpecificCycle = new Dictionary<int, OnCycleDelegate>();
                    public Dictionary<string, string> Abc = new Dictionary<string, string>();
                    public bool IsEnabled = false;

                    internal StatusSwitcher Parent;
                    List<Trigger> triggers = new List<Trigger>();
                    
                    private StringQueue stringQueue = new StringQueue();

                    public Triggers(StatusSwitcher Parent)
                    {
                        this.Parent = Parent;
                    }

                    public Trigger Add(Trigger Trigger)
                    {
                        Trigger.Parent = this;
                        triggers.Add(Trigger);
                        return Trigger;
                    }

                    public OnCycleDelegate OnNextCycleEnd
                    {
                        get
                        {
                            var cycle = Parent.Parent.Cycle;
                            OnCycleDelegate onCycle;

                            if(OnSpecificCycle.TryGetValue(cycle, out onCycle))
                            {
                                OnSpecificCycle.Remove(cycle);
                                return onCycle;
                            }

                            return null;
                        }

                        set
                        {
                            OnSpecificCycle.Add(Parent.Parent.Cycle + 1, value);
                        }
                    }

                    private string parseDelimiter(string del)
                    {
                        if (del.StartsWith("€$"))
                        {
                            string name = del.Substring(2);
                            string o;
                            Abc.TryGetValue(name, out o);
                            return o;
                        }

                        return del;
                    }

                    public Trigger CheckString(char ch)
                    {
                        stringQueue.Push(ch);

                        foreach (Trigger t in triggers)
                        {
                            bool enabled = t.IsEnabled == null || t.IsEnabled.Invoke();

                            if (enabled)
                            {
                                //priority to Delimiters
                                if (t.Delimiters != null)
                                {
                                    foreach (string del in t.Delimiters)
                                    {
                                        if (stringQueue.Check(parseDelimiter(del)))
                                        {
                                            // should be putted in new environment
                                            t.activatorDelimiter = del;
                                            return t;
                                        }
                                    }
                                }

                                if (stringQueue.Check(parseDelimiter(t.Delimiter)))
                                {
                                    t.activatorDelimiter = t.Delimiter; // yes, it is obvious...
                                    return t;
                                }
                            }
                        }

                        return null;
                    }

                    public class Trigger
                    {
                        public delegate void OnTriggeredDelegate();
                        public delegate bool IsEnabledDelegate();

                        public Triggers Parent;
                        public OnTriggeredDelegate OnTriggered;
                        public IsEnabledDelegate IsEnabled;
                        public string Delimiter;
                        public string[] Delimiters;
                        public string ActivateStatus;

                        public string activatorDelimiter;
                    }
                }
            }
        }

        public void Parse(string Script)
        {
            ///
            /// Cycle string
            ///
            var amanuensis = new AST.Amanuensis();

            foreach (char ch in Script)
            {
                amanuensis.Push(ch);
            }

            var ast = amanuensis.GetAST;
            Console.WriteLine("Here we arrived");

        }

        #endregion
    }
}
