using System;
using System.Collections.Generic;
using System.Linq;
using static FractalMachine.Code.AST;
using FractalMachine.Classes;

namespace FractalMachine.Code.Langs
{
    public class Light : Lang
    {
        #region Static

        public static Light OpenFile(string FileName)
        {
            var text = System.IO.File.ReadAllText(FileName);
            var light = new Light();
            light.Parse(text);
            return light;
        }

        #endregion

        public AST AST;
        public OrderedAST orderedAst = null;
        public Linear Linear;

        #region Parse

        public void Parse(string Script)
        {
            ///
            /// Cycle string
            ///
            var amanuensis = new Amanuensis();

            amanuensis.Read(Script);
            AST = amanuensis.GetAST;
        }

        #endregion

        #region Implementation

        public OrderedAST GetOrderedAst()
        {
            if (orderedAst != null) return orderedAst;
            return orderedAst = new OrderedAST(AST);
        }

        public override Linear GetLinear()
        {
            return Linear = GetOrderedAst().ToLinear();
        }

        public override string Language
        {
            get { return "Light"; }
        }

        #endregion

        #region Classes

        public class Amanuensis : AST.Amanuensis
        {
            private StatusSwitcher statusSwitcher;
            private AST mainAst, curAst;
            private string strBuffer;

            private Switch isSymbol = new Switch();

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
                var trgAngularBrackets = statusDefault.Add(new Triggers.Trigger { Delimiter = "<", ActivateStatus = "inAngularBrackets" });
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

                // InAngularBrackets
                var statusInAngularBrackets = statusSwitcher.Define("inAngularBrackets");
                var trgExitAngularBrackets = statusInAngularBrackets.Add(new Triggers.Trigger { Delimiter = ">", ActivateStatus = "default" });

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
                    /*var end = curAst.Instruction.NewChild(Line, Pos, AST.Type.Instruction);
                    end.aclass = "end";*/
                    curAst.NewInstruction(Line, Pos);
                };

                trgOperators.OnTriggered = delegate (Triggers.Trigger trigger)
                {
                    if (trigger.activatorDelimiter == ":" && curAst.Instruction.MainSubject == "namespace")
                    {
                        var attr = curAst.Instruction.NewChild(Line, Pos, AST.Type.Attribute);
                        attr.subject = ":";
                        newInstructionAtNewLine = true;
                    }
                    else
                    {
                        var child = curAst.Instruction.NewChild(Line, Pos, AST.Type.Instruction);
                        child.subject = trigger.activatorDelimiter;
                        child.aclass = "operator";
                        clearBuffer();
                    }
                };

                trgFastOperation.OnTriggered = delegate (Triggers.Trigger trigger)
                {
                    var instr = curAst.Instruction.LastChild.NewInstruction(Line, Pos);
                    instr.subject = trigger.activatorDelimiter;
                    instr.aclass = "fastOperator";
                };

                trgOpenBlock.OnTriggered = delegate (Triggers.Trigger trigger)
                {
                    var child = curAst.Instruction.NewChild(Line, Pos, AST.Type.Block);
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

                    if (del == "}" && autocloseBlocks.Contains(ast.MainSubject))
                    {
                        trgNewInstruction.Trig();
                    }
                };

                trgSpace.OnTriggered = delegate
                {
                    eatBufferAndClear();
                };

                trgNewLine.OnTriggered = delegate
                {
                    if (newInstructionAtNewLine)
                    {
                        trgNewInstruction.Trig();
                        newInstructionAtNewLine = false;
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
                };

                ///
                /// Strings
                ///

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
                    var ret = eatBufferAndClear();
                    if (ret != null) ret.aclass = "string";
                };

                /// Angular bracket
                trgExitAngularBrackets.OnTriggered = delegate
                {
                    var ret = eatBufferAndClear();
                    if (ret != null) ret.aclass = "angularBracket";
                };

                ///
                /// Statuses
                ///

                char[] ExcludeSymbols = new char[] { '_' };

