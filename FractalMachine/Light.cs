using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace FractalMachine
{
    public class Light
    {
        #region Static

        public static Light OpenScript(string FileName)
        {
            var text = System.IO.File.ReadAllText(FileName);
            var light = new Light();
            light.Parse(text);
            return light;
        }

        #endregion

        public AST AST;

        #region Parse

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

            AST = amanuensis.GetAST;

        }

        #endregion
    }


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
        private AST parent;
        //private Dictionary<string, List<AST>> subs = new Dictionary<string, List<AST>>();
        private List<AST> childs = new List<AST>();

        // Instruction preview
        internal string subject; // variable name, if,
        internal Type type;
        internal int line, pos;
        internal AST next;

        // e creare un AST specializzato per ogni tipologia di istruzione?
        // no, ma crei un descrittore per ogni tipologia di AST, così da dare un ordine a childs

        public enum Type
        {
            Block,
            Instruction,
            Attribute
        }

        #region Constructor

        public AST(AST Parent, int Line, int Pos) : base()
        {
            parent = Parent;
        }

        #endregion

        #region Properties

        public AST Child
        {
            get
            {
                if (childs.Count == 0)
                    NewChild(line, pos);

                var child = childs[childs.Count - 1];

                while (child.next != null)
                    child = child.next;

                return child;
            }
        }

        #endregion

        #region InternalMethods

        internal AST NewChild(int Line, int Pos, Type type = Type.Instruction)
        {
            var child = new AST(this, Line, Pos) { type = type };
            childs.Add(child);
            return child;
        }

        internal AST SetNext(int Line, int Pos)
        {
            var child = new AST(this, Line, Pos) { };
            next = child;
            return child;
        }

        internal void Next(int Line, int Pos)
        {
            NewChild(Line, Pos);
        }

        internal void Eat(string Value, int Line, int Pos)
        {
            var child = Child;
            child.Insert(Value, Line, Pos);
        }

        internal void Insert(string Value, int Line, int Pos)
        {
            childs.Add(new AST (this, Line, Pos) { subject = Value, type = Type.Attribute });
        }

        #endregion

        public class Amanuensis
        {
            private StatusSwitcher statusSwitcher;
            private AST ast;
            private string strBuffer;

            private Switch isSymbol = new Switch();

            internal int Cycle = 0;

            int Line = 0, Pos = 0;

            public Amanuensis()
            {
                statusSwitcher = new StatusSwitcher(this);
                ast = new AST(null, 0, 0);

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

                statusSwitcher.IgnoreChars = new char[] { '\r' };

                /// Default
                var statusDefault = statusSwitcher.Define("default");

                var trgString = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { "\"", "\'" }, ActivateStatus = "inString" });
                var trgSpace = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { " ", "\t" } });
                var trgNewInstruction = statusDefault.Add(new Triggers.Trigger { Delimiter = ";" });
                var trgAssign = statusDefault.Add(new Triggers.Trigger { Delimiter = "=" });

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

                /// Default

                trgNewInstruction.OnTriggered = delegate
                {
                    ast.Next(Line, Pos);
                };

                trgAssign.OnTriggered = delegate
                {
                    //qui entra in gioco next
                    var child = ast.Child.SetNext(Line, Pos);
                    child.subject = "=";
                    clearBuffer();
                    //child.next
                };

                /// Symbols

                isSymbol.EnableInvoke = delegate
                {
                    return statusDefault.IsEnabled;
                };

                /// Strings

                bool onEscapeString = false;

                /*statusInString.OnEnter = delegate {
                    Console.WriteLine("First call");
                };*/

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
                    eatBufferAndClear();
                };

            }

            #region Buffer

            void eatBufferAndClear()
            {
                ast.Eat(strBuffer, Line, Pos - strBuffer.Length);
                clearBuffer();
            }

            void clearBuffer()
            {
                strBuffer = "";
            }

            #endregion

            public AST GetAST
            {
                get
                {
                    return ast;
                }
            }


            public void Push(char Char)
            {
                var charType = new CharType(Char);
                isSymbol.Value = charType.CharacterType == CharType.CharTypeEnum.Symbol;

                if (Char == '\n')
                {
                    Pos = 0;
                    Line++;
                }
                else if (!statusSwitcher.Ping(Char))
                {
                    strBuffer += Char;
                }

                Pos++;
                Cycle++;
            }


            public class StatusSwitcher
            {
                public delegate void OnTriggeredDelegate(Triggers.Trigger Trigger);

                public OnTriggeredDelegate OnTriggered;
                public Triggers CurrentStatus;
                public Dictionary<string, Triggers> statuses = new Dictionary<string, Triggers>();
                public char[] IgnoreChars;

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
                    if (IgnoreChars != null)
                    {
                        foreach (char ignCh in IgnoreChars)
                        {
                            if (ignCh == ch)
                                return false;
                        }
                    }

                    bool triggered = false;

                    var trigger = CurrentStatus.CheckString(ch);
                    if (trigger != null)
                    {
                        triggered = true;
                        OnTriggered?.Invoke(trigger);
                        trigger.OnTriggered?.Invoke();

                        if (trigger.ActivateStatus != null)
                        {
                            SwitchStatus(trigger.ActivateStatus);

                            if (!string.IsNullOrEmpty(trigger.activatorDelimiter))
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
                    CurrentStatus.OnEnter?.Invoke();
                }
            }

            public class Triggers
            {
                public delegate void OnCycleDelegate();

                public OnCycleDelegate OnCycleEnd, OnEnter;
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

                        if (OnSpecificCycle.TryGetValue(cycle, out onCycle))
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

                            if (t.Delimiter != null && stringQueue.Check(parseDelimiter(t.Delimiter)))
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
}
