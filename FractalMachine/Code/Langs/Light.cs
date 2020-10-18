using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static FractalMachine.Code.AST;
using FractalMachine.Classes;

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
        public OrderedAst orderedAst = null;
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

        public override AST.OrderedAst GetOrderedAst()
        {
            if (orderedAst != null) return orderedAst;
            return orderedAst = OrderedAst.FromAST(AST);
        }

        public override Linear GetLinear()
        {
            return OrderedAst.ToLinear((OrderedAst)GetOrderedAst());
        }

        #endregion

        #region Classes

        public class Amanuensis : AST.Amanuensis
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
                if (!String.IsNullOrEmpty(strBuffer))
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

        public class OrderedAst : AST.OrderedAst
        {
            internal AST ast;
            internal OrderedAst parent;
            internal List<OrderedAst> codes = new List<OrderedAst>();
            internal List<string> attributes = new List<string>();

            internal int tempVarCount = 0, tempVar = -1;

            internal string declarationType = "";
            internal int nAttrs;
            internal bool isFunction = false;

            internal Linear lin;

            internal bool isBlockParenthesis = false;
            internal bool isDeclaration = false;
            internal bool isBlockDeclaration = false;
            internal bool isComma = false;

            public OrderedAst(AST ast)
            {
                linkAst(ast);
            }

            public OrderedAst NewChildFromAst(AST ast)
            {
                var cc = newClassCode(ast);
                codes.Add(cc);

                if (ast.IsBlockParenthesis)
                {
                    if (this.ast.IsInstructionFree || this.ast.subject == ".")
                        isFunction = true;
                }

                return cc;
            }

            public void Revision()
            {
                if (ast != null)
                {
                    isBlockParenthesis = ast.IsBlockParenthesis;
                }

                // Check attributes
                nAttrs = attributes.Count;
                if (nAttrs >= 2)
                {
                    declarationType = attributes[nAttrs - 2];
                    if (Properties.DeclarationTypes.Contains(declarationType))
                        isDeclaration = true;
                }

                // Check subcodes
                var ncodes = codes.Count;
                if (ncodes >= 1)
                {
                    var last = codes[ncodes - 1];
                    bool hasLastBlockBrackets = last.ast?.IsBlockBrackets ?? false;
                    isBlockDeclaration = hasLastBlockBrackets && isDeclaration;
                }

                foreach (var c in codes)
                    c.Revision();
            }

            void linkAst(AST ast)
            {
                this.ast = ast;
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

            public void ReadProperty(string Property)
            {
                attributes.Add(Property);
            }

            internal OrderedAst newClassCode(AST ast)
            {
                var cc = new OrderedAst(ast);
                cc.parent = this;
                return cc;
            }

            internal bool IsInFunctionParenthesis
            {
                get
                {
                    bool parenthesis = false;
                    var a = this;
                    while (a != null && !a.isFunction)
                    {
                        parenthesis = a.ast?.IsBlockParenthesis ?? false || parenthesis;
                        a = a.parent;
                    }

                    return a != null && a.isFunction && parenthesis;
                }
            }

            internal string Subject
            {
                get
                {
                    return ast.subject;
                }
            }

            internal OrderedAst TopFunction
            {
                get
                {
                    var a = this;
                    while (a != null && !a.isFunction)
                    {
                        a = a.parent;
                    }

                    return a;
                }
            }

            #region FromAST

            static OrderedAst orderedAst;

            public static OrderedAst FromAST(AST ast)
            {
                orderedAst = new OrderedAst(ast);
                readAst(ast);
                orderedAst.Revision();
                return orderedAst;
            }

            static void readAst(AST ast)
            {
                switch (ast.type)
                {
                    case AST.Type.Attribute:
                        readAstAttribute(ast);
                        break;

                    default:
                        readAstBlockOrInstruction(ast);
                        break;
                }
            }

            static void readAstBlockOrInstruction(AST ast)
            {
                orderedAst = orderedAst.NewChildFromAst(ast);

                foreach (var child in ast.children)
                {
                    readAst(child);
                }

                orderedAst = orderedAst.parent;
            }

            static void readAstAttribute(AST ast)
            {
                orderedAst.ReadProperty(ast.subject);
            }


            #endregion

            #region ToLinear

            delegate void OnCallback();
            delegate void OnAddAttribute(string Attribute);

            static Linear outLin;
            static List<string> Params = new List<string>();

            public static Linear ToLinear(OrderedAst oAst)
            {
                orderedAstToLinear(oAst);
                return outLin;
            }

            static void orderedAstToLinear(OrderedAst oAst)
            {
                if (outLin == null)
                    outLin = new Linear(oAst);

                Linear lin = null;
                OnCallback onEnd = null;

                bool enter = false;
                bool recordAttributes = true;

                OnAddAttribute onAddAttribute = delegate (string attr)
                {
                    Params.Add(attr);
                };

                if (oAst.isDeclaration)
                {
                    if (oAst.isBlockDeclaration)
                    {
                        lin = new Linear(outLin, oAst);
                        lin.Op = oAst.declarationType;
                        lin.Name = Extensions.Pull(oAst.attributes);
                        lin.Attributes = oAst.attributes;

                        if (oAst.isFunction)
                        {
                            // Read parenthesis arguments
                            var parenthesis = Extensions.Pull(oAst.codes, 0);
                            injectDeclareFunctionParameters(lin, parenthesis);
                        }

                        outLin = lin;
                        enter = true;
                    }
                    else
                    {
                        lin = new Linear(outLin, oAst);
                        lin.List();
                        lin.Op = "declare";
                        lin.Name = oAst.attributes[oAst.nAttrs - 1];
                        lin.Attributes = oAst.attributes;
                        Params.Add(lin.Name);
                    }
                }
                else
                {
                    if (oAst.ast.IsOperator)
                    {
                        switch (oAst.Subject)
                        {
                            case "=":
                                lin = new Linear(outLin, oAst);
                                lin.Op = oAst.Subject;
                                lin.Attributes.Add(pullParams());

                                onEnd = delegate
                                {
                                    lin.Attributes.Add(pullParams());
                                };

                                break;

                            case ".":
                                if (oAst.parent.Subject != ".")
                                    oAst.attributes[0] = pullParams() + "." + oAst.attributes[0];
                                else
                                    oAst.attributes[0] = oAst.parent.attributes[0] + "." + oAst.attributes[0];

                                recordAttributes = false;

                                break;

                            default:
                                lin = new Linear(outLin, oAst);
                                lin.Op = oAst.Subject;
                                lin.Attributes.Add(pullParams());
                                lin.Assign = "$" + oAst.getTempVar();
                                Params.Add("$" + oAst.getTempVar());

                                onEnd = delegate
                                {
                                    lin.Attributes.Add(pullParams());
                                };

                                break;
                        }

                    }

                    if (oAst.ast.IsBlockParenthesis)
                    {
                        if (oAst.IsInFunctionParenthesis)
                        {
                            if (oAst.TopFunction.isDeclaration)
                            {
                                // bisognerebbe poter accedere alla lin della funzione...
                            }
                            else
                            {
                                onEnd = delegate ()
                                {
                                    lin = new Linear(outLin, oAst);
                                    lin.Op = "push";
                                    lin.Attributes.Add(pullParams());
                                };
                            }
                        }
                    }

                    if (oAst.isFunction)
                    {
                        lin = new Linear(outLin, oAst);
                        lin.Op = "call";
                        lin.Name = oAst.attributes[0];
                    }
                }

                if (enter)
                {
                    outLin = lin;
                }

                // Prepare attributes
                if (recordAttributes)
                {
                    foreach (var s in oAst.attributes)
                    {
                        onAddAttribute(s);
                    }
                }

                ///
                /// Child analyzing
                ///

                foreach (var code in oAst.codes)
                {
                    orderedAstToLinear(code);
                }


                ///
                /// Exit
                ///

                if (onEnd != null)
                    onEnd();

                if (lin != null)
                    lin.List();

                if (enter)
                {
                    outLin = outLin.parent;
                }
            }

            static void injectDeclareFunctionParameters(Linear lin, OrderedAst oAst)
            {
                var sett = lin.NewSetting(oAst);
                sett.Op = "parameters";

                var oa = oAst.codes[0];
                Linear l = new Linear(sett, oa);
                l.List();

                while (oa != null)
                {
                    if (oa.Subject == ",")
                    {
                        l = new Linear(sett, oa);
                        l.List();
                    }

                    switch (oa.Subject)
                    {
                        case "=":
                            // todo: creare settings al posto che attributes (oppure creare dictionary)
                            l.Attributes = oa.attributes;
                            break;

                        default:
                            l.Name = oa.attributes[0];
                            break;
                    }

                    if (oa.codes.Count == 1)
                        oa = oa.codes[0];
                    else
                        oa = null;
                }

            }

            static string pullParams()
            {
                var c = Params.Count - 1;

                if (c < 0)
                    return "tolinParams EMPTY";

                string s = Params[c];
                Params.RemoveAt(c);
                return s;
            }

            #endregion
        }

        #endregion
    }
}