                statusDefault.OnCharCycle = delegate (char ch)
                {
                    var charType = new CharType(ch);
                    isSymbol.Value = ExcludeSymbols.Contains(ch) ? false : charType.CharacterType == CharType.CharTypeEnum.Symbol;
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

            bool newInstructionAtNewLine = false;

            AST eatBufferAndClear()
            {
                //todo: check if strBuffer is text
                if (!String.IsNullOrEmpty(strBuffer))
                {
                    //todo: Handle difference between Light and CPP (or create class apart)
                    if(strBuffer == "#include") // For CPP
                        newInstructionAtNewLine = true;

                    var ret = curAst.InsertAttribute(Line, Pos - strBuffer.Length, strBuffer);
                    clearBuffer();
                    return ret;
                }

                return null;
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
                for (int c = 0; c < str.Length; c++)
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

                        // Thanks this part there is no confusion between trigger with same starting chars
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

                                if (t.IsEnabled == null || t.IsEnabled.Invoke())
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
        }

        public class OrderedAST : AST.OrderedAST
        {
            internal AST ast;
            internal OrderedAST parent;
            internal List<OrderedAST> codes = new List<OrderedAST>();
            //internal List<string> attributes = new List<string>();

            internal int tempVarCount = 0, tempVar = -1;

            public OrderedAST(AST ast)
            {
                linkAst(ast);
            }

            public OrderedAST(AST ast, OrderedAST parent)
            {
                this.parent = parent;
                linkAst(ast);
            }

            public void Revision()
            {
                // cosa farci?
            }

            void linkAst(AST ast)
            {
                this.ast = ast;

                foreach (var child in ast.children)
                {
                    codes.Add(new OrderedAST(child, this));
                }

                Revision();
            }

            internal int getTempNum()
            {
                var num = tempVarCount;
                var par = parent;

                while (par != null)
                {
                    num += par.tempVarCount;
                    par = par.parent;
                }

                tempVarCount++;
                return num;
            }

            internal int getTempVar()
            {
                if (tempVar == -1)
                    tempVar = getTempNum();
                return tempVar;
            }

            internal OrderedAST newClassCode(AST ast)
            {
                var cc = new OrderedAST(ast);
                cc.parent = this;
                return cc;
            }

            #region Properties

            internal bool IsInFunctionParenthesis
            {
                get
                {
                    bool parenthesis = false;
                    var a = this;
                    while (a != null && !a.HasFunction)
                    {
                        parenthesis = a.IsBlockParenthesis || parenthesis;
                        a = a.parent;
                    }

                    return a != null && a.HasFunction && parenthesis;
                }
            }

            internal OrderedAST Left
            {
                get
                {
                    var pos = parent.codes.IndexOf(this);
                    if (pos > 0)
                        return parent.codes[pos - 1];
                    else
                        return null;
                    
                }
            }

            public bool HasFunction
            {
                get
                {
                    foreach (var code in codes)
                    {
                        if (code.IsBlockParenthesis)
                            return true;
                    }

                    return false;
                }
            }

            public bool HasFinalBracketsBlock
            {
                get
                {
                    var last = LastCode;
                    if (last != null)
                        return last.IsBlockBrackets;

                    return false;
                }
            }

            public bool IsDeclaration
            {
                get
                {
                    return !IsOperator && CountAttributes >= 2 && !Properties.Statements.Contains(codes[0].Subject);
                }
            }

            public int CountAttributes
            {
                get
                {
                    int i = 0;

                    foreach (var code in codes)
                    {
                        if (code.ast.type == AST.Type.Attribute)
                            i++;
                    }

                    return i;
                }
            }

            public OrderedAST LastCode
            {
                get
                {
                    var cnt = codes.Count;
                    if (cnt > 0)
                        return codes[cnt - 1];

                    return null;
                }
            }

            public bool IsInstructionsContainer
            {
                get
                {
                    return parent == null || IsBlockBrackets;
                }
            }

            public bool IsOperator
            {
                get
                {
                    return ast.aclass == "operator" || ast.aclass == "fastOperator";
                }
            }

            public bool IsInstructionFree
            {
                get
                {
                    return ast.type == AST.Type.Instruction && String.IsNullOrEmpty(ast.aclass);
                }
            }

            public bool IsBlockParenthesis
            {
                get
                {
                    return ast.type == AST.Type.Block && ast.subject == "(";
                }
            }

            public bool IsBlockBrackets
            {
                get
                {
                    return ast.type == AST.Type.Block && Subject == "{";
                }
            }

            public bool IsAssign
            {
                get
                {
                    return IsOperator && Subject == "=";
                }
            }

            internal string Subject
            {
                get
                {
                    return ast.subject;
                }
            }

            internal OrderedAST TopFunction
            {
                get
                {
                    var a = this;
                    while (a != null && !a.HasFunction)
                    {
                        a = a.parent;
                    }

                    return a;
                }
            }

            internal OrderedAST TopOfSeries
            {
                get
                {
                    var subj = Subject;
                    var oa = parent;
                    while (oa.Subject == subj)
                    {
                        oa = oa.parent;
                    }

                    return oa;
                }
            }

            #endregion

            #region ToLinear

            /*
              _______    _      _                       
             |__   __|  | |    (_)                      
                | | ___ | |     _ _ __   ___  __ _ _ __ 
                | |/ _ \| |    | | '_ \ / _ \/ _` | '__|
                | | (_) | |____| | | | |  __/ (_| | |   
                |_|\___/|______|_|_| |_|\___|\__,_|_| 
            */

            delegate void OnCallback();
            delegate void OnOperation(Bag bag, OrderedAST ast);
            delegate void OnOperationInt(int val);

            class Bag
            {
                public Status status;
                public Linear Linear, _lin;
                public Bag Parent;
                public List<string> Params = new List<string>();
                public Dictionary<string, string> Dict = new Dictionary<string, string>();

                public OnCallback OnNextParamOnce;
                public OnOperation Operation, OnNextParam;
                public OnOperationInt setTempReturn;

                internal bool disableStatementDecoder = false;

                int linsStackPos = 0;
                List<Linear> linsStack = new List<Linear>();

                public Bag()
                {
                    ///
                    /// Callbacks
                    ///
                    setTempReturn = delegate (int val)
                    {
                        lin.Return = "$" + val;
                        addParam("$" + val);
                    };

                }

                public enum Status
                {
                    Ground,
                    DeclarationParenthesis
                }

                #region Lin

                public Linear lin
                {
                    get
                    {
                        return _lin;
                    }

                    set
                    {
                        linsStack.Add(value);
                        _lin = value;
                    }
                }

                public void catchLin(Linear lin = null)
                {
                    if (lin == null)
                        lin = _lin;

                    for (int l = linsStack.Count - 1; l >= linsStackPos; l--)
                    {
                        var ll = linsStack[l];

                        if (ll == lin)
                            break;

                        if (ll.Op != null)
                            lin.Add(linsStack[l]);

                        linsStack.RemoveAt(l);
                    }
                }

                public void addLin(AST ast)
                {
                    lin = new Linear(ast);
                    linsStackPos++;
                }

                public void backLin()
                {
                    linsStackPos--;

                    if (linsStack[linsStackPos].Op == null)
                        linsStack.RemoveAt(linsStackPos);

                    if (linsStackPos > 0)
                        _lin = linsStack[linsStackPos - 1];
                    else
                        _lin = null;
                }

                #endregion

                #region Param
                public void addParam(string p)
                {
                    Params.Add(p);
                }

                public string pullParams(bool withoutRemove = false)
                {
                    var c = Params.Count - 1;

                    if (c < 0)
                        return null;

                    string s = Params[c];
                    if (!withoutRemove) Params.RemoveAt(c);
                    return s;
                }
                #endregion

                public Bag subBag(Status status = Status.Ground)
                {
                    var b = new Bag();
                    b.Parent = this;
                    b.status = status;
                    b.Linear = Linear;
                    return b;
                }
            }

            public Linear ToLinear()
            {
                var bag = new Bag();
                bag.Linear = new Linear(ast);
                toLinear(bag);
                return bag.Linear;
            }

            void toLinear(Bag bag)
            {
                //if (outLin == null) outLin = new Linear(oAst.ast);

                Linear lin = null;
                OnCallback onEnd = null;
                bool enter = false;

                ///
                /// Callbacks
                ///
                OnCallback setTempReturn = delegate
                {
                    lin.Return = "$" + getTempVar();
                    bag.addParam("$" + getTempVar());
                };

                OnCallback onSquareBrackets = delegate
                {
                    if (Subject == "[")
                    {
                        if (Left.ast.type == AST.Type.Attribute)
                        {
                            //todo: Pensare ad un metodo migliore...
                            bag.addParam(bag.pullParams() + "[]");
                        }
                    }
                };

                ///
                /// Analyze AST
                ///
                if (ast.type == AST.Type.Attribute)
                {
                    if (ast.aclass == "string")
                        ast.subject = Properties.StringMark + Subject;
                    if (ast.aclass == "angularBracket")
                        ast.subject = Properties.AngularBracketsMark + Subject;

                    bag.addParam(Subject);

                    bag.OnNextParamOnce?.Invoke();
                    bag.OnNextParamOnce = null;

                    bag.OnNextParam?.Invoke(bag, this);

                    return;
                }

                if (bag.status == Bag.Status.Ground)
                {
                    if (ast.type == AST.Type.Block)
                    {
                        onSquareBrackets();

                        if (Subject == "(")
                        {
                            if (!parent.IsDeclaration)
                            {
                                ///
                                /// Here parameters are simply collected from bag.Paramas
                                ///
                                lin = new Linear(bag.Linear, ast);
                                lin.Op = "call";
                                lin.Name = bag.pullParams();

                                bag = bag.subBag();
                                bag.disableStatementDecoder = true;
                              
                                onEnd = delegate
                                {
                                    for (int l = bag.Params.Count - 1; l >= 0; l--)
                                    {
                                        Linear lin = new Linear(bag.Linear, ast);
                                        lin.Op = "push";
                                        lin.Name = bag.Params.Pull(0);
                                        lin.List();
                                    }

                                    setTempReturn();
                                };
                            }
                            else
                            {
                                ///
                                /// This is a good example of different status management
                                ///
                                bag = bag.subBag(Bag.Status.DeclarationParenthesis);
                                var l = bag.Linear = bag.Linear.NewSetting(ast);
                                l.Op = "parameters";

                                bag.Operation = delegate (Bag bag, OrderedAST oAst)
                                {
                                    var p = bag.pullParams();
                                    if (p != null)
                                    {
                                        var lin = new Linear(l, oAst.ast);
                                        lin.Op = "parameter";
                                        lin.Name = p;
                                        lin.List();

                                        if (bag.Params.Count > 0)
                                            lin.Parameters.Add("type", bag.pullParams());
                                    }
                                };

                                onEnd = delegate
                                {
                                    bag.Operation(bag, this);
                                };
                            }
                        }

                        if(Subject == "{")
                            bag = bag.subBag();
                    }
                    else
                    {
                        if (Subject == null && codes.Count == 0)
                            return; // Is a dummy instruction

                        if (IsDeclaration)
                        {
                            lin = new Linear(bag.Linear, ast);
                            if (HasFinalBracketsBlock)
                                lin.Op = "function";
                            else
                                lin.Op = "declare";
                            enter = true;

                            onEnd = delegate
                            {
                                

                                lin.Name = bag.pullParams();
                                lin.Return = bag.pullParams();
                                lin.Attributes = bag.Params;
                                bag.Params = new List<string>();
                            };
                        }
                        else if (IsOperator)
                        {
                            ///
                            /// Operators
                            ///

                            switch (Subject)
                            {
                                case "=":
                                    lin = new Linear(bag.Linear, ast);
                                    lin.Op = Subject;
                                    lin.Name = bag.pullParams(true);

                                    onEnd = delegate
                                    {
                                        lin.Attributes.Add(bag.pullParams());
                                    };

                                    break;

                                case ".":

                                    bag.OnNextParamOnce = delegate
                                    {
                                        var p = bag.pullParams();
                                        bag.addParam(bag.pullParams() + "." + p);
                                    };

                                    break;

                                case ",":
                                    // Repeat previous operation
                                    bag.Operation?.Invoke(bag, this);

                                    break;

                                default:
                                    lin = new Linear(bag.Linear, ast);
                                    lin.Op = Subject;
                                    lin.Attributes.Add(bag.pullParams());
                                    setTempReturn();

                                    onEnd = delegate
                                    {
                                        lin.Attributes.Add(bag.pullParams());
                                    };

                                    break;
                            }

                        }
                        else
                        {
                            onSquareBrackets();

                            ///
                            /// Statement decoder could cause a statement confusion
                            ///
                            onEnd = delegate
                            {
                                var pars = bag.Params;

                                if (!bag.disableStatementDecoder && pars.Count > 0)
                                {
                                    lin = new Linear(bag.Linear, ast);
                                    lin.Op = pars.Pull(0);
                                    lin.Attributes.Add(pars.Pull(0));

                                    //todo: handle types
                                    string prev = "";
                                    int i = 0;
                                    while (pars.Count > 0)
                                    {
                                        if (i % 2 == 1)
                                            lin.Parameters.Add(prev, pars.Pull(0));
                                        else
                                            prev = pars.Pull(0);

                                        i++;
                                    }
                                }

                            };
                        }
                    }
                }
                else if (bag.status == Bag.Status.DeclarationParenthesis)
                {
                    onSquareBrackets();

                    if (IsOperator)
                    {
                        switch (Subject)
                        {
                            case ",":
                                bag.Operation?.Invoke(bag, this);
                                break;

                            case "=":

                                onEnd = delegate
                                {
                                    bag.Linear.LastInstruction.Return = bag.pullParams();
                                };

                                break;
                        }
                    }
                }

                if (enter)
                {
                    bag.Linear = lin;
                }


                ///
                /// Child analyzing
                ///

                foreach (var code in codes)
                {
                    // todo: try catch for error checking(?)
                    code.toLinear(bag);
                }

                ///
                /// Exit
                ///

                if (onEnd != null)
                    onEnd();

                if (lin != null)
                    lin.List();

                if (enter)
                    bag.Linear = bag.Linear.parent;

            }


            #endregion
        }

        #endregion
    }
}
