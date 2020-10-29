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
                var trgOperators = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { "==", "!=", "=", ".", "+", "-", "/", "%", "*", "&&", "||", "&", "|", ":" } });
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

                trgNewInstruction.OnTriggered = delegate (Triggers.Trigger trigger)
                {
                    /*var end = curAst.Instruction.NewChild(Line, Pos, AST.Type.Instruction);
                    end.aclass = "end";*/

                    var ast = curAst.NewInstruction(Line, Pos);

                    if (trigger.activatorDelimiter == ",")
                        ast.subject = ",";
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

            bool newInstructionAtNewLine = false;

            AST eatBufferAndClear()
            {
                //todo: check if strBuffer is text
                if (!String.IsNullOrEmpty(strBuffer))
                {
                    //todo: Handle difference between Light and CPP (or create class apart)
                    if (strBuffer == "#include") // For CPP
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

            internal OrderedAST prev;
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
                    var iSquareBrackets = IndexLastSquareBrackets;
                    bool hasAccomulatedAttributes = iSquareBrackets >= 0;
                    if (hasAccomulatedAttributes)
                    {
                        for (int c = iSquareBrackets; c < codes.Count; c++)
                        {
                            if (codes[c].IsAttribute)
                            {
                                hasAccomulatedAttributes = false;
                                break;
                            }
                        }
                    }
                    return hasAccomulatedAttributes;
                }
            }

            #endregion

            #region IndexNavigation

            internal OrderedAST Left
            {
                get
                {
                    /*var pos = parent.codes.IndexOf(this);
                    if (pos > 0)
                        return parent.codes[pos - 1];
                    else
                        return null;*/

                    return prev;
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
                    return !IsOperator && (CountAttributes >= 2 || (CountAttributes >= 1 && HasAccumulatedAttributes)) && !Light.Statements.Contains(codes[0].Subject);
                }
            }

            public bool IsBlockDeclaration
            {
                get
                {
                    var cc = codes.Count;
                    if (cc < 2) return false;
                    return IsDeclaration && codes[cc - 1].IsBlockBrackets && (codes[cc - 2].IsBlockParenthesis || IsCodeBlockWithoutParameters);
                }
            }

            public bool IsCodeBlockWithoutParameters
            {
                get
                {
                    return CodeBlocksWithoutParameters.Contains(prev?.Subject ?? ""); // or a Contains could accept a null value?
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

            #region Parameters

            public class Parameter
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

            public class Parameters : List<Parameter>
            {
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
   
            }

            #endregion

            class Bag
            {
                public Status status;
                public Linear Linear;
                public Bag Parent;
                public Parameters Params = new Parameters();
                public Dictionary<string, string> Dict = new Dictionary<string, string>();

                public OnCallback OnNextParamOnce;
                public OnOperation OnRepeteable;
                //public OnOperationInt setTempReturn;

                internal bool disableStatementDecoder = false;

                public Bag()
                {
                }

                public enum Status
                {
                    Ground,
                    DeclarationParenthesis,
                    JSON //todo
                }


                #region Param
                public void addParam(string p)
                {
                    Params.Add(new Parameter(p));
                }

                #endregion

                public Bag subBag(Status status = Status.Ground)
                {
                    var b = new Bag();
                    b.Parent = this;
                    b.status = status;
                    b.Linear = Linear;
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
            OnCallback onEnd = null, setTempReturn;
            bool enter = false;

            void toLinear(Bag parentBag)
            {
                bag = parentBag;

                //if (outLin == null) outLin = new Linear(oAst.ast);

                ///
                /// Callbacks
                ///
                setTempReturn = delegate //todo move as function
                {
                    lin.Return = Properties.InternalVariable + getTempVar();
                    bag.addParam(Properties.InternalVariable + getTempVar());
                };

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

                    code.toLinear(sbag);
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

                bag.addParam(Subject);

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
                    if (parent.HasAccumulatedAttributes && parent.codes[parent.IndexLastSquareBrackets] == this)
                    {
                        // Is accomulated attributes
                        bag.Parent.Params.Add(new Parameter(bag.Params));
                    }
                    else if (Left.ast.type == AST.Type.Attribute)
                    {
                        //todo put Paramas into []
                        if (bag.Params.Count > 0)
                        {
                            throw new Exception("todo");
                        }

                        bag.addParam(bag.Params.Pull().StrValue + "[]");
                    }
                    else
                    {

                    }
                };
            }

            void toLinear_ground_block_brackets()
            {
                if (!parent.IsDeclaration && !IsBlockDeclaration)
                {
                    // is JSON
                    throw new Exception("json todo");
                }
                else
                {
                    bag = bag.subBag();
                }


            }

            void toLinear_ground_block_parenthesis()
            {
                if (parent.IsDeclaration)
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

            void toLinear_ground_instruction()
            {
                if (Subject == null && codes.Count == 0)
                    return; // Is a dummy instruction

                if (IsDeclaration)
                {
                    toLinear_ground_instruction_declaration();
                }
                else if (IsOperator)
                {
                    toLinear_ground_instruction_operator();

                }
                else if (IsRepeatedInstruction)
                {
                    if (bag.OnRepeteable != null)
                    {
                        bag = bag.subBag();
                        bag.Linear = bag.Linear.LastInstruction.Clone(ast);
                        onEnd = delegate ()
                        {
                            bag.OnRepeteable.Invoke(bag, this);
                        };
                    }
                }
                else
                {
                    ///
                    /// This means that it is an entry statement, so parameters could cleared at the end
                    /// Statement decoder could cause a statement confusion
                    ///

                    // toLinear_checkSquareBrackets(); // it make no sense here

                    onEnd = delegate
                    {
                        var pars = bag.Params;

                        if (!bag.disableStatementDecoder && pars.Count > 1)
                        {
                            lin = new Linear(bag.Linear, ast);
                            lin.Op = bag.Params.Pull(0).StrValue;

                            var statement = Statement.Get(lin.Op);

                            switch (statement.Decoder)
                            {
                                case Statement.DecoderType.Normal:

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

                                case Statement.DecoderType.WithParameters:

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
                    };
                }
            }

            void toLinear_ground_instruction_declaration()
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
            }

            void toLinear_ground_instruction_operator()
            {
                switch (Subject)
                {
                    case "=":
                        var names = bag.Params.Pull(-1, false);
                        List<Linear> lins = new List<Linear>();

                        foreach (var name in names.Values)
                        {
                            lin = new Linear(bag.Linear, ast);
                            lin.Op = Subject;
                            lin.Name = name;
                            lin.List();
                            lins.Add(lin);
                        }

                        onEnd = delegate
                        {
                            var attr = bag.Params.Pull().StrValue;
                            foreach (var lin in lins)
                            {
                                lin.Attributes.Add(attr);
                            }
                        };

                        break;

                    case ".":

                        bag.OnNextParamOnce = delegate
                        {
                            var p = bag.Params.Pull().StrValue;
                            bag.addParam(bag.Params.Pull().StrValue + "." + p);
                        };

                        break;

                    default:
                        lin = new Linear(bag.Linear, ast);
                        lin.Op = Subject;
                        lin.Attributes.Add(bag.Params.Pull().StrValue);
                        setTempReturn();

                        onEnd = delegate
                        {
                            lin.Attributes.Add(bag.Params.Pull().StrValue);
                        };

                        break;
                }
            }

            #endregion
        }

        /// <summary>
        /// To rethink
        /// </summary>
        public class Statement
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

            public static Dictionary<string, Statement> List;

            internal static Statement Add(string Name)
            {
                var ins = new Statement();
                ins.Name = Name;
                List.Add(Name, ins);
                return ins;
            }



            public static Statement Get(string Name)
            {
                init();

                Statement o;
                if (!List.TryGetValue(Name, out o))
                    o = new Statement();

                return o;
            }

            static void init()
            {
                if (List == null)
                {
                    List = new Dictionary<string, Statement>();

                    ///
                    /// List of possible statements
                    ///
                    var import = Add("import");
                    import.Decoder = DecoderType.WithParameters;

                    var include = Add("#include");
                }
            }

            #endregion
        }

        #endregion
    }
}
