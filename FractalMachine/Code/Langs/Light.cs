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
using System.ComponentModel.DataAnnotations;

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

                // Good practice:
                // - put first triggers with multiple characters, in order of character length

                /// Default
                var statusDefault = statusSwitcher.Define("default");

                var trgString = statusDefault.Add(new Triggers.Trigger { Delimiter = "\"", ActivateStatus = "inString" });
                var trgAngularBrackets = statusDefault.Add(new Triggers.Trigger { Delimiter = "<", ActivateStatus = "inAngularBrackets" });
                var trgSpace = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { " ", "\t", "," } });
                var trgNewInstruction = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { ";", "," } }); // technically the , is a new instruction
                var trgNewLine = statusDefault.Add(new Triggers.Trigger { Delimiter = "\n" });
                var trgOperators = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { "==", "!=", "<=", ">=", "<", ">", "=", "+", "-", "/", "%", "*", "&&", "||", "&", "|", ":" } });
                var trgFastIncrement = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { "++", "--" } });
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

                trgFastIncrement.OnTriggered = delegate (Triggers.Trigger trigger)
                {
                    var instr = curAst.Instruction.NewInstruction(Line, Pos);
                    instr.subject = trigger.activatorDelimiter;
                    instr.aclass = "fastIncrement";
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
                    // isSymbol.Value = before switch
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
                        // is short operation ie test += 2 => test = test + 2
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

            #region TempVar
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

            #endregion

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
                    return ast.aclass == "operator" || IsFastIncrement;
                }
            }

            public bool IsFastIncrement
            {
                get
                {
                    return ast.aclass == "fastIncrement";
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
                    return ast.type == AST.Type.Attribute || IsAccumulator;
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

            public bool IsAttached //to previous attribute, with the exception for operators
            {
                get
                {
                    if (String.IsNullOrEmpty(Subject))
                        return false;

                    var ct = new CharType(Subject[0]);
                    return !ct.IsAlphanumeric;
                }
            }

            #endregion

            internal string Subject
            {
                get
                {
                    return ast.subject;
                }

                set
                {
                    ast.subject = value;
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


            #region Parameters

            internal class Parameter
            {
                string strValue;
                List<string> values;
                Parameters paramsValue;

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
                    paramsValue = parameters;
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

                public Parameters AsParams
                {
                    get
                    {
                        return paramsValue;
                    }
                }
            }

            internal class Parameters : List<Parameter>
            {
                public string Name;
                public string Type;

                Bag bag;
                public Parameters(Bag bag):base()
                {
                    // you shot me down, bag bag
                    this.bag = bag;
                }

                #region New
                public void New (Parameter par)
                {
                    bag.posNewParam = Count;
                    Add(par);                    
                }
                public void New(string par)
                {
                    New(new Parameter(par));
                }
                #endregion

                public int FlagPos = 0;
                public void RecordFlagPosition()
                {
                    FlagPos = Count;
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

            internal delegate void OnCallback();
            internal delegate void OnOperation(OrderedAST ast);
            internal delegate bool OnScheduler(OrderedAST ast);

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
                    JSON, //todo
                    ReadAsIs
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

                public Bag subBag(OrderedAST oAst, Status status = Status.Ground)
                {
                    var b = subBag(status);
                    oAst.bag = b;
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
            OnOperation onBeforeChildCycle;

            bool exitLinear = false;

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
                    case Bag.Status.ReadAsIs:
                        toLinear_readAsIs();
                        break;
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

                    onBeforeChildCycle?.Invoke(code);

                    code.toLinear(sbag);

                    onSchedulerPostCode?.Invoke(code);
                }

                ///
                /// Exit
                ///

                if (onEnd != null)
                    onEnd();

                if (exitLinear)
                    bag.Linear = bag.Linear.parent;

                if (lin != null)
                    lin.List();

            }

            void toLinear_attribute()
            {
                if (ast.aclass == "string")
                    ast.subject = Properties.StringMark + Subject;
                if (ast.aclass == "angularBracket")
                    ast.subject = Properties.AngularBracketsMark + Subject;

                bag.NewParam(Subject); // == bag.Params.New(Subject)

                bag.OnNextParamOnce?.Invoke();
                bag.OnNextParamOnce = null;
            }

            void toLinear_readAsIs()
            {
                if (IsOperator)
                    bag.Params.New(Subject);
            }

            void toLinear_declarationParenthesis()
            {
                //toLinear_checkSquareBrackets();

                if (IsOperator)
                {
                    switch (Subject)
                    {
                        case "=":
                            bag.OnRepeteable?.Invoke(this.prev);
                            onEnd = delegate
                            {
                                bag.Linear.LastInstruction.Return = bag.Params.Pull().StrValue;
                            };
                            break;
                    }
                }
                else if (IsRepeatedInstruction)
                {
                    bag.OnRepeteable?.Invoke(this.prev);
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
                    if (IsAccumulator)
                    {
                        // Are accumulated attributes
                        bag.Params.Name = "accumulated";
                        bag.Params.Type = "[]";
                        bag.Parent.Params.New(new Parameter(bag.Params));
                    }
                    else
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
                    }
                };
            }

            void toLinear_ground_block_brackets()
            {
                var completedStatement = bag.statement.GetCompletedStatement;
                var csType = completedStatement.Type;

                if (csType == "Declaration.Function" || csType == "Block")
                {
                    bag = bag.subBag();
                }
                else
                {
                    if (IsAccumulator)
                    {
                        // Are accumulated attributes
                        bag = bag.subBag(Bag.Status.DeclarationParenthesis);
                        onEnd = delegate
                        {
                            bag.Params.Name = "accumulated";
                            bag.Params.Type = "{}";
                            bag.Parent.Params.New(new Parameter(bag.Params));
                        };
                    }
                    else
                    {
                        // is JSON
                        throw new Exception("json todo");
                    }
                }

            }

            void toLinear_ground_block_parenthesis()
            {
                var completedStatement = bag.statement.GetCompletedStatement;

                if (completedStatement.Type == "Declaration.Function")
                {
                    ///
                    /// This is a good example of different status management
                    ///
                    bag = bag.subBag(Bag.Status.DeclarationParenthesis);
                    var l = bag.Linear = bag.Linear.SetSettings("parameters", ast);

                    bag.OnRepeteable = delegate (OrderedAST oAst)
                    {
                        var bag = oAst.bag;
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
                        bag.OnRepeteable(LastCode);
                    };
                }
                else if(completedStatement.Type == "Block")
                {
                    bag = bag.subBag();
                    var l = bag.Linear = bag.Linear.SetSettings("parameter", ast);
                    
                    // Multiple instructions
                    if (codes.Count > 1)
                    {
                        onBeforeChildCycle = delegate (OrderedAST oAst)
                        {
                            var subLin = bag.Linear = new Linear(l, oAst.ast);
                            subLin.Op = "instruction";
                            subLin.List();
                            //attachStatement();
                        };
                    }
                  
                    onEnd = delegate
                    {
                        var p = bag.Params.Pull();
                        l.Name = p.StrValue;
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

                attachStatement();

                if (IsOperator)
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
                        bag.OnRepeteable?.Invoke(this);
                    };
                }
            }

            void toLinear_ground_instruction_operator()
            {
                if (IsFastIncrement)
                {
                    Subject = Subject[0].ToString();
                    bag.NewParam("1");
                }

                switch (Subject)
                {
                    case "=":
                        var names = bag.Params.Pull(false);
                        //attachStatement();

                        onEnd = delegate
                        {
                            var attr = bag.Params.Pull().StrValue;

                            foreach (var name in names.Values)
                            {
                                lin = new Linear(parent.bag.Linear, ast);
                                lin.Op = Subject;
                                lin.Name = name;
                                lin.List();
                                lin.Attributes.Add(attr);
                            }
                        };

                        break;

                    default:
                        lin = new Linear(parent.bag.Linear, ast);
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

                //attachStatement();
            }

            void attachStatement()
            {
                bag.statement = new Statement(this);
                onSchedulerPostCode = bag.statement.OnPostCode;
                onEnd = bag.statement.OnEnd;
            }

            public class Statement // Classe usa e getta
            {
                Statement parent;
                OrderedAST parentOrderedAST, curderedAST;
                Bag curBag;
                List<Statement> Disks = new List<Statement>();

                OnCallback OnRepeteable;

                List<OnScheduler> Scheduler = new List<OnScheduler>();
                int SchedulerPos = 0;
                int AbsorbedParams = 0;               

                public Statement(OrderedAST oast)
                {
                    parentOrderedAST = oast;

                    AddDisk(new Import());
                    AddDisk(new Namespace());
                    AddDisk(new Declaration());
                    AddDisk(new Block());
                }

                private Statement() { }

                internal void AddDisk(Statement disk)
                {
                    disk.parent = this;

                    // Don't add twice the same type
                    bool alreadyExisting = false;
                    foreach (var d in Disks)
                        if (d.GetType() == disk.GetType())
                            alreadyExisting = true;

                    if (!alreadyExisting)
                    {
                        Disks.Add(disk);
                        disk.OnRepeteable?.Invoke();
                    }
                }              
                internal bool OnPostCode( OrderedAST orderedAST)
                {
                    curBag = orderedAST.bag;
                    curderedAST = orderedAST;

                    if (Scheduler.Count > SchedulerPos)
                    {
                        var res = Scheduler[SchedulerPos].Invoke(orderedAST);
                        if (res && Scheduler.Count <= SchedulerPos) CheckDisksTurn();
                        return res;
                    }

                    int d = 0;
                    for (; d<Disks.Count; d++)
                    {
                        var disk = Disks[d];
                        if (!Disks[d].OnPostCode(orderedAST))
                        {
                            Disks.RemoveAt(d--);
                            if(d == 0 && ImCompleted)
                                LastSurvivor();
                        }
                    }

                    OnRepeteable?.Invoke();

                    return ImCompleted || Disks.Count > 0;
                }

                #region Properties

                bool _imCompleted = false;
                internal bool ImCompleted
                {
                    get
                    {
                        foreach (var d in Disks)
                            if (d.ImCompleted)
                                return true;

                        return _imCompleted || SchedulerPos >= Scheduler.Count;
                    }

                    set
                    {
                        _imCompleted = value;
                    }
                }
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
                        foreach (var d in Disks)
                            if (d.ImCompleted)
                                return d.GetCompletedStatement;

                        if(Disks.Count == 1)
                            return Disks[0].GetCompletedStatement;

                        return this;
                    }
                }

                public string Type
                {
                    get
                    {
                        var t = GetType();
                        if (t == typeof(Statement)) return "";
                        return t.FullName.Substring(typeof(Statement).FullName.Length + 1).Replace('+', '.');
                    }
                }

                #endregion

                #region Virtual

                /// <summary>
                /// Called at the end of OrderedAst initiator
                /// </summary>
                internal virtual void OnEnd()
                {
                    if (ImCompleted)
                        PullAbsorbedParams();

                    foreach (var disk in Disks)
                        if(disk.ImCompleted) 
                            disk.OnEnd();
                }

                /// <summary>
                /// Called when a statement is surely the requested statement
                /// </summary>
                internal virtual void LastSurvivor()
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

                internal void CheckDisksTurn()
                {
                    for(int d=0; d<Disks.Count; d++)
                    {
                        if (!Disks[d].YourTurn(curderedAST))
                            Disks.RemoveAt(d--);
                    }
                }

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

                public class Block : Statement
                {
                    string[] Blocks = new string[] { "if", "else", "for" };
                    //string[] BlocksWithParameters = new string[] { "if" };
                    //string[] BlocksWithoutParameters = new string[] { "else" };

                    string Name;
                    public Block()
                    {
                        Scheduler.Add(scheduler_0);
                    }
                    
                    // Name
                    bool scheduler_0(OrderedAST ast)
                    {
                        var bag = ast.bag;

                        if (!bag.HasNewParam)
                            return false;

                        Name = bag.Params.LastParam.StrValue;

                        bool isBlock = Blocks.Contains(Name);
                        
                        //if(!isBlock && (isBlock = BlocksWithoutParameters.Contains(Name))) IncreasePos();
                      
                        if (isBlock)
                        {
                            IncreasePos();
                            Monopoly();

                            var lin = new Linear(OrderedAST.bag.Linear, OrderedAST.ast);
                            lin.Op = "block";
                            lin.Name = Name;
                            lin.List();

                            var b = OrderedAST.bag = OrderedAST.bag.subBag();
                            b.Linear = lin;
                        }

                        return isBlock;
                    }

                    internal override void OnEnd()
                    {
                        OrderedAST.bag = OrderedAST.bag.Parent;
                    }
                }

                // ie import IO from System
                public class Import : Statement
                {
                    Linear lin;
                    string Name;

                    static string[] Parameters = new string[] { "from" };
                    public Import()
                    {
                        OnRepeteable = delegate
                        {
                            AddDisk(new Parameter());
                        };

                        Scheduler.Add(scheduler_0);
                        Scheduler.Add(scheduler_1);
                    }
                    bool scheduler_0(OrderedAST ast)
                    {
                        var bag = ast.bag;

                        if (!bag.HasNewParam)
                            return false;

                        var spar = bag.Params.LastParam.StrValue;

                        if (spar == "import")
                        {
                            IncreasePos(true);
                            Monopoly();

                            return true;
                        }

                        return false;
                    }

                    bool scheduler_1(OrderedAST ast)
                    {
                        var bag = ast.bag;

                        if (!bag.HasNewParam)
                            return false;

                        var spar = bag.Params.LastParam.StrValue;

                        Name = spar;

                        IncreasePos(true);

                        lin = new Linear(bag.Linear, ast.ast);
                        lin.Op = "import";
                        lin.Name = Name;
                        lin.List();

                        return true;
                    }

                    internal override void LastSurvivor()
                    {
                        // throw new exception
                    }

                    #region Statements 

                    /// <summary>
                    /// This is an example of generic keyword handler
                    /// </summary>
                    public class Parameter : Statement
                    {
                        string parameter;
                        string value = "";
                        public Parameter()
                        {
                            Scheduler.Add(scheduler_0);
                            Scheduler.Add(scheduler_1);
                        }
                        bool scheduler_0(OrderedAST ast)
                        {
                            var bag = ast.bag;

                            if (!bag.HasNewParam)
                                return false;

                            var spar = bag.Params.LastParam.StrValue;

                            if (Parameters.Contains(spar)) // spar or ast.Subject in this case have the same value
                            {
                                parameter = spar;
                                IncreasePos();

                                OrderedAST.bag = OrderedAST.bag.subBag(Bag.Status.ReadAsIs);
                                OrderedAST.bag.Params.RecordFlagPosition();

                                return true;
                            }

                            return false;
                        }

                        bool scheduler_1(OrderedAST ast)
                        {
                            var bag = ast.bag;

                            if (!bag.HasNewParam)
                                return false;

                            while (bag.Params.FlagPos < bag.Params.Count)
                            {
                                var spar = bag.Params.LastParam.StrValue;
                                value += spar;
                            }

                            if (!(ast.Right?.IsAttached ?? false))
                            {
                                IncreasePos();

                                var p = (Import)parent;
                                p.lin.Parameters.Add(parameter, value);

                                OrderedAST.bag = OrderedAST.bag.Parent;
                            }

                            return true;
                        }
                    }

                    #endregion
                }

                // ie namespace MyNamespace.Utils:
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
                    bool scheduler_0( OrderedAST ast)
                    {
                        var bag = ast.bag;

                        if (!bag.HasNewParam)
                            return false;

                        var spar = bag.Params.LastParam.StrValue;

                        if (spar == "namespace")
                        {
                            IncreasePos(true);
                            Monopoly();  

                            return true;
                        }

                        return false;
                    }

                    bool scheduler_1(OrderedAST ast)
                    {
                        var bag = ast.bag;

                        if (!bag.HasNewParam)
                            return false;

                        var spar = bag.Params.LastParam.StrValue;

                        Name = spar;

                        ImCompleted = true;
                        IncreasePos(true);

                        lin = new Linear(bag.Linear, ast.ast);
                        lin.Op = "namespace";
                        lin.Name = Name;
                        lin.List();

                        return true;
                    }

                    bool scheduler_2(OrderedAST ast)
                    {
                        if(ast.Subject == ":")
                        {
                            lin.Continuous = true;
                            IncreasePos(true);
                            return true;
                        }

                        //todo throw new Exception
                        return false;
                    }
                }

                // ie: public int test;
                public class Declaration : Statement
                {
                    Linear lin;
                    string Modifier;
                    string DeclType;
                    string[] Names; // names because declaration supports accumulators
                    public Declaration()
                    {
                        AddDisk(new Function());

                        Scheduler.Add(scheduler_0);
                        Scheduler.Add(scheduler_1);
                        Scheduler.Add(scheduler_2);
                    }

                    // Modifiers
                    bool scheduler_0(OrderedAST ast)
                    {
                        var bag = ast.bag;

                        if (!bag.HasNewParam)
                            return false;

                        var spar = bag.Params.LastParam.StrValue;

                        if (Light.Modifiers.Contains(spar))
                            Modifier = spar;
                        else // with an else if you could put differnt type of modifier without enforcing the order of entry
                            return Scheduler[++SchedulerPos].Invoke(ast);

                        AbsorbedParams++;

                        return true;
                    }

                    // Type
                    bool scheduler_1(OrderedAST ast)
                    {
                        var bag = ast.bag;

                        if (!bag.HasNewParam)
                            return false;

                        var spar = bag.Params.LastParam.StrValue;

                        DeclType = spar;              
                        IncreasePos(true);

                        // Is anonymous function
                        if (DeclType == "function" && ast.Right.IsBlockParenthesis)
                        {
                            bag.NewParam("%anonymous");
                            Scheduler[SchedulerPos](ast);
                        }

                        return true;
                    }

                    // Name
                    bool scheduler_2(OrderedAST ast)
                    {
                        var bag = ast.bag;

                        if (!ast.IsAttribute)
                            return false;

                        Names = bag.Params.LastParam.Values;

                        IncreasePos(true);

                        /// Completed!

                        // Saves name param for next instruction
                        if (ast.Right?.IsOperator ?? false)
                        {
                            var namesParam = bag.Params.Pull();
                            AbsorbedParams--;
                            bag.Params.New(namesParam);
                        }

                        foreach (var Name in Names)
                        {
                            lin = new Linear(bag.Linear, OrderedAST.ast);
                            lin.Op = "declare";
                            lin.Name = Name;
                            lin.Return = DeclType ?? lin.Return;
                            if (!String.IsNullOrEmpty(Modifier)) lin.Attributes.Add(Modifier);
                            lin.List();
                        }

                        return true;
                    }

                    #region Statements

                    /// <summary>
                    /// The "static" converter in OrderedAst could afford the function without problems
                    /// This class is used mostly for handle grammar errors
                    /// </summary>
                    public class Function : Statement
                    {
                        Declaration decl;
                        bool isDefaultType = false;

                        // A function has simply a BlockParenthesis and a Block
                        public Function()
                        {
                            Scheduler.Add(scheduler_0);
                            Scheduler.Add(scheduler_1);
                        }
                        internal override bool YourTurn(OrderedAST currentOrderedAst)
                        {
                            decl = (Declaration)parent;

                            switch (decl.Type)
                            {
                                case "class":
                                    if (!currentOrderedAst.Right?.IsBlockBrackets ?? true)
                                        return false;

                                    SchedulerPos++; // it jumps parameters
                                    isDefaultType = true;

                                    break;

                                default:
                                    // by default is a function (if has parenthesis)
                                    if (!currentOrderedAst.Right?.IsBlockParenthesis ?? true)
                                        return false;

                                    if (decl.Type == "function")
                                        isDefaultType = true;

                                    break;
                            }

                            var oa = OrderedAST;
                            oa.bag = oa.bag.subBag();
                            oa.bag.EnterLastLinear();

                            return true;
                        }

                        bool scheduler_0(OrderedAST ast)
                        {
                            if (ast.Right?.IsBlockBrackets ?? false)
                            {
                                IncreasePos();
                                return true;
                            }

                            return false;
                        }
                        bool scheduler_1(OrderedAST ast)
                        {

                            var decl = (Declaration)parent;
                            var name = decl.Names[0];

                            var lin = decl.lin;
                            if (isDefaultType)
                            {
                                lin.Op = decl.DeclType;
                            }
                            else
                            {
                                lin.Op = "function";
                                lin.Return = decl.DeclType;
                            }

                            lin.Return = decl.DeclType;

                            if (name == "%anonymous")
                                lin.Name = name = Properties.InternalVariable + OrderedAST.getTempVar();

                            OrderedAST.bag.NewParam(name); 

                            return true;
                        }


                    }

                    #endregion
                }


                #endregion
            }

            #endregion
        }

        #endregion
    }
}
