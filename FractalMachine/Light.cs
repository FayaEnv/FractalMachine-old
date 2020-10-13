using FractalMachine.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
        private Linear linear;

        #region Parse

        public void Parse(string Script)
        {
            ///
            /// Cycle string
            ///
            var amanuensis = new AST.Amanuensis();

            amanuensis.Read(Script);
            AST = amanuensis.GetAST;

            linear = new Linear(AST);
        }

        #endregion

        #region Classes

        // Work in progress
        public class Linear
        {
            public Dictionary<string, Linear> Blocks = new Dictionary<string, Linear>();
            public List<Instruction> Instructions = new List<Instruction>();

            public Linear() { }

            public Linear(AST OriginAST)
            {
                readAst(OriginAST);
            }

            public class Instruction
            {
                public string Op;
                public string Name;
                public ListString Attributes = new ListString();
            }

            #region FromAST

            int blockRtrn = 0;
            
            private void readAst(AST instr, Instruction from = null)
            {
                switch (instr.type)
                {
                    case AST.Type.Block:
                        readAstBlock(instr, from);
                        break;

                    case AST.Type.Instruction:
                        readAstInstruction(instr, from);
                        break;
                }
            }


            private void readAstBlock(AST instr, Instruction from = null)
            {
                foreach (var child in instr.children)
                {
                    // always excpeted Type.Instruction
                    readAst(child);
                }

                if (instr.subject == "(")
                    readAstParenthesis(instr, from);
            }

            private void readAstParenthesis(AST instr, Instruction from = null)
            {
                if (from != null)
                {
                    var ret = "@rtrn" + (blockRtrn++);
                    var i = new Instruction { Op = "assign" };
                    i.Attributes.Add(ret);
                    Instructions.Add(i);
                    from.Attributes.Add(ret);
                }
            }

            private void readAstInstruction(AST instr, Instruction from = null)
            {
                var i = new Instruction();

                bool accumulator = false;

                if(instr.IsDeclaration)
                    Instructions.Add(i);

                if (instr.IsOperator)
                {
                    i.Op = instr.subject;

                    if (instr.subject == ".")
                    {
                        accumulator = true;
                        goto readChildren;
                    }
                        
                    if (instr.IsAssign)
                    {
                        i.Op = "assign";
                    }

                    i.Attributes.Add(from.Attributes.Pop());
                    
                }

                readChildren:

                AST prevAst = null;
                foreach (var child in instr.children)
                {
                    switch (child.type)
                    {
                        case AST.Type.Attribute:
                            if (!accumulator)
                                i.Attributes.Add(child.subject);
                            else
                                from.Attributes.AddToLast(child.subject);

                            i.Name = child.subject;
                            break;

                        case AST.Type.Instruction:
                            readAstInstruction(child, i);

                            break;

                        case AST.Type.Block:

                            if (child.subject == "(" && prevAst?.type == AST.Type.Attribute)
                                readAstFunctionCall(child, i);
                            else
                                readAstBlock(child, i);

                            break;
                    }

                    prevAst = child;
                }

                if (instr.IsOperator && instr.subject != ".")
                {
                    Instructions.Add(i);
                }

            }
            
            private void readAstFunctionCall(AST instr, Instruction from)
            {
                var i = new Instruction();
                i.Op = "call";
                i.Name = from.Attributes.Pop();

                Instructions.Add(i);
            }

            #endregion
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

        public void Toggle()
        {
            Value = !Value;
        }
    }

    public class AST
    {
        private AST parent;
        //private Dictionary<string, List<AST>> subs = new Dictionary<string, List<AST>>();
        internal List<AST> children = new List<AST>();

        // Instruction preview
        internal string subject, aclass; // variable name, if,
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

        public AST Next
        {
            get
            {
                if (children.Count == 0)
                    return null;

                return children[children.Count - 1];
            }
        }

        public bool IsOperator
        {
            get
            {
                return aclass == "operator" || aclass == "fastOperator";
            }
        }

        public bool IsAssign
        {
            get
            {
                return IsOperator && subject == "=";
            }
        }

        public bool IsBlockDeclaration
        {
            get
            {
                //todo: has up to 2 attributes, 1 block ( and 1 block {
                return false;
            }
        }

        public bool IsDeclaration
        {
            get
            {
                var numAttr = 0;
                foreach(var child in children)
                {
                    if (child.type == Type.Attribute)
                        numAttr++;
                    else
                        break;
                }

                return numAttr>1;
            }
        }

        public AST BeforeMe
        {
            get
            {
                if (parent == null)
                    return null;
                var pos = parent.children.IndexOf(this);
                
                if (pos == 0) 
                    return null;

                return parent.children[pos - 1];
            }
        }

        public string Subject
        {
            get
            {
                if (subject != null)
                {
                    return subject;
                }
                else
                {
                    if (children.Count > 0 && children[0].type == Type.Attribute)
                        return children[0].subject;
                }

                return null;
            }
        }

        #endregion

        #region InternalProperties

        internal AST Instruction
        {
            get
            {
                var child = LastChild;

                while(child?.LastChild != null && child.LastChild.type == Type.Instruction)
                {
                    child = child.LastChild;
                }

                return child;
            }
        }

        internal AST LastChild
        {
            get
            {
                if (children.Count == 0)
                    return null;

                return children[children.Count - 1];
            }
        }

        internal AST GetTopBlock
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
            children.Add(child);
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
                var trgOperators = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { "==", "!=", "=", ".", "+", "-", "/", "%", "*", "&&", "||", "&", "|", ",", ":" } });
                var trgFastOperation = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { "++", "--" } });

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

                /// Vars
                var autocloseBlocks = new string[] { "if", "else", "while" };

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
                    child.aclass = "operator";
                    clearBuffer();
                };

                trgFastOperation.OnTriggered = delegate (Triggers.Trigger trigger)
                {
                    var instr = curAst.Instruction.LastChild.NewInstruction(Line, Pos);
                    instr.subject = trigger.activatorDelimiter;
                    instr.aclass = "fastOperator";
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

                    var del = trigger.activatorDelimiter;

                    bool correct = true;
                    if (del == "}") correct = ast.subject == "{";
                    if (del == ")") correct = ast.subject == "(";
                    if (del == "]") correct = ast.subject == "[";

                    if (!correct)
                        throw new Exception("Closing " + ast.subject + " block with " + del + " on line " + Line);

                    if (del == "}" && autocloseBlocks.Contains(ast.Subject))
                    {
                        trgNewInstruction.Trig();
                    }
                };

                trgSpace.OnTriggered = delegate
                {
                    eatBufferAndClear();
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

                ///
                /// Statuses
                ///

                statusDefault.OnCharCycle = delegate (char ch)
                {
                    var charType = new CharType(ch);
                    isSymbol.Value = charType.CharacterType == CharType.CharTypeEnum.Symbol;
                };

                statusDefault.OnTriggered = delegate 
                {
                    isSymbol.Toggle();
                };

                statusInComment.OnExit = statusInInlineComment.OnExit = delegate
                {
                    clearBuffer();
                };


                //statusSwitcher.DefineCompleted();
            }

            #region BufferAndAst

            void eatBufferAndClear()
            {
                //todo: check if strBuffer is text
                if (strBuffer.Length > 0)
                {
                    curAst.InsertAttribute(Line, Pos - strBuffer.Length, strBuffer);
                    clearBuffer();
                }
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

            public void Read(string str)
            {
                for(int c=0; c<str.Length; c++)
                {
                    var Char = str[c];

                    if (Char != '\r')
                    {
                        // Count position
                        if (Char == '\n')
                        {
                            Pos = 0;
                            Line++;
                        }
                        else
                            Pos++;

                        CharTree ct = statusSwitcher.CurrentStatus.delimetersTree;
                        CharTree val = null;

                        for (int cc = c; cc < str.Length; cc++)
                        {
                            var ch = str[cc];

                            ct = ct.CheckChar(ch);

                            if (ct == null) break;

                            if (ct.value != null)
                            {
                                // check first if enabled
                                var t = (Triggers.Trigger)ct.value;

                                if(t.IsEnabled == null || t.IsEnabled.Invoke())
                                    val = ct;
                            }
                        }

                        if (val != null)
                        {
                            var trigger = (Triggers.Trigger)val.value;
                            trigger.activatorDelimiter = val.String;
                            statusSwitcher.Triggered(trigger);

                            var add = trigger.activatorDelimiter.Length;
                            c += add - 1;
                            Pos += add;
                        }
                        else
                        { 
                            statusSwitcher.Cycle(Char);
                            strBuffer += Char;
                        }

                        statusSwitcher.UpdateCurrentStatus();
                        Cycle++;
                    }
                }
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

                public void Cycle(char ch)
                {
                    CurrentStatus.OnCharCycle?.Invoke(ch);
                }

                internal void Triggered(Triggers.Trigger trigger)
                {
                    OnTriggered?.Invoke(trigger);
                    CurrentStatus.OnTriggered?.Invoke(trigger);
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
                    CurrentStatus.OnExit?.Invoke();
                    CurrentStatus = statuses[status];
                    CurrentStatus.IsEnabled = true;
                    CurrentStatus.OnEnter?.Invoke();
                }
            }

            public class Triggers
            {
                public delegate void OnCycleDelegate();
                public delegate void OnCharCycleDelegate(char Char);

                public OnCycleDelegate OnCycleEnd, OnEnter, OnExit;
                public OnCharCycleDelegate OnCharCycle;
                public Dictionary<int, OnCycleDelegate> OnSpecificCycle = new Dictionary<int, OnCycleDelegate>();
                public StatusSwitcher.OnTriggeredDelegate OnTriggered;
                public bool IsEnabled = false;

                internal StatusSwitcher Parent;
                internal CharTree delimetersTree = new CharTree();
                List<Trigger> triggers = new List<Trigger>();

                private StringQueue stringQueue = new StringQueue();

                public Triggers(StatusSwitcher Parent)
                {
                    this.Parent = Parent;
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

                public bool PushChar(char ch)
                {
                    stringQueue.Push(ch);

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
                                    Parent.Triggered(t);
                                    return true;
                                }
                            }
                        }
                    }

                    return false;
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

                    public string activatorDelimiter;
                 
                    public void Trig()
                    {
                        OnTriggered?.Invoke(this);
                    }
                }
            }
        }
    }
}
