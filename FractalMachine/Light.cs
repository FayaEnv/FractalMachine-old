using FractalMachine.Classes;
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

        // e creare un AST specializzato per ogni tipologia di istruzione?
        // no, ma crei un descrittore per ogni tipologia di AST, così da dare un ordine a childs

        public enum Type
        {
            Block,
            Instruction,
            Attribute
        }

        #region Constructor

        public AST(AST Parent, int Line, int Pos, Type type = Type.Block) : base()
        {
            parent = Parent;
            line = Line;
            pos = Pos;
            this.type = type;

            if (type == Type.Block)
                NewChild(Line, Pos, Type.Instruction);
        }

        #endregion

        #region Properties

        public AST Instruction
        {
            get
            {
                var child = LastChild;

                if(child?.LastChild != null && child.LastChild.type == Type.Instruction)
                {
                    return child.LastChild;
                }

                return child;
            }
        }

        public AST LastChild
        {
            get
            {
                if (childs.Count == 0)
                    return null;

                return childs[childs.Count - 1];
            }
        }

        public AST GetTopBlock
        {
            get
            {
                var a = parent;
                while (a.type != Type.Block)
                    a = a.parent;
                return a;
            }
        }

        #endregion

        #region Methods

        internal AST NewChild(int Line, int Pos, Type type)
        {
            var child = new AST(this, Line, Pos, type);
            childs.Add(child);
            return child;
        }

        internal AST NewInstruction(int Line, int Pos)
        {
            return NewChild(Line, Pos, Type.Instruction);
        }

        internal void InsertAttribute(int Line, int Pos, string Content)
        {
            var ast = Instruction;
            var child = ast.NewChild(Line, Pos, Type.Attribute);
            child.subject = Content;
        }

        #endregion

        public class Amanuensis
        {
            private StatusSwitcher statusSwitcher;
            private AST mainAst, curAst;
            private string strBuffer;

            private Switch isSymbol = new Switch();

            internal int Cycle = 0;

            int Line = 0, Pos = 0;

            public Amanuensis()
            {
                statusSwitcher = new StatusSwitcher(this);
                curAst = mainAst = new AST(null, 0, 0);

                ///
                /// Define triggers
                ///

                /// Default
                var statusDefault = statusSwitcher.Define("default");

                var trgString = statusDefault.Add(new Triggers.Trigger { Delimiter = "\"", ActivateStatus = "inString" });
                var trgSpace = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { " ", "\t", "," } });
                var trgNewInstruction = statusDefault.Add(new Triggers.Trigger { Delimiter = ";" });
                var trgNewLine = statusDefault.Add(new Triggers.Trigger { Delimiter = "\n" });
                var trgOperators = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { "==", "!=", "=", ".", "+", "-", "/", "%" } });

                var trgOpenBlock = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { "(", "{", "[" } });
                var trgCloseBlock = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { ")", "}", "]" } });

                var trgInlineComment = statusDefault.Add(new Triggers.Trigger { Delimiter = "//", ActivateStatus = "inInlineComment" });
                var trgComment = statusDefault.Add(new Triggers.Trigger { Delimiter = "/*", ActivateStatus = "inComment" });

                /// InString
                var statusInString = statusSwitcher.Define("inString");
                var trgEscapeString = statusInString.Add(new Triggers.Trigger { Delimiter = "\\" });
                var trgExitString = statusInString.Add(new Triggers.Trigger { Delimiter = "\"", ActivateStatus = "default" });

                /// InlineComment
                var statusInInlineComment = statusSwitcher.Define("inInlineComment");
                var trgExitInlineComment = statusInInlineComment.Add(new Triggers.Trigger { Delimiter = "\n", ActivateStatus = "default" });

                /// Comment
                var statusInComment = statusSwitcher.Define("inComment");
                var trgExitComment = statusInComment.Add(new Triggers.Trigger { Delimiter = "*/", ActivateStatus = "default" });

                ///
                /// Delegates
                ///

                /// StatusSwitcher
                statusSwitcher.OnTriggered = delegate (Triggers.Trigger trigger)
                {
                    Debug.Print("Trigger activated by " + trigger.activatorDelimiter);
                };

                /// Default

                trgNewInstruction.OnTriggered = delegate
                {
                    curAst.NewInstruction(Line, Pos);
                };

                trgOperators.OnTriggered = delegate (Triggers.Trigger trigger)
                {
                    var child = curAst.Instruction.NewChild(Line, Pos, Type.Instruction);
                    child.subject = trigger.activatorDelimiter;
                    clearBuffer();
                };

                trgOpenBlock.OnTriggered = delegate(Triggers.Trigger trigger)
                {
                    var child = curAst.Instruction.NewChild(Line, Pos, Type.Block);
                    child.subject = trigger.activatorDelimiter;
                    curAst = child;
                    clearBuffer();
                };

                trgCloseBlock.OnTriggered = delegate (Triggers.Trigger trigger)
                {                    
                    var ast = curAst;

                    closeBlock();

                    bool isFirstLevelBlock = ast.parent.parent.type == Type.Block;
                    if (trigger.activatorDelimiter == "}" && isFirstLevelBlock)
                    {
                        trgNewInstruction.Trig();
                    }
                };

                /// Symbols

                isSymbol.EnableInvoke = delegate
                {
                    return statusDefault.IsEnabled;
                };

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
                            statusSwitcher.Ping(strBuffer);
                            clearBuffer();
                        }
                    }
                };

                /// Strings

                bool onEscapeString = false;

                /*statusInString.OnEnter = delegate {
                    Debug.Print("First call");
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

                statusSwitcher.DefineCompleted();
            }

            #region Buffer

            void topAst()
            {
                curAst = curAst.parent;
            }

            void eatBufferAndClear()
            {
                curAst.InsertAttribute(Line, Pos - strBuffer.Length, strBuffer);
                Count(strBuffer);
                clearBuffer();
            }

            void clearBuffer()
            {
                strBuffer = "";
            }

            void closeBlock()
            {
                curAst = curAst.GetTopBlock;
            }

            #endregion

            public AST GetAST
            {
                get
                {
                    return mainAst;
                }
            }


            char[] ignoreChars = new char[] { '\r' };

            public void Push(char Char)
            {
                var charType = new CharType(Char);
                isSymbol.Value = charType.CharacterType == CharType.CharTypeEnum.Symbol;

                foreach (char ignCh in ignoreChars)
                {
                    if (ignCh == Char) return;
                }

                strBuffer += Char;
            }



            public void Count(string str)
            {
                foreach(char ch in str)
                {
                    if (ch == '\n')
                    {
                        Pos = 0;
                        Line++;
                    }
                    else
                        Pos++;
                }
            }


            public class StatusSwitcher
            {
                public delegate void OnTriggeredDelegate(Triggers.Trigger Trigger);

                public OnTriggeredDelegate OnTriggered;
                public Triggers CurrentStatus;
                public Dictionary<string, Triggers> statuses = new Dictionary<string, Triggers>();
                public char[] IgnoreChars;

                Triggers.Trigger triggerInTheBarrel;

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

                public void DefineCompleted()
                {
                    foreach(var status in statuses)
                    {
                        status.Value.DefineCompleted();
                    }
                }

                public bool Ping(string str)
                {
                    for(int c=0; c<str.Length; c++)
                    {
                        CharTree ct;
                        CharTree val = null;
                        var cc = c;

                        while(cc < str.Length && (ct = CurrentStatus.delimetersTree.CheckChar(str[cc++])) != null)
                        {
                            if (ct.value != null)
                                val = ct;
                        }

                        if(val != null)
                        {
                            Triggers.Trigger trigger = (Triggers.Trigger) val.value;
                            trigger.activatorDelimiter = val.String;
                        }
                    }

                    return false;

                    // old ping
                    /*if (triggerInTheBarrel != null)
                    {
                        triggerInTheBarrel.inTheBarrelFor += ch;
                        if (!triggerInTheBarrel.StillHasAdversary())
                        {
                            trig(triggerInTheBarrel);
                            triggerInTheBarrel = null;
                        }
                    }



                    bool triggered = false;

                    var trigger = CurrentStatus.CheckString(ch);
                    if (trigger != null)
                    {
                        triggered = true;

                        if (trigger.HasAdversary())
                        {
                            trigger.inTheBarrelFor = trigger.activatorDelimiter;
                            triggerInTheBarrel = trigger;
                        }
                        else
                        {
                            triggerInTheBarrel = null;
                            trig(trigger);
                        }
                    }

                    UpdateCurrentStatus();

                    if (!triggered)
                    {

                    }

                    return triggered;*/
                }

                private void trig(Triggers.Trigger trigger)
                {
                    OnTriggered?.Invoke(trigger);
                    trigger.OnTriggered?.Invoke(trigger);

                    if (trigger.ActivateStatus != null)
                    {
                        SwitchStatus(trigger.ActivateStatus);
                    }
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
                public bool IsEnabled = false;

                internal StatusSwitcher Parent;
                internal CharTree delimetersTree = new CharTree();
                List<Trigger> triggers = new List<Trigger>();
                KeyLengthSortedDescDictionary<Trigger> triggersByDelimeters = new KeyLengthSortedDescDictionary<Trigger>();

                private StringQueue stringQueue = new StringQueue();

                public Triggers(StatusSwitcher Parent)
                {
                    this.Parent = Parent;
                }

                public void DefineCompleted()
                {
                    foreach(var trigger in triggers)
                    {
                        List<string> dynDels = new List<string>();

                        foreach(var del in trigger.Delimiters)
                        {
                            if (!del.StartsWith("€$"))
                            {
                                if (!delimetersTree.CheckAlone(del))
                                {
                                    trigger.adversaries.Add(del);
                                }

                                triggersByDelimeters.Add(del, trigger);
                            }
                            else
                                dynDels.Add(del);
                        }

                        trigger.Delimiters = dynDels.ToArray();
                    }
                }

                public Trigger Add(Trigger Trigger)
                {
                    if (Trigger.Delimiter != null)
                        Trigger.Delimiters = new string[] { Trigger.Delimiter };

                    if (Trigger.Delimiters == null)
                        throw new Exception("No delimiter is specified");

                    Trigger.Parent = this;
                    triggers.Add(Trigger);

                    foreach (var del in Trigger.Delimiters)
                        delimetersTree.Insert(del, Trigger);

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

                public Trigger CheckString(char ch)
                {
                    stringQueue.Push(ch);

                    // Check for static delimiters
                    foreach(var td in triggersByDelimeters)
                    {
                        var t = td.Value;
                        var del = td.Key;

                        if (stringQueue.Check(del))
                        {                          
                            bool enabled = t.IsEnabled == null || t.IsEnabled.Invoke();
                            if (enabled)
                            {
                                t.activatorDelimiter = del;
                                return t;
                            }
                        }
                    }

                    // Check for dynamic delimiters
                    foreach (Trigger t in triggers)
                    {
                        bool enabled = t.IsEnabled == null || t.IsEnabled.Invoke();

                        if (enabled)
                        {
                            foreach (string del in t.Delimiters)
                            {
                                if (stringQueue.Check(del))
                                {
                                    // should be putted in new environment
                                    t.activatorDelimiter = del;
                                    return t;
                                }
                            }
                        }
                    }

                    return null;
                }

                public class Trigger
                {
                    //public delegate void OnTriggeredDelegate();
                    public delegate bool IsEnabledDelegate();

                    public Triggers Parent;                  
                    public StatusSwitcher.OnTriggeredDelegate OnTriggered;
                    public IsEnabledDelegate IsEnabled;
                    public string Delimiter;
                    public string[] Delimiters;
                    public string ActivateStatus;

                    public List<string> adversaries = new List<string>();
                    public string activatorDelimiter;
                    public string inTheBarrelFor = "";
                 
                    public void Trig()
                    {
                        OnTriggered?.Invoke(this);
                    }

                    public bool HasAdversary()
                    {
                        return adversaries.Contains(activatorDelimiter);
                    }

                    public bool StillHasAdversary()
                    {
                        return (Parent.delimetersTree.CheckString(inTheBarrelFor) != null);
                    }
                }
            }
        }
    }
}
