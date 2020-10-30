/*
   Copyright 2020 (c) Riccardo Cecchini
   
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using static FractalMachine.Code.AST;
using FractalMachine.Classes;
using System.Transactions;

namespace FractalMachine.Code.Langs
{
    public class Light : Lang
    {
        #region Static

        public static string[] Statements = new string[] { "import", "namespace", "#include" };
        public static string[] ContinuousStatements = new string[] { "namespace", "private", "public" };
        public static string[] Modifiers = new string[] { "private", "public", "protected" };
        public static string[] DeclarationOperations = new string[] { "declaration", "function" };
        public static string[] CodeBlocks = new string[] { "if", "else" };
        public static string[] CodeBlocksWithoutParameters = new string[] { "else" };

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

        public override Language Language
        {
            get { return Language.Light; }
        }

        #endregion

        #region Classes

        public class Amanuensis : AST.Amanuensis
        {
            private StatusSwitcher statusSwitcher;
            private AST mainAst, curAst;
            private string strBuffer;

            private Switch isSymbol = new Switch();
       
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
                var trgNewInstruction = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { ";", "," } }); // technically the , is a new instruction
                var trgNewLine = statusDefault.Add(new Triggers.Trigger { Delimiter = "\n" });
                var trgOperators = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { "==", "!=", "=", "+", "-", "/", "%", "*", "&&", "||", "&", "|", ":" } });
                var trgFastOperation = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { "++", "--" } });
                var trgInsert = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { "." } }); // Add to buffer without any new instruction

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

                trgNewInstruction.OnTriggered = delegate (Triggers.Trigger trigger)
                {
                    /*var end = curAst.Instruction.NewChild(Line, Pos, AST.Type.Instruction);
                    end.aclass = "end";*/

                    var ast = curAst.NewInstruction(Line, Pos);

                    if (trigger.activatorDelimiter == ",")
                        ast.subject = ",";
                };

                trgInsert.OnTriggered = delegate (Triggers.Trigger trigger)
                {
                    appendBuffer = true;
                    strBuffer += trigger.activatorDelimiter;
                };

                trgOperators.OnTriggered = delegate (Triggers.Trigger trigger)
                {
                    // Check for continuous statement
                    if (trigger.activatorDelimiter == ":" && Light.ContinuousStatements.Contains(curAst.Instruction.MainSubject))
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

            // Fast switches flags
            bool newInstructionAtNewLine = false;
            bool appendBuffer = false;
            AST eatBufferAndClear()
            {
                //todo: check if strBuffer is text (few months later: boh(??))
                if (!String.IsNullOrEmpty(strBuffer))
                {
                    AST ret;
                    if (appendBuffer)
                    {
                        ret = curAst.AppendToLastAttribute(strBuffer);
                        appendBuffer = false;
                    }
                    else
                    {
                        //todo: Handle difference between Light and CPP (or create class apart)
                        if (strBuffer == "#include") // For CPP
                            newInstructionAtNewLine = true;

                        ret = curAst.InsertAttribute(Line, Pos - strBuffer.Length, strBuffer);
                    }

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

            internal OrderedAST prev, next;
            internal int tempVarCount = 0, tempVar = -1;

            public OrderedAST(AST ast)
            {
                linkAst(ast);
            }

            public OrderedAST(AST ast, OrderedAST parent)
            {
                this.parent = parent;
                parent.codes.Add(this);
                linkAst(ast);
            }

            public void Revision()
            {
                if (IsOperator)
                {
                    if (Subject != "=")
                    {
                        // is short operation
                        if (codes.Count == 1 && LastCode.Subject == "=")
                        {
                            var lc = LastCode;
                            codes = LastCode.codes;
                            lc.codes = new List<OrderedAST>();
                            new OrderedAST(parent.ast.children[0], lc);
                            lc.codes.Add(this);
                            parent.codes[parent.codes.Count - 1] = lc;
                            // it's a little twisted
                            // ex: test += 2
                            // +.codes = 2
                            // =.codes.Add(test)
                            // =.codes.Add(+)
                            // test last child (+) substituted with =
                        }
                    }
                }
            }

            void linkAst(AST ast)
            {
                this.ast = ast;

                OrderedAST previous = null;
                foreach (var child in ast.children)
                {
                    var ch = new OrderedAST(child, this);
                    if(previous != null) previous.next = ch;
                    ch.prev = previous;
                    previous = ch;
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

            #region Count

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

            public int CountAttributesOnRight
            {
                get
                {
                    int count = 0;
                    var cur = Right;
                    while(cur != null)
                    {
                        if (cur.IsAttribute)
                            count++;

                        if (parent.HasAccumulatedAttributes && cur == LastBlockBrackets)
                            count++;

                        cur = cur.Right;
                    }

                    return count;
                }
            }

            #endregion

            #region Has

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

            public bool HasAccumulatedAttributes
            {
                get
                {
                    return LastBlockBrackets?.IsAccumulator ?? false;
                }
            }

            #endregion

            #region IndexNavigation

            internal OrderedAST Left
            {
                get
                {
                    return prev;
                }
            }

            internal OrderedAST Right
            {
                get
                {
                    return next;
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

            public int IndexLastBrackets
            {
                get
                {
                    for(int c=codes.Count-1; c>=0; c--)
                    {
                        if (codes[c].IsBlockBrackets)
                            return c;
                    }
                    return -1;
                }
            }

            public int IndexLastSquareBrackets
            {
                get
                {
                    for (int c = codes.Count - 1; c >= 0; c--)
                    {
                        if (codes[c].IsBlockSquareBrackets)
                            return c;
                    }
                    return -1;
                }
            }

            #endregion

            #region Is

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

            public bool IsAccumulator
            {
                get
                {
                    //todo: there is another type of accumulator: the SquareBrackets accumulato
                    // could be found if it has an operator in front of it
                    if (ast.type != AST.Type.Block && ast.subject != "(")
                        return false;

                    foreach(var code in codes)
                    {
                        var lc = code.LastCode;
                        //todo study: in what cases lc == null?
                        if (lc == null || lc.IsOperator && lc.Subject == ":") //Is JSON property
                            return false;
                    }

                    return true;
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

            public bool IsBlockSquareBrackets
            {
                get
                {
                    return ast.type == AST.Type.Block && Subject == "[";
                }
            }

            public bool IsMainBlock
            {
                get
                {
                    return IsBlockBrackets; //todo && not in operation
                }
            }

            public bool IsAssign
            {
                get
                {
                    return IsOperator && Subject == "=";
                }
            }

            public bool IsRepeatedInstruction
            {
                get
                {
                    return ast.type == AST.Type.Instruction && Subject == ",";
                }
            }

            public bool IsAttribute
            {
                get
                {
                    return ast.type == AST.Type.Attribute;
                }
            }

            public bool IsDeclaration
            {
                get
                {
                    return !IsOperator && (CountAttributes >= 2 || (CountAttributes >= 1 && HasAccumulatedAttributes)) && !Statements.Contains(codes[0].Subject);
                }
            }

            public bool IsBlockDeclaration
            {
                get
                {
                    var cc = codes.Count;
                    if (cc < 2) return false;
                    return codes[cc - 1].IsBlockBrackets && (codes[cc - 2].IsBlockParenthesis || IsCodeBlockWithoutParameters) && IsDeclaration;
                }
            }

            public bool IsCodeBlockWithoutParameters
            {
                get
                {
                    return CodeBlocksWithoutParameters.Contains(prev?.Subject ?? ""); // or a Contains could accept a null value?
                }
            }

            public bool IsCallBlock
            {
                get
                {
                    return !IsAccumulator && ast.type == AST.Type.Block && ast.subject != "{";
                }
            }

            #endregion

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

            internal OrderedAST LastAttribute
            {
                get
                {
                    var c = LastCode;
                    while(c != null)
                    {
                        if (c.ast.type == AST.Type.Attribute)
                            return c;
                        c = c.Left;
                    }
                    return null;
                }
            }

            internal OrderedAST LastBlockBrackets
            {
                get
                {
                    var c = LastCode;
                    while (c != null)
                    {
                        if (c.IsBlockBrackets)
                            return c;
                        c = c.Left;
                    }
                    return null;
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

            internal delegate void OnCallback();
            internal delegate void OnOperation(Bag bag, OrderedAST ast);
            internal delegate void OnOperationInt(int val);

            #region Parameters

            internal class Parameter
            {
                string strValue;
                List<string> values;

                public Parameter(string value)
                {
                    strValue = value;
                }

                public Parameter(List<string> values)
                {
                    this.values = values;
                }

                public Parameter(Parameters parameters)
                {
                    //inherit values
                    values = parameters.ToStringList();
                }

                public string StrValue
                {
                    get
                    {
                        if (values != null)
                            throw new Exception("oops");
                        return strValue;
                    }
                }

                public string[] Values
                {
                    get
                    {
                        if (!String.IsNullOrEmpty(strValue))
                            return new string[] { strValue };
                        return values.ToArray() ?? new string[] { };
                    }
                }
            }

            internal class Parameters : List<Parameter>
            {
                Bag bag;
                public Parameters(Bag bag):base()
                {
                    // you shot me down, bag bag
                    this.bag = bag;
                }

                public void New (Parameter par)
                {
                    bag.posNewParam = Count;
                    Add(par);                    
                }

                public string[] ToStringArray()
                {
                    var strings = new string[Count];
                    int s = 0;
                    foreach(var par in this)
                    {
                        strings[s++] = par.StrValue;
                    }
                    return strings;
                }

                public List<string> ToStringList()
                {
                    var strings = new List<string>();
                    foreach (var par in this)
                    {
                        strings.Add(par.StrValue);
                    }
                    return strings;
                }

                public Parameter LastParam
                {
                    get
                    {
                        if (Count == 0) return null;
                        return this[Count - 1];
                    }
                }
   
            }

            #endregion

            internal class Bag
            {
                public Status status;
                public Linear Linear;
                public Bag Parent;
                public Parameters Params;
                public Dictionary<string, string> Dict = new Dictionary<string, string>();
                internal int posNewParam = -1;
                public Statement statement;

                public OnCallback OnNextParamOnce;
                public OnOperation OnRepeteable;
                //public OnOperationInt setTempReturn;

                internal bool disableStatementDecoder = false;

                public Bag()
                {
                    Params = new Parameters(this);
                }

                public enum Status
                {
                    Ground,
                    DeclarationParenthesis,
                    JSON //todo
                }


                #region Param

                public bool HasNewParam
                {
                    get
                    {
                        return posNewParam < Params.Count && posNewParam >= 0;
                    }
                }

                public void NewParam(string p)
                {
                    Params.New(new Parameter(p));
                }

                #endregion

                public void EnterLastLinear()
                {
                    Linear = Linear.LastInstruction;
                }

                public Bag subBag(Status status = Status.Ground)
                {
                    var b = new Bag();
                    b.Parent = this;
                    b.status = status;
                    b.Linear = Linear;
                    b.statement = statement;
                    //b.OnRepeteable = OnRepeteable; //Repeteable should works at the same level
                    return b;
                }
            }

            public Linear ToLinear()
            {
                var bag = new Bag();
                bag.Linear = new Linear(ast);
                toLinear(bag);
                Revision(bag.Linear);
                return bag.Linear;
            }

            #region Revision

            /// <summary>
            /// This is used for calculate namespace and modifiers
            /// </summary>
            /// <param name="lin"></param>
            void Revision(Linear lin)
            {
                // Check for continuous
                RevisionContinuousNamespace(lin);
                RevisionContinuousModifiers(lin);
                //todo RevisionInternalOperation for declare assignation
            }

            void RevisionContinuousNamespace(Linear lin)
            {
                Linear continuous = null;

                for (int i = 0; i < lin.Instructions.Count; i++)
                {
                    var instr = lin.Instructions[i];

                    if (instr.Op == "namespace" && instr.Continuous)
                    {
                        continuous = instr;
                        instr.Continuous = false;
                    }
                    else
                    {
                        if (continuous != null)
                        {
                            continuous.Instructions.Add(instr);
                            instr.parent = continuous;
                            lin.Instructions.RemoveAt(i--);
                        }
                    }
                }
            }

            void RevisionContinuousModifiers(Linear lin)
            {
                Linear continuous = null;

                for (int i = 0; i < lin.Instructions.Count; i++)
                {
                    var instr = lin.Instructions[i];

                    if (Light.Modifiers.Contains(instr.Op) && instr.Continuous)
                    {
                        continuous = instr;
                        lin.Instructions.RemoveAt(i--);
                    }
                    else
                    {
                        if (continuous != null && Light.DeclarationOperations.Contains(instr.Op))
                        {
                            //todo: check if instr has yet a modifier (?)
                            instr.Attributes.Add(continuous.Op);
                        }
                    }
                }
            }

            #endregion

            Bag bag;
            Linear lin = null;  
            OnCallback onEnd = null;
            OnScheduler onSchedulerPostCode = null;
            bool enter = false;

            void setTempReturn()
            {
                lin.Return = Properties.InternalVariable + getTempVar();
                bag.NewParam(Properties.InternalVariable + getTempVar());
            }

            void toLinear(Bag parentBag)
            {
                bag = parentBag;

                //if (outLin == null) outLin = new Linear(oAst.ast);

                ///
                /// Analyze AST
                ///

                /// Attributes
                if (ast.type == AST.Type.Attribute)
                {
                    toLinear_attribute();
                    return;
                }

                switch (bag.status)
                {
                    case Bag.Status.Ground:
                        toLinear_ground();
                        break;
                    case Bag.Status.DeclarationParenthesis:
                        toLinear_declarationParenthesis();
                        break;
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

                    var sbag = bag;
                    //if (IsMainBlock && !IsRepeatedInstruction) sbag = bag.subBag();
                    sbag.posNewParam = -1; // Reset new param

                    code.toLinear(sbag);

                    onSchedulerPostCode?.Invoke(sbag, code);
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

            void toLinear_attribute()
            {
                if (ast.aclass == "string")
                    ast.subject = Properties.StringMark + Subject;
                if (ast.aclass == "angularBracket")
                    ast.subject = Properties.AngularBracketsMark + Subject;

                bag.NewParam(Subject);

                bag.OnNextParamOnce?.Invoke();
                bag.OnNextParamOnce = null;
            }

            void toLinear_declarationParenthesis()
            {
                //toLinear_checkSquareBrackets();

                if (IsOperator)
                {
                    switch (Subject)
                    {
                        case "=":
                            bag.OnRepeteable?.Invoke(bag, this.prev);
                            onEnd = delegate
                            {
                                bag.Linear.LastInstruction.Return = bag.Params.Pull().StrValue;
                            };
                            break;
                    }
                }
                else if (IsRepeatedInstruction)
                {
                    bag.OnRepeteable?.Invoke(bag, this.prev);
                }
            }

            void toLinear_ground()
            {
                /// Blocks
                if (ast.type == AST.Type.Block)
                {
                    toLinear_ground_block();
                }
                /// Instructions
                else
                {
                    toLinear_ground_instruction();
                }
            }

            void toLinear_ground_block()
            {
                if (Subject == "[")
                    toLinear_ground_block_squareBrackets();

                if (Subject == "(")
                    toLinear_ground_block_parenthesis();

                if (Subject == "{")
                    toLinear_ground_block_brackets();
            }

            void toLinear_ground_block_squareBrackets()
            {
                bag = bag.subBag(Bag.Status.DeclarationParenthesis);

                onEnd = delegate
                {
                    if (bag.Params.Count > 0)
                    {
                        // Is array call
                        throw new Exception("todo");
                    }
                    else if (Left.ast.type == AST.Type.Attribute)
                    {
                        // Is type definition
                        bag.NewParam(bag.Params.Pull().StrValue + "[]");
                    }
                };
            }

            void toLinear_ground_block_brackets()
            {
                if (!parent.IsBlockDeclaration)
                {
                    if (IsAccumulator)
                    {
                        // Is accumulated attributes
                        bag = bag.subBag(Bag.Status.DeclarationParenthesis);
                        onEnd = delegate
                        {
                            bag.Parent.Params.New(new Parameter(bag.Params));
                        };
                    }
                    else
                    {
                        // is JSON
                        throw new Exception("json todo");
                    }
                }
                else
                {
                    bag = bag.subBag();
                }


            }

            void toLinear_ground_block_parenthesis()
            {
                var completedStatement = bag.statement.GetCompletedStatement;

                if (completedStatement.Type == "Declaration")
                {
                    ///
                    /// This is a good example of different status management
                    ///
                    bag = bag.subBag(Bag.Status.DeclarationParenthesis);
                    var l = bag.Linear = bag.Linear.SetSettings("parameters", ast);
                    l.Op = "parameters";

                    bag.OnRepeteable = delegate (Bag bag, OrderedAST oAst)
                    {
                        var p = bag.Params.Pull();
                        if (p != null)
                        {
                            var lin = new Linear(l, oAst.ast);
                            lin.Op = "parameter";
                            lin.Name = p.StrValue;
                            lin.List();

                            if (bag.Params.Count > 0)
                                lin.Return = bag.Params.Pull().StrValue;
                        }
                    };

                    onEnd = delegate
                    {
                        bag.OnRepeteable(bag, LastCode);
                    };
                }
                else
                {
                    ///
                    /// Here parameters are simply collected from bag.Paramas
                    ///
                    lin = new Linear(bag.Linear, ast);
                    lin.Op = "call";
                    lin.Name = bag.Params.Pull().StrValue;

                    bag = bag.subBag();
                    bag.disableStatementDecoder = true;

                    onEnd = delegate
                    {
                        for (int l = bag.Params.Count - 1; l >= 0; l--)
                        {
                            Linear lin = new Linear(bag.Linear, ast);
                            lin.Op = "push";
                            lin.Name = bag.Params.Pull(0).StrValue;
                            lin.List();
                        }

                        setTempReturn();
                    };
                }
            }

            /// <summary>
            /// Work here
            /// </summary>
            void toLinear_ground_instruction()
            {
                if (Subject == null && codes.Count == 0)
                    return; // Is a dummy instruction

                /*if (IsDeclaration)
                {
                    toLinear_ground_instruction_declaration();
                }
                else */if (IsOperator)
                {
                    toLinear_ground_instruction_operator();
                }
                else if (IsRepeatedInstruction)
                {
                    toLinear_ground_repeatedInstruction();
                }
                else
                {
                    toLinear_ground_instruction_default();
                }
            }

            void toLinear_ground_repeatedInstruction()
            {
                if (bag.OnRepeteable != null)
                {
                    bag = bag.subBag();
                    bag.Linear = bag.Linear.LastInstruction.Clone(ast);
                    onEnd = delegate ()
                    {
                        bag.OnRepeteable?.Invoke(bag, this);
                    };
                }
            }

            /*void toLinear_ground_instruction_declaration()
            {              
                bool isFunction = HasFinalBracketsBlock;

                lin = new Linear(bag.Linear, ast);
                bag = bag.subBag();
                bag.Linear = lin;

                if (isFunction)
                {
                    lin.Op = "function";              
                    bag.Linear = lin;
                }

                enter = true;

                bag.OnRepeteable = delegate (Bag b, OrderedAST oa)
                {
                    var names = b.Params.Pull().Values;
                    var ret = b.Params.Pull()?.StrValue;
                    var attr = b.Params.ToStringList();
                    // b.Params = new Parameters();

                    foreach (var name in names)
                    {
                        var lin = new Linear(bag.Parent.Linear, ast);
                        lin.Op = "declare";
                        lin.Name = name;
                        lin.Return = ret ?? lin.Return;
                        lin.Attributes = attr; //todo for repeated instructions
                        lin.List();
                    }

                    // Add subsequent assign
                    foreach(var l in lin.Instructions)
                    {
                        bag.Parent.Linear.Add(l);
                    }

                    lin = null;
                };

                onEnd = delegate
                {
                    if (isFunction)
                    {
                        lin.Op = "function";
                        lin.Name = bag.Params.Pull()?.StrValue;
                        lin.Return = bag.Params.Pull()?.StrValue ?? lin.Return;
                        lin.Attributes = bag.Params.ToStringList(); //todo for repeated instructions
                    }
                    else
                        bag.OnRepeteable?.Invoke(bag, this);
                };
            }*/

            void toLinear_ground_instruction_operator()
            {
                switch (Subject)
                {
                    case "=":
                        var names = bag.Params.Pull(-1, false);
                        List<Linear> lins = new List<Linear>();

                        onEnd = delegate
                        {
                            var attr = bag.Params.Pull().StrValue;

                            foreach (var name in names.Values)
                            {
                                lin = new Linear(bag.Linear, ast);
                                lin.Op = Subject;
                                lin.Name = name;
                                lin.List();
                                lin.Attributes.Add(attr);
                            }
                        };

                        break;

                    /*case ".":

                        bag.OnNextParamOnce = delegate
                        {
                            var p = bag.Params.Pull().StrValue;
                            bag.NewParam(bag.Params.Pull().StrValue + "." + p);
                        };

                        break;*/

                    default:
                        lin = new Linear(bag.Linear, ast);
                        onEnd = delegate
                        {                         
                            lin.Op = Subject;
                            lin.Attributes.Add(bag.Params.Pull().StrValue);
                            lin.Attributes.Add(bag.Params.Pull().StrValue);
                            setTempReturn();
                        };

                        break;
                }
            }

            void toLinear_ground_instruction_default()
            {
                ///
                /// This means that it is an entry statement, so parameters could cleared at the end
                /// Statement decoder could cause a statement confusion
                ///

                // toLinear_checkSquareBrackets(); // it make no sense here

                /*onEnd = delegate
                {
                    var pars = bag.Params;

                    if (!bag.disableStatementDecoder && pars.Count > 1)
                    {
                        lin = new Linear(bag.Linear, ast);
                        lin.Op = bag.Params.Pull(0).StrValue;

                        var statement = StatementOld.Get(lin.Op);

                        switch (statement.Decoder)
                        {
                            case StatementOld.DecoderType.Normal:

                                while (pars.Count > 0)
                                {
                                    var p = bag.Params.Pull(0).StrValue;
                                    lin.Attributes.Add(p);
                                    lin.Continuous = (p == ":");
                                }

                                if (lin.Continuous)
                                    lin.Attributes.Pull();

                                if (lin.Op == "namespace")
                                    lin.Name = lin.Attributes.Pull(0);

                                break;

                            case StatementOld.DecoderType.WithParameters:

                                lin.Attributes.Add(bag.Params.Pull(0).StrValue);

                                //todo: handle types (?)
                                string prev = "";
                                int i = 0;
                                while (pars.Count > 0)
                                {
                                    if (i % 2 == 1)
                                        lin.Parameters.Add(prev, bag.Params.Pull(0).StrValue);
                                    else
                                        prev = bag.Params.Pull(0).StrValue;

                                    i++;
                                }

                                break;
                        }
                    }

                    bag.Params.Clear();
                };*/

                bag.statement = new Statement(this);
                onSchedulerPostCode = bag.statement.OnPostCode;
                onEnd = bag.statement.OnEnd;
            }

            delegate bool OnScheduler(Bag bag, OrderedAST ast);
            public class Statement // Classe usa e getta
            {
                Statement parent;
                OrderedAST parentOrderedAST, currentOrderedAST;
                Bag currentBag;
                List<Statement> Disks = new List<Statement>();
                Statement completedStatement = null;

                List<OnScheduler> Scheduler = new List<OnScheduler>();
                int SchedulerPos = 0;
                int AbsorbedParams = 0;

                bool imCompleted = false, winnerCalled = false;

                public Statement(OrderedAST oast)
                {
                    parentOrderedAST = oast;

                    AddDisk(new Namespace());
                    AddDisk(new Retrieve());
                    AddDisk(new Declaration());
                }

                private Statement() { }

                internal void AddDisk(Statement disk)
                {
                    disk.parent = this;
                    Disks.Add(disk);
                }

                
                internal bool OnPostCode(Bag bag, OrderedAST orderedAST)
                {
                    currentBag = bag;
                    currentOrderedAST = orderedAST;

                    // order to be decided
                    if (Scheduler.Count > SchedulerPos)
                        return Scheduler[SchedulerPos].Invoke(bag, orderedAST);

                    int d = 0;
                    for (; d<Disks.Count; d++)
                    {
                        var disk = Disks[d];
                        if (!Disks[d].OnPostCode(bag, orderedAST))
                        {
                            if(d < Disks.Count && disk == Disks[d]) // else it was already removed
                                Disks.RemoveAt(d--);
                        }
                    }

                    if (d == 0)
                    {
                        if (imCompleted)
                        {
                            if(!winnerCalled) Winner();
                            return true;
                        }
                        parent?.ImLoser(this);
                    }

                    /*if (d == 1 && (completedStatement?.Disks.Count ?? 1) == 0 && !winnerCalled)
                        completedStatement.Winner();*/

                    return false;
                }

                #region Properties

                internal OrderedAST OrderedAST
                {
                    get
                    {
                        // Just a little tedious
                        Statement par = this;
                        while(par != null && par.parentOrderedAST == null)
                            par = par.parent;
                        return par?.parentOrderedAST;
                    }
                }

                public Statement GetCompletedStatement
                {
                    get
                    {
                        return completedStatement;
                    }
                }

                public string Type
                {
                    get
                    {
                        return this.GetType().FullName.Substring(typeof(Statement).FullName.Length + 1).Replace('+', '.');
                    }
                }

                #endregion

                #region Virtual

                /// <summary>
                /// Called at the end of OrderedAst initiator
                /// </summary>
                internal virtual void OnEnd()
                {
                    if(imCompleted)
                        currentBag.Params.Clear();

                    foreach (var disk in Disks)
                        if(disk.imCompleted) 
                            disk.OnEnd();
                }

                /// <summary>
                /// Called when a statement is surely the requested statement
                /// </summary>
                internal virtual void Winner()
                {

                }

                /// <summary>
                /// It is used for advise a statement that is his turn
                /// </summary>
                /// <param name="currentOrderedAST"></param>
                /// <returns></returns>
                internal virtual bool YourTurn(OrderedAST currentOrderedAST)
                {
                    return true;
                }

                #endregion

                #region Methods
                /// <summary>
                /// This is your branch for sure, so destroy the competion
                /// </summary>
                internal void Monopoly(Statement winner = null)
                {
                    if(winner == null)
                    {
                        parent.Monopoly(this);
                    }
                    else
                    {
                        for (int d = 0; d < Disks.Count; d++)
                            if (Disks[d] != winner)
                                Disks.RemoveAt(d--);
                    }
                }

                // I'm a loser baby so why don't you kill me?
                internal void ImLoser(Statement me)
                {
                    Disks.Remove(me);
                }

                internal void ImCompleted(Statement statement = null)
                {
                    if (statement == null)
                    {
                        statement = this;
                        int d = 0;
                        for(; d<Disks.Count; d++)
                        {
                            if (!Disks[d].YourTurn(currentOrderedAST))
                                Disks.RemoveAt(d--);
                        }

                        if (d == 0 && statement.imCompleted)
                        {
                            statement.Winner();
                            winnerCalled = true;
                        }

                        imCompleted = true;
                    }
                    else
                    {
                        // If it's clear, it clear other disks
                        /*for (int d = 0; d < Disks.Count; d++)
                            if (Disks[d] != statement) 
                                Disks.RemoveAt(d--);*/
                    }

                    if (parent != null)
                        parent.ImCompleted(statement);
                    else
                        completedStatement = statement;
                    
                }

                void PullAbsorbedParams()
                {
                    for (int p = 0; p < AbsorbedParams; p++)
                        OrderedAST.bag.Params.Pull();
                }

                void IncreasePos(bool increaseParams = false)
                {
                    SchedulerPos++;
                    if (increaseParams) IncreaseParams();
                }

                void IncreaseParams()
                {
                    AbsorbedParams++;
                }

                #endregion

                #region Statements

                public class Namespace : Statement
                {
                    Linear lin;
                    string Name;
                    public Namespace()
                    {
                        Scheduler.Add(scheduler_0);
                        Scheduler.Add(scheduler_1);
                        Scheduler.Add(scheduler_2);
                    }
                    bool scheduler_0(Bag bag, OrderedAST ast)
                    {
                        if (!bag.HasNewParam)
                            return false;

                        var param = bag.Params.LastParam;
                        var spar = param.StrValue;

                        if (spar == "namespace")
                        {
                            IncreasePos(true);
                            Monopoly();  

                            return true;
                        }

                        return false;
                    }

                    bool scheduler_1(Bag bag, OrderedAST ast)
                    {
                        if (!bag.HasNewParam)
                            return false;

                        var param = bag.Params.LastParam;
                        var spar = param.StrValue;

                        Name = spar;

                        IncreasePos(true);
                        ImCompleted();

                        lin = new Linear(bag.Linear, ast.ast);
                        lin.Op = "namespace";
                        lin.Name = Name;
                        lin.List();

                        return true;
                    }

                    bool scheduler_2(Bag bag, OrderedAST ast)
                    {
                        if(ast.Subject == ":")
                        {
                            lin.Continuous = true;
                            IncreasePos(true);
                            return true;
                        }

                        return false;
                    }
                }

                // ie: test [= 42 or ()]
                public class Retrieve : Statement
                {
                    string Subject;
                    public Retrieve()
                    {
                        Scheduler.Add(scheduler_0);
                    }

                    bool scheduler_0(Bag bag, OrderedAST ast)
                    {
                        if (!bag.HasNewParam)
                            return false;

                        var param = bag.Params.LastParam;
                        var spar = param.StrValue;

                        Subject = spar;
                        IncreaseParams();

                        var right = ast.Right; // or wrong (lfmao che simpi)
                        if (right == null || right.IsOperator || right.IsCallBlock)
                        {
                            ImCompleted();
                            return true;
                        }

                        return false;
                    }
                }

                // ie: public int test;
                public class Declaration : Statement
                {
                    string Modifier;
                    string Type;
                    string[] Names; // names because declaration supports accumulators

                    public Declaration()
                    {
                        AddDisk(new Function());

                        Scheduler.Add(scheduler_0);
                        Scheduler.Add(scheduler_1);
                        Scheduler.Add(scheduler_2);
                    }

                    // Modifiers
                    bool scheduler_0(Bag bag, OrderedAST ast)
                    {
                        if (!bag.HasNewParam)
                            return false;

                        var param = bag.Params.LastParam;
                        var spar = param.StrValue;

                        if (Light.Modifiers.Contains(spar))
                            Modifier = spar;
                        else // with an else if you could put differnt type of modifier without enforcing the order of entry
                            return Scheduler[++SchedulerPos].Invoke(bag, ast);

                        AbsorbedParams++;

                        return true;
                    }

                    // Type
                    bool scheduler_1(Bag bag, OrderedAST ast)
                    {
                        if (!bag.HasNewParam)
                            return false; 

                        var param = bag.Params.LastParam;
                        var spar = param.StrValue;

                        Type = spar;
                        IncreasePos(true);

                        return true;
                    }

                    // Name
                    bool scheduler_2(Bag bag, OrderedAST ast)
                    {
                        if (!bag.HasNewParam)
                            return false;

                        var param = bag.Params.LastParam;
                        var spar = param.Values;

                        Names = spar;
                        IncreasePos(true);

                        /// Completed!

                        // Saves name param for next instruction
                        if (ast.Right?.IsOperator ?? false)
                        {
                            var namesParam = bag.Params.Pull();
                            AbsorbedParams--;
                            PullAbsorbedParams();
                            bag.Params.New(namesParam);
                        }

                        foreach (var Name in Names)
                        {
                            var lin = new Linear(bag.Linear, OrderedAST.ast);
                            lin.Op = "declare";
                            lin.Name = Name;
                            lin.Return = Type ?? lin.Return;
                            if (!String.IsNullOrEmpty(Modifier)) lin.Attributes.Add(Modifier);
                            lin.List();
                        }

                        ImCompleted();

                        return true;
                    }

                    #region Statements

                    /// <summary>
                    /// The "static" converter in OrderedAst could afford the function without problems
                    /// This class is used mostly for handle grammar errors
                    /// </summary>
                    public class Function : Statement
                    {
                        // A function has simply a BlockParenthesis and a Block
                        public Function()
                        {
                            Scheduler.Add(scheduler_0);
                            Scheduler.Add(scheduler_1);
                        }

                        internal override bool YourTurn(OrderedAST currentOrderedAst)
                        {
                            // stuff [...]

                            // then look to the future
                            if (currentOrderedAst.Right?.IsBlockParenthesis ?? false)
                            {
                                var oa = OrderedAST;
                                oa.enter = true;
                                oa.bag = oa.bag.subBag();
                                oa.bag.EnterLastLinear();

                                OrderedAST.enter = true;
                                return true;
                            }

                            return false;
                        }

                        //todo: bland methods
                        bool scheduler_0(Bag bag, OrderedAST ast)
                        {
                            if (ast.Right?.IsBlockBrackets ?? false)
                            {
                                IncreasePos();
                                return true;
                            }

                            return false;
                        }
                        bool scheduler_1(Bag bag, OrderedAST ast)
                        {
                            ImCompleted();
                            return true;
                        }


                    }

                    #endregion
                }

                #endregion
            }

            #endregion
        }

        

        /// <summary>
        /// To rethink. This is used for distinguish paridally the type of statements
        /// </summary>
        public class StatementOld
        {
            #region Dynamic
            public string Name;
            public DecoderType Decoder = DecoderType.Normal;

            #endregion

            public enum DecoderType
            {
                Normal,
                WithParameters
            }

            #region Static

            public static Dictionary<string, StatementOld> List;

            internal static StatementOld Add(string Name)
            {
                var ins = new StatementOld();
                ins.Name = Name;
                List.Add(Name, ins);
                return ins;
            }



            public static StatementOld Get(string Name)
            {
                init();

                StatementOld o;
                if (!List.TryGetValue(Name, out o))
                    o = new StatementOld();

                return o;
            }

            static void init()
            {
                if (List == null)
                {
                    List = new Dictionary<string, StatementOld>();

                    ///
                    /// List of possible statements
                    ///
                    var import = Add("import");
                    import.Decoder = DecoderType.WithParameters;

                    var include = Add("#include");

                    var _namespace = Add("namespace");
                }
            }

            #endregion
        }

        #endregion
    }
}
