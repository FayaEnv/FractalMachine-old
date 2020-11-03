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
using static FractalMachine.Code.Type;

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

        #region Types

        public class TypesSet : Code.TypesSet
        {
            override internal void init()
            {
                /// char
                var _char = AddType("char");
                _char.Bytes = 1;
                _char.Signed = true;

                /// uchar
                var _uchar = AddType("uchar");
                _uchar.Bytes = 1;

                /// short
                var _short = AddType("short");
                _short.Bytes = 2;
                _short.Signed = true;

                /// ushort
                var _ushort = AddType("ushort");
                _ushort.Bytes = 2;

                /// int
                var _int = AddType("int");
                _int.Bytes = 4;
                _int.Signed = true;

                /// uint
                var _uint = AddType("uint");
                _uint.Bytes = 4;

                /// long
                var _long = AddType("long");
                _long.Bytes = 8;
                _long.Signed = true;

                /// ulong
                var _ulong = AddType("ulong");
                _ulong.Bytes = 8;

                /// float
                var _float = AddType("float");
                _float.Bytes = 4;
                _float.Floating = true;

                /// double
                var _double = AddType("double");
                _double.Bytes = 8;
                _double.Floating = true;

                /// double
                var _decimal = AddType("decimal");
                _decimal.Bytes = 12;
                _decimal.Floating = true;

                /// string
                var _string = AddType("string");
                _string.Base = _char;
                _string.Array = true;
            }

            override public AttributeType SolveAttribute(string Name)
            {
                var atype = new AttributeType(this);
                atype.Type = AttributeType.Types.Name;

                if (Name.HasStringMark())
                {
                    atype.TypeRef = "string";
                }
                else if (Char.IsDigit(Name[0]))
                {
                    atype.TypeRef = "int";

                    for (int c = 0; c < Name.Length; c++)
                    {
                        if (!Char.IsLetter(Name[c]))
                        {
                            atype.Type = AttributeType.Types.Invalid;
                            break;
                        }

                        if (Name[c] == '.')
                            atype.TypeRef = "double";

                        if (c == Name.Length - 1 && Name[c] == 'f')
                            atype.TypeRef = "float";
                    }     
                }

                return atype;
            }

            override public string ConvertAttributeTo(string Attribute, Type To, AttributeType From=null)
            {
                if(From == null)
                    From = SolveAttribute(Attribute);

                if (To.Class)
                {
                    throw new Exception("todo");
                }

                if (From.Type == AttributeType.Types.Type)
                {
                    switch (From.TypeRef)
                    {
                        case "string":
                            return Attribute.NoMark();

                        default:
                            if (To.AttributeReference == "string")
                                return Properties.StringMark + Attribute;
                            break;
                    }
                }

                return Attribute;
            }
        }

        static TypesSet myTypesSet;

        override public Code.TypesSet GetTypesSet
        {
            get
            {
                if (myTypesSet == null)
                    myTypesSet = new TypesSet();

                return myTypesSet;
            }
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
                var trgOperator = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { "==", "!=", "<=", ">=", "<", ">", "=", "+", "-", "/", "%", "*", "&", "|", ":", "?" } });
                var trgConjunction = statusDefault.Add(new Triggers.Trigger { Delimiters = new string[] { "&&", "||" } });
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

                trgOperator.OnTriggered = delegate (Triggers.Trigger trigger)
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

                trgConjunction.OnTriggered = delegate (Triggers.Trigger trigger)
                {
                    var child = curAst.Instruction.NewChild(Line, Pos, AST.Type.Instruction);
                    child.subject = trigger.activatorDelimiter;
                    child.aclass = "conjunction";
                    clearBuffer();
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

                    if (del == "}")
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
            internal OrderedAST Parent;
            internal List<OrderedAST> codes = new List<OrderedAST>();

            internal OrderedAST prev, next;

            public OrderedAST(AST ast)
            {
                linkAst(ast);
            }

            public OrderedAST(AST ast, OrderedAST parent)
            {
                this.Parent = parent;
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
                            new OrderedAST(Parent.ast.children[0], lc);
                            lc.codes.Add(this);
                            Parent.codes[Parent.codes.Count - 1] = lc;
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
                    if (!ch.IsDummyInstruction)
                    {
                        codes.Add(ch);
                        if (previous != null) previous.next = ch;
                        ch.prev = previous;
                        previous = ch;
                    }
                }

                Revision();
            }

            #region TempVar

            internal int tempVarCount = 0, tempVar = -1;
            internal int getTempNum()
            {
                var num = tempVarCount;
                var par = Parent;

                //todo: improve this checking using linears (are more reliable, in particular for JSON)
                if (!IsBlockContainer)
                    return par.getTempNum();

                while (par != null)
                {
                    num += par.tempVarCount;
                    par = par.Parent;
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

            public OrderedAST FirstCode
            {
                get
                {
                    var cnt = codes.Count;
                    if (cnt > 0)
                        return codes[0];

                    return null;
                }
            }

            public OrderedAST RightBlockParenthesis
            {
                get
                {
                    if (IsBlockParenthesis)
                        return this;
                    if (Right == null)
                        return null;
                    return Right.RightBlockParenthesis;
                }
            }

            public OrderedAST RightBlockBrackets
            {
                get
                {
                    if (IsBlockBrackets)
                        return this;
                    if (Right == null)
                        return null;
                    return Right.RightBlockBrackets;
                }
            }

            #endregion

            #region Is
            public bool IsAssignAccumulator
            {
                get
                {
                    //todo: there is another type of accumulator: the SquareBrackets accumulato
                    // could be found if it has an operator in front of it
                    if (ast.type != AST.Type.Block || ast.subject == "[")
                        return false;

                    if (!(Left?.IsAssign ?? false)) 
                        return false;

                    foreach (var code in codes)
                    {
                        var lc = code.LastCode;
                        //todo study: in what cases lc == null?
                        if (lc == null || lc.IsOperator && lc.Subject == ":") //Is JSON property
                            return false;
                    }

                    return true;
                }
            }

            public bool IsBlockContainer
            {
                get
                {
                    return Parent == null || (IsBlockBrackets && !isJsonObject);
                }
            }

            #region Operators
            public bool IsOperator
            {
                get
                {
                    return ast.aclass == "operator" || IsFastIncrement || IsConjunction;
                }
            }

            public bool IsAssign
            {
                get
                {
                    return ast.aclass == "operator" && ast.subject == "=";
                }
            }

            public bool IsFastIncrement
            {
                get
                {
                    return ast.aclass == "fastIncrement";
                }
            }

            public bool IsConjunction
            {
                get
                {
                    return ast.aclass == "conjunction";
                }
            }

            public int OperatorPriority
            {
                get
                {
                    if (IsConjunction) return 1;
                    return 0;
                }
            }

            #endregion
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

            bool _forceRepeat = false;
            public bool IsRepeatedInstruction
            {
                get
                {
                    return _forceRepeat || ast.type == AST.Type.Instruction && Subject == ",";
                }

                set //todo: l'ho fatto ma forse è inutile
                {
                    _forceRepeat = value;
                }
            }

            public bool IsAttribute
            {
                get
                {
                    return ast.type == AST.Type.Attribute || IsAssignAccumulator;
                }
            }

            public bool IsDotAttribute
            {
                get
                {
                    return ast.type == AST.Type.Attribute && Subject.Length > 0 && Subject[0] == '.';
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

            public bool IsToAssign
            {
                get
                {
                    return (Right?.IsAssign ?? false) || (Parent?.Parent?.IsAssignAccumulator ?? false);
                }
            }

            public bool IsDummyInstruction
            {
                get
                {
                    return ast.type == AST.Type.Instruction && Subject == null && codes.Count == 0;
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


            #endregion

            #region Methods

            void NewInstructionFromChild(OrderedAST child)
            {
                var instrAst = new AST(child.ast, child.ast.line, child.ast.pos, AST.Type.Instruction);
                var oAst = new OrderedAST(instrAst, this);

                var index = codes.IndexOf(child);
                while(index<codes.Count)
                {
                    oAst.codes.Add(codes[index]);
                    codes[index].Parent = oAst;
                    codes.RemoveAt(index);
                }

                codes.Add(oAst);
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

                // Array
                internal bool isArray = false;
                internal bool isArrayAssign = false;
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
                        if(strValue != null)
                            return strValue; 

                        if (values != null)
                        {
                            if (values.Count == 1)
                                return values[0];

                            return null;
                        }

                        return null;
                    }

                    set
                    {
                        strValue = value;
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
                        if (isArray && paramsValue == null)
                            paramsValue = new Parameters(null);

                        return paramsValue;
                    }
                }

                public Parameters AsAccumulator
                {
                    get
                    {
                        if ((paramsValue?.Type ?? "") == "()=")
                            return paramsValue;

                        var pars = new Parameters(null);
                        pars.Type = "()=";
                        pars.Add(this);

                        return pars;
                    }                    
                }
            }

            internal class Parameters : List<Parameter>
            {
                public string Name;
                public string Type;

                Bag bag;
                public Parameters(Bag bag) : base()
                {
                    // you shot me down, bag bag
                    this.bag = bag;
                }

                #region New
                public void New(Parameter par)
                {
                    if (par == null)
                        return; // for debug purposes
                    if(bag != null) bag.posNewParam = Count;
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
                    foreach (var par in this)
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
                internal int posNewParam = -1;

                public OnOperation OnRepeatable;

                public Bag()
                {
                    Params = new Parameters(this);
                }

                public enum Status
                {
                    Ground,
                    DeclarationParameters,
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

                public void AppendToParam(string p)
                {
                    var pp = Params.Pull(false);
                    pp.StrValue += p;
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
            Statement _statement;

            OnScheduler onSchedulerPostCode = null;
            OnOperation onBeforeChildCycle, onAfterChildCycle;
            Statement firstStatement
            {
                get
                {
                    if (_statement != null)
                        return _statement;
                    if (Parent != null)
                        return Parent.firstStatement;
                    return null;
                }
            }

            string setTempReturn(bool AddAsParam = true)
            {
                var ret = Properties.InternalVariable + getTempVar();
                if (lin != null) lin.Return = ret;
                if(AddAsParam) bag.NewParam(ret);
                return ret;
            }

            void toLinear(Bag parentBag)
            {
                bag = parentBag;

                ///
                /// Analyze AST
                ///

                /// Attributes
                if (ast.type == AST.Type.Attribute)
                {
                    toLinear_attribute();
                    return;
                }

                if (IsRepeatedInstruction)
                {
                    if (bag.OnRepeatable != null)
                        bag.OnRepeatable?.Invoke(this);
                }

                switch (bag.status)
                {
                    case Bag.Status.Ground:
                        toLinear_ground();
                        break;
                    case Bag.Status.DeclarationParameters:
                        toLinear_declarationParenthesis();
                        break;
                    case Bag.Status.ReadAsIs:
                        toLinear_readAsIs();
                        break;
                }

                ///
                /// Child analyzing
                ///

                for(int c=0; c<codes.Count; c++)
                {
                    var code = codes[c];

                    // todo: try catch for error checking(?)

                    var sbag = bag;
                    //if (IsMainBlock && !IsRepeatedInstruction) sbag = bag.subBag();
                    sbag.posNewParam = -1; // Reset new param

                    onBeforeChildCycle?.Invoke(code);
                    code.toLinear(sbag);
                    onAfterChildCycle?.Invoke(code);

                    onSchedulerPostCode?.Invoke(code);

                    // if block clear params 
                    if (IsBlockContainer)
                        bag.Params.Clear();
                }

                ///
                /// Exit
                ///
                if (!endCalled)
                    callEnd();

            }

            OnCallback onEnd = null;
            List<OnCallback> onPreEnds = new List<OnCallback>(), onPostEnds = new List<OnCallback>();
            bool relaxDontListIt = false;
            bool endCalled = false;
            void callEnd()
            {
                foreach (var end in onPreEnds)
                    end();

                if (onEnd != null)
                    onEnd();

                if (!relaxDontListIt && lin != null)
                    lin.List();

                foreach (var end in onPostEnds)
                    end();

                endCalled = true;
            }

            void toLinear_attribute()
            {
                if (ast.aclass == "string")
                    ast.subject = Properties.StringMark + Subject;
                if (ast.aclass == "angularBracket")
                    ast.subject = Properties.AngularBracketsMark + Subject;

                bool append = false;
                if(bag.status == Bag.Status.Ground)
                {
                    if (IsDotAttribute)
                    {
                        append = true;
                    }
                }

                if(append)
                    bag.AppendToParam(Subject);
                else
                    bag.NewParam(Subject); 
            }

            void toLinear_readAsIs()
            {
                if (IsOperator)
                    bag.Params.New(Subject);
            }

            void toLinear_declarationParenthesis()
            {
                if (IsOperator)
                {
                    switch (Subject)
                    {
                        case "=":
                            bag.OnRepeatable?.Invoke(this.prev);
                            onEnd = delegate
                            {
                                bag.Linear.LastInstruction.Return = bag.Params.Pull().StrValue;
                            };
                            break;
                    }
                }
            }

            #region toLinear_ground

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

            #region toLinear_ground_block
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
                if (Left == null)
                {
                    ///
                    /// It is alone, so it is a JSON block
                    /// 
                    lin = new Linear(bag.Linear, ast);
                    lin.Op = "call";
                    lin.Name = "[]";
                    lin.Return = setTempReturn();
                    lin.List();

                    bag = bag.subBag();

                    onEnd = delegate
                    {
                        int i = 0;
                        foreach (var code in codes)
                        {
                            var res = bag.Params.Pull(0).StrValue;

                            var li = new Linear(bag.Linear, code.ast);
                            li.Op = "call";
                            li.Type = "[]";
                            li.Name = lin.Return;
                            li.Attributes.Add(i++.ToString());
                            li.Attributes.Add(res);
                            li.List();
                        }
                    };
                }
                else
                {
                    ///
                    /// Pointers
                    ///
                    var left = bag.Params.Pull(false);
                    left.isArray = true;
                    left.isArrayAssign = IsToAssign;

                    bag = bag.subBag();

                    onEnd = delegate
                    {
                        var pars = bag.Params;

                        //Get type
                        var t = "[";
                        for (int i = 1; i < pars.Count; i++) t += ",";
                        t += "]";
                        pars.Type = t;

                        // Convert in call
                        if (!IsToAssign || (IsToAssign && !(Right?.IsAssign ?? true)))
                        {
                            var lin = new Linear(bag.Linear, ast);
                            lin.Op = "call";
                            lin.Type = t;
                            lin.Name = left.StrValue;
                            left.StrValue = lin.Return = setTempReturn(false);
                            lin.List();

                            foreach (var val in pars)
                            {
                                lin.Attributes.Add(val.StrValue);
                            }
                        }
                        else
                        {
                            // or leave it as reference (for assign)
                            left.AsParams.New(new Parameter(pars));
                        }
                    };

                }
            }

            bool isJsonObject;
            void toLinear_ground_block_brackets()
            {
                var completedStatement = firstStatement.GetCompletedStatement;
                var csType = completedStatement.Type;

                if (csType == "Declaration.Function" || csType == "Block")
                {
                    bag = bag.subBag();
                }
                else
                {
                    if (IsAssignAccumulator)
                    {
                        // Are accumulated attributes
                        bag = bag.subBag(Bag.Status.DeclarationParameters);
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
                        isJsonObject = true;

                        lin = new Linear(bag.Linear, ast);
                        lin.Op = "call";
                        lin.Name = "{}";
                        lin.Return = setTempReturn();
                        lin.List();

                        bag = bag.subBag();

                        bag.OnRepeatable = delegate (OrderedAST oAst)
                        {
                            var key = bag.Params.Pull(0).StrValue;
                            var res = bag.Params.Pull(0).StrValue;

                            var li = new Linear(bag.Linear, oAst.Left.ast);
                            li.Op = "call";
                            li.Type = "{}";
                            li.Name = lin.Return;
                            li.Attributes.Add(key);
                            li.Attributes.Add(res);
                            li.List();
                        };

                        onEnd = delegate
                        {
                            if (LastCode != null)
                                bag.OnRepeatable(LastCode);
                        };
                    }
                }

            }

            void toLinear_ground_block_parenthesis()
            {
                if (IsAssignAccumulator)
                {
                    ///
                    /// Assign Accumulator
                    ///
                    bag = bag.subBag();

                    var mainParams = new Parameters(bag);
                    mainParams.Type = "()=";

                    bag.OnRepeatable = delegate
                    {
                        mainParams.New(new Parameter(bag.Params));
                        bag.Params = new Parameters(bag);
                    };

                    onEnd = delegate
                    {
                        bag.OnRepeatable(null);
                        bag.Parent.Params.New(new Parameter(mainParams));
                    };
                }
                else
                {
                    var completedStatement = firstStatement.GetCompletedStatement;
                    var csType = completedStatement.Type;

                    if (csType == "Declaration.Function")
                    {
                        ///
                        /// This is a good example of different status management
                        ///
                        bag = bag.subBag(Bag.Status.DeclarationParameters);
                        var l = bag.Linear = bag.Linear.SetSettings("parameters", ast);

                        bag.OnRepeatable = delegate (OrderedAST oAst)
                        {
                            var bag = oAst.bag;
                            var p = bag.Params.Pull();
                            if (p != null)
                            {
                                var lin = new Linear(l, oAst.Left.ast);
                                lin.Op = "parameter";
                                lin.Name = p.StrValue;
                                lin.List();

                                if (bag.Params.Count > 0)
                                    lin.Return = bag.Params.Pull().StrValue;
                            }
                        };

                        onEnd = delegate
                        {
                            if(LastCode != null)
                                bag.OnRepeatable(LastCode);
                        };
                    }
                    else if (csType == "Block")
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
                    else if (csType == "Retrieve")
                    {
                        ///
                        /// Here parameters are simply collected from bag.Paramas
                        ///
                        lin = new Linear(bag.Linear, ast);
                        lin.Op = "call";
                        lin.Name = bag.Params.Pull().StrValue;

                        bag = bag.subBag();

                        onEnd = delegate
                        {
                            for (int l = bag.Params.Count - 1; l >= 0; l--)
                            {
                                var val = bag.Params.Pull(0).StrValue;
                                lin.Attributes.Add(val);
                            }

                            bag = bag.Parent;
                            setTempReturn();
                        };
                    }
                    else if(Right.IsAttribute && !Right.IsDotAttribute && codes.Count==1 && LastCode.IsAttribute)
                    {
                        //it's a cast
                        lin = new Linear(bag.Linear, ast);
                        lin.Op = "cast";

                        relaxDontListIt = true;

                        onEnd = delegate
                        {
                            lin.Name = bag.Params.Pull().StrValue;
                        };

                        Parent.onPreEnds.Add(delegate
                        {
                            lin.Return = bag.Params.Pull(false).StrValue;
                            lin.List();
                        });
                    }
                }
            }

            #endregion

            #region toLinear_ground_instruction
            void toLinear_ground_instruction()
            {
                attachStatement();

                if (IsOperator)
                    toLinear_ground_instruction_operator();
                else
                    toLinear_ground_instruction_default();
            }

            #region assignIf
            bool isAssignIf = true;
            bool inAssignIf
            {
                get
                {
                    if (isAssignIf)
                        return true;

                    if (Parent != null)
                        return Parent.inAssignIf;

                    return false;
                }
            }
            #endregion

            #region cascadeExecution

            bool preventCascadeExecution = false;
            public void cascadeExecution()
            {
                if(Parent != null)
                {
                    if (!Parent.preventCascadeExecution && Parent.ast.type == AST.Type.Instruction)
                    {
                        Parent.callEnd();
                        Parent.cascadeExecution();
                    }
                }
            }

            #endregion
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
                        toLinear_ground_instruction_operator_assign();
                        break;

                    case ":":
                        // Use cases:
                        // 1. JSON Object: { key: value }
                        // 2. Function call test(key1: value, key2: value)
                        // 3. Short if operator x ? y : z
                        cascadeExecution();

                        break;

                    case "?":
                        // assignIf
                        isAssignIf = true;
                        preventCascadeExecution = true;

                        if (!(Left?.Parent != null)) //todo EndsWith(":")
                            throw new Exception("Marcello, what is it?");

                        Left.Parent.callEnd(); // Execute calculation
                        var condition = bag.Params.Pull().StrValue;

                        lin = new Linear(bag.Linear, ast);
                        lin.Op = "assignIf";
                        lin.Name = condition;

                        onEnd = delegate
                        {
                            lin.Attributes.Add(bag.Params.Pull(-2).StrValue);
                            lin.Attributes.Add(bag.Params.Pull(-1).StrValue);
                            setTempReturn();
                        };

                        break;

                    default:
                        lin = new Linear(Parent.bag.Linear, ast);
                        lin.Op = Subject;
                        lin.Attributes.Add(bag.Params.Pull().StrValue);

                        onEnd = delegate
                        {
                            lin.Attributes.Add(bag.Params.Pull().StrValue);
                            setTempReturn();
                        };

                        if (codes.Count == 2)
                        {
                            // ie operators have priority against conjunctions
                            if (LastCode.IsConjunction && LastCode.OperatorPriority > OperatorPriority)
                            {
                                onAfterChildCycle = delegate (OrderedAST ast)
                                {
                                    callEnd();
                                    onAfterChildCycle = null;
                                };
                            }
                        }

                        break;
                }
            }
            void toLinear_ground_instruction_operator_assign()
            {
                // Don't pull because the name could be used by previous instruction
                var pull = bag.Params.Pull();
                var names = pull.AsAccumulator;
                //attachStatement();

                onEnd = delegate
                {
                    var attr = bag.Params.Pull(false).StrValue;

                    foreach (var parName in names)
                    {
                        if (parName.isArray)
                        {
                            lin = new Linear(Parent.bag.Linear, ast);
                            lin.Op = "call";
                            lin.Name = parName.StrValue;
                            lin.List();

                            foreach(var index in parName.AsParams)
                                lin.Attributes.Add(index.StrValue);

                            lin.Attributes.Add(attr);
                        }
                        else
                        {
                            lin = new Linear(Parent.bag.Linear, ast);
                            lin.Op = Subject;
                            lin.Name = parName.StrValue;
                            lin.List();
                            lin.Attributes.Add(attr);
                        }
                    }
                };
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

            #endregion

            #endregion

            void attachStatement(Statement statement = null)
            {
                if (statement != null)
                    _statement = statement;
                else
                    _statement = new Statement(this);

                onSchedulerPostCode = _statement.OnPostCode;
                onPostEnds.Add(_statement._onPostEnd);               
            }

            // Classe usa e getta
            public class Statement 
            {
                /* To think:
                    - Replace Disks.RemoveAt with a bool parameter "removed" for performance reasons?
                */
                Statement parent;
                OrderedAST parentOrderedAST, curderedAST;
                List<Statement> Disks = new List<Statement>();

                OnCallback OnRepeteable;

                List<OnScheduler> Scheduler = new List<OnScheduler>();
                int SchedulerPos = 0;

                List<Linear> Barrel = new List<Linear>();

                bool killed = false;
                public Statement(OrderedAST oast)
                {
                    parentOrderedAST = oast;

                    AddDisk(new Import());
                    AddDisk(new Namespace());
                    AddDisk(new Declaration());
                    AddDisk(new Block());
                    AddDisk(new Retrieve());
                }

                private Statement() { }

                

                #region Properties

                bool _imCompleted = false;
                internal bool ImCompleted
                {
                    get
                    {
                        foreach (var d in Disks)
                            if (d.ImCompleted)
                                return true;

                        return !killed && (_imCompleted || SchedulerPos >= Scheduler.Count);
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
                internal bool OnPostCode(OrderedAST orderedAST)
                {
                    curderedAST = orderedAST;

                    if (Scheduler.Count > SchedulerPos)
                    {
                        var res = Scheduler[SchedulerPos].Invoke(orderedAST);
                        if (res && Scheduler.Count <= SchedulerPos) CheckDisksTurn();
                        return res;
                    }

                    int d = 0;
                    for (; d < Disks.Count; d++)
                    {
                        var disk = Disks[d];
                        if (!Disks[d].OnPostCode(orderedAST))
                        {
                            Disks[d].killed = true;
                            Disks.RemoveAt(d--);
                            //if(d == 0 && ImCompleted)LastSurvivor();
                        }
                    }

                    OnRepeteable?.Invoke();

                    return ImCompleted || Disks.Count > 0;
                }
                internal void _onPostEnd()
                {
                    foreach (var disk in Disks)
                        if (disk.ImCompleted)
                            disk._onPostEnd();

                    PullAbsorbedParams();
                }

                #region Params
                // Not particularly efficient
                List<Parameter> pulledParameters = new List<Parameter>();
                internal Parameter Pull(int Pos=-1, bool Remove = true)
                {
                    if (!Remove)
                        return OrderedAST.bag.Params.Pull(Pos, false);

                    Parameter par;
                    while (pulledParameters.Contains(par = OrderedAST.bag.Params.Pull(Pos, false)) && par != null)
                        Pos += Pos < 0 ? -1 : 1;

                    if(par != null)
                        pulledParameters.Add(par);

                    return par;
                }
                internal void ReversePull(Parameter ToReverse = null)
                {
                    if (ToReverse == null)
                        ToReverse = pulledParameters.Pull();
                    else
                        pulledParameters.Remove(ToReverse);
                }
                #endregion
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

                /// <summary>
                /// This parameter handling can be exaggerated, since at each cycle in a block the Params are cleaned
                /// </summary>
                void PullAbsorbedParams()
                {
                    foreach (var par in pulledParameters)
                        OrderedAST.bag.Params.Remove(par);
                }

                void NextScheduler()
                {
                    SchedulerPos++;
                }

                #endregion

                #region Statements
                public class Retrieve : Statement
                {
                    string Name;
                    public Retrieve()
                    {
                        Scheduler.Add(scheduler_0);
                        Scheduler.Add(scheduler_1);
                    }

                    bool scheduler_0(OrderedAST ast)
                    {
                        // Cast exception
                        if (ast.IsBlockParenthesis)
                            return true;

                        if (ast.IsAttribute)
                        {
                            NextScheduler();
                            ImCompleted = true;
                            var pull = Pull(Remove: false);
                            //Name = pull.StrValue;

                            return true;
                        }

                        return false;
                    }

                    bool scheduler_1(OrderedAST ast)
                    {
                        var bag = OrderedAST.bag;

                        // Is array call
                        if (ast.IsBlockSquareBrackets)
                        {
                            return true;
                        }

                        if (ast.IsAttribute)
                            return false;

                        return true;
                    }
                }
                public class Block : Statement
                {
                    string[] Blocks = new string[] { "if", "else", "for" };
                    //string[] BlocksWithoutParameters = new string[] { "else" };

                    string Name;
                    public Block()
                    {
                        Scheduler.Add(scheduler_0);
                    }
                    
                    // Name
                    bool scheduler_0(OrderedAST ast)
                    {
                        var bag = OrderedAST.bag;

                        if (!bag.HasNewParam)
                            return false;

                        Name = bag.Params.LastParam.StrValue;

                        bool isBlock = Blocks.Contains(Name);
                        
                        //if(!isBlock && (isBlock = BlocksWithoutParameters.Contains(Name))) IncreasePos();
                      
                        if (isBlock)
                        {
                            NextScheduler();
                            Monopoly();
                            
                            // Parameters checking
                            var parenthesis = ast.Right;
                            var brackets = parenthesis?.Right;

                            if (!parenthesis.IsBlockParenthesis)
                            {
                                brackets = parenthesis;
                                parenthesis = null;
                            }

                            if (!brackets.IsBlockBrackets)
                                brackets = null;

                            // Check if it's a short block
                            if (brackets == null) //is short block
                                OrderedAST.NewInstructionFromChild((parenthesis ?? ast).Right);

                            // Create linear
                            var lin = new Linear(OrderedAST.bag.Linear, OrderedAST.ast);
                            lin.Op = Name;
                            lin.Type = "block";                            
                            lin.List();

                            var b = OrderedAST.bag = OrderedAST.bag.subBag();
                            b.Linear = lin;

                            OrderedAST.onPreEnds.Add(delegate
                            {
                                OrderedAST.bag = OrderedAST.bag.Parent;
                            });
                        }

                        return isBlock;
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
                        var bag = OrderedAST.bag;

                        if (!bag.HasNewParam)
                            return false;

                        var spar = Pull().StrValue;
                        if (spar == null) return false;

                        if (spar == "import")
                        {
                            NextScheduler();
                            Monopoly();

                            return true;
                        }

                        return false;
                    }

                    bool scheduler_1(OrderedAST ast)
                    {
                        var bag = OrderedAST.bag;

                        if (!bag.HasNewParam)
                            return false;

                        var spar = Pull().StrValue;

                        Name = spar;

                        NextScheduler();

                        lin = new Linear(bag.Linear, ast.ast);
                        lin.Op = "import";
                        lin.Name = Name;
                        lin.List();

                        return true;
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

                            var spar = Pull().StrValue;

                            if (Parameters.Contains(spar)) // spar or ast.Subject in this case have the same value
                            {
                                parameter = spar;
                                NextScheduler();

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
                                NextScheduler();

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
                        var bag = OrderedAST.bag;

                        if (!bag.HasNewParam)
                            return false;

                        var spar = Pull().StrValue;

                        if (spar == "namespace")
                        {
                            NextScheduler();
                            Monopoly();  

                            return true;
                        }

                        return false;
                    }

                    bool scheduler_1(OrderedAST ast)
                    {
                        var bag = OrderedAST.bag;

                        if (!bag.HasNewParam)
                            return false;

                        var spar = Pull().StrValue;

                        Name = spar;

                        ImCompleted = true;
                        NextScheduler();

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
                            NextScheduler();
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
                        var bag = OrderedAST.bag;

                        if (!ast.IsAttribute)
                            return false;

                        var spar = Pull().StrValue;

                        if (Light.Modifiers.Contains(spar))
                        {
                            Modifier = spar;
                        }
                        else // with an else if you could put differnt type of modifier without enforcing the order of entry
                        {
                            ReversePull();
                            return Scheduler[++SchedulerPos].Invoke(ast);
                        }

                        return true;
                    }

                    // Type
                    bool scheduler_1(OrderedAST ast)
                    {
                        var bag = OrderedAST.bag;

                        if (!ast.IsAttribute)
                            return false;

                        var spar = Pull().StrValue;

                        DeclType = spar;              
                        NextScheduler();

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
                        var bag = OrderedAST.bag;

                        // Type array definition
                        if (ast.IsBlockSquareBrackets)
                        {
                            var par = Pull();
                            if (par == null) return false;
                            var values = par.Values;

                            if (values.Length == 0)
                                DeclType += "[]";
                            else
                                return false; // for the moment parameters are not excepted in type declaration

                            return true;
                        }

                        if (!ast.IsAttribute)
                            return false;

                        // This instruction supports repeated instructions (ie int var1, var2)
                        OrderedAST.bag.OnRepeatable = delegate(OrderedAST ast)
                        {
                            // so attach this statement to repeated function
                            ast.attachStatement(this);
                            SchedulerPos = 2;
                        };

                        var parNames = Pull();
                        if (parNames == null) return false;

                        Names = parNames.Values;

                        /// Completed!
                        NextScheduler();

                        // Saves name param for next instruction
                        if (ast.Right?.IsOperator ?? false && false) //tothink
                        {
                            bag.Params.New(parNames);
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
                                NextScheduler();
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
