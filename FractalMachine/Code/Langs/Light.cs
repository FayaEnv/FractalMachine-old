using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static FractalMachine.Code.AST;
using FractalMachine.Classes;
using System.Net.NetworkInformation;

namespace FractalMachine.Code.Langs
{
    public class Light : Lang
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
                    var child = curAst.Instruction.NewChild(Line, Pos, AST.Type.Instruction);
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
                    var ret = eatBufferAndClear();
                    if(ret != null) ret.aclass = "string";
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

            AST eatBufferAndClear()
            {
                //todo: check if strBuffer is text
                if (!String.IsNullOrEmpty(strBuffer))
                {
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

                foreach(var child in ast.children)
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

            public bool HasFunction
            {
                get
                {
                    foreach(var code in codes)
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
                    if(last != null)
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

                    foreach(var code in codes)
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
                    while(oa.Subject == subj)
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

                public OnCallback  OnNextParamOnce;
                public OnOperation Operation, OnNextParam;
                public OnOperationInt setTempReturn;

                int linsStackPos = 0;
                List<Linear> linsStack = new List<Linear>();

                public Bag()
                {
                    ///
                    /// Callbacks
                    ///
                    setTempReturn = delegate(int val)
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

                    for (int l = linsStack.Count-1; l >= linsStackPos; l--)
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
                    if(!withoutRemove) Params.RemoveAt(c);
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

                ///
                /// Analyze AST
                ///
                if (ast.type == AST.Type.Attribute)
                {
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
                        if (Subject == "[")
                        {
                            if (parent.IsDeclaration)
                            {
                                //todo: Pensare ad un metodo migliore...
                                bag.addParam(bag.pullParams() + "[]");
                            }
                        }

                        if (Subject == "(")
                        {
                            if (!parent.IsDeclaration)
                            {
                                lin = new Linear(bag.Linear, ast);
                                lin.Op = "call";
                                lin.Name = bag.pullParams();

                                bag.Operation = delegate
                                {
                                    var p = bag.pullParams();
                                    if (p != null)
                                    {
                                        Linear lin = new Linear(bag.Linear, ast);
                                        lin.Op = "push";
                                        lin.Attributes.Add(bag.pullParams());
                                        lin.List();
                                    }
                                };

                                onEnd = delegate
                                {
                                    bag.Operation(bag, this);
                                    setTempReturn();
                                };
                            }
                            else
                            {
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
                                    }
                                };

                                onEnd = delegate
                                {
                                    bag.Operation(bag, this);
                                };
                            }
                        }

                        if (Subject == "{")
                        {
                            bag = bag.subBag();
                        }
                    }
                    else
                    {
                        if (Subject == null && codes.Count == 0)
                            return; // Is a dummy instruction

                        if (IsDeclaration)
                        {
                            lin = new Linear(bag.Linear, ast);
                            lin.Op = "declare";
                            enter = true;

                            onEnd = delegate
                            {
                                lin.Name = bag.pullParams();
                                lin.Attributes = bag.Params;
                                bag.Params = new List<string>();
                            };
                        }
                        else if (IsOperator)
                        {
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
                            onEnd = delegate
                            {
                                var pars = bag.Params;

                                lin = new Linear(bag.Linear, ast);
                                lin.Op = Extensions.Pull(pars, 0);
                                lin.Attributes.Add(Extensions.Pull(pars, 0));

                                string prev = "";
                                int i = 0;
                                while (pars.Count > 0)
                                {
                                    if (i % 2 == 1)
                                        lin.Parameters.Add(prev, Extensions.Pull(pars, 0));
                                    else
                                        prev = Extensions.Pull(pars, 0);

                                    i++;
                                }

                            };
                        }
                    }
                }
                else if (bag.status == Bag.Status.DeclarationParenthesis)
                {
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

           

                /*void injectDeclareFunctionParameters(Bag bag)
                {
                    var sett = lin.NewSetting(ast);
                    sett.Op = "parameters";

                    var oa = codes[0];
                    Linear l = new Linear(sett, oa.ast);
                    l.List();

                    while (oa != null)
                    {
                        var sBag = bag.subBag();
                        orderedAstToLinear(sBag);

                        if (oa.Subject == ",")
                        {
                            l = new Linear(sett, oa.ast);
                            l.List();
                        }

                        switch (oa.Subject)
                        {
                            case "=":
                                // todo: creare settings al posto che attributes (oppure creare dictionary)
                                l.Attributes = oa.attributes;
                                break;

                            default:
                                // check for better modes
                                if(oa.attributes.Count > 0)
                                    l.Name = oa.attributes[0];
                                break;
                        }

                        if (oa.codes.Count == 1)
                            oa = oa.codes[0];
                        else
                            oa = null;
                    }

                }*/

                #endregion
            }

        #endregion
    }
}
