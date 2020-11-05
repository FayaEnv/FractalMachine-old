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

using FractalMachine.Classes;
using FractalMachine.Code.Components;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace FractalMachine.Code.Langs
{
    public class CPP : Lang
    {
        AST AST;
        Linear Linear;

        public static CPP OpenFile(string FileName)
        {
            var text = System.IO.File.ReadAllText(FileName);
            var light = new CPP();

            // Temporary replacing
            text = text.Replace("::", ".");

            light.Parse(text);
            return light;
        }

        public void Parse(string Script)
        {
            var amanuensis = new Light.Amanuensis();
            amanuensis.Read(Script);
            AST = amanuensis.GetAST;

            //orderedAst = Light.OrderedAst.FromAST(AST);
        }

        public override Linear GetLinear()
        {
            var oAst = new Light.OrderedAST(AST);
            return Linear = oAst.ToLinear();
        }

        public override Language Language
        {
            get { return Language.CPP; }
        }

        override public Code.TypesSet GetTypesSet
        {
            get
            {
                throw new Exception("Todo");
                return null;
            }
        }

        #region Settings

        public class Settings : Lang.Settings
        {
            public override string EntryPointFunction
            {
                get
                {
                    return "main";
                }
            }

            public override string OpenBlock
            {
                get
                {
                    return "{";
                }
            }

            public override string CloseBlock
            {
                get
                {
                    return "}";
                }
            }

            public override string StructureImport
            {
                get
                {
                    return "#include \"$%PATH\"";
                }
            }

        }

        static Settings _settings;

        public override Lang.Settings GetSettings
        {
            get
            {
                if (_settings == null)
                    _settings = new Settings();

                return _settings;
            }
        }

        #endregion

        public abstract class Writer
        {
            Linear linear;
            Writer parent;
            string content = "";
            List<Writer> writers = new List<Writer>();

            public Writer(Writer Parent, Linear Linear)
            {
                parent = Parent;
                if (Parent != null)
                    Parent.writers.Add(this);

                linear = Linear;
                Linear.DebugLine = LineNumber;

            }

            #region Write
            public void Write(string toWrite)
            {
                content += toWrite;
            }

            public void NewLine()
            {
                Write("\r\n");
                LineNumber++;
            }

            void Reset()
            {
                content = "";
            }

            #endregion

            internal virtual int LineNumber
            {
                get
                {
                    return parent.LineNumber;
                }

                set
                {
                    parent.LineNumber = value;
                }
            }

            public string Compose()
            {
                Output();
                return content;
            }

            internal abstract void Output();

            public Writer Add(Writer writer)
            {
                writer.parent = this;
                writers.Add(writer);
                return writer;
            }

            internal string ReadAttribute(string Attribute)
            {
                if (Attribute.HasStringMark())
                    return '"'+Attribute.NoMark() + '"';

                return Attribute;
            }

       

            #region Subclasses

            /*public class Main : Writer
            {
                File context;
                int _lineNumber = 1;

                public Main(File context, Linear linear):base(null, linear) 
                {
                    this.context = context;
                }

                internal override void Output()
                {
                    Reset();

                    foreach (var writer in writers)
                    {
                        Write(writer.Compose());
                        NewLine();
                    }
                }

                internal override int LineNumber
                {
                    get
                    {
                        return _lineNumber;
                    }

                    set
                    {
                        _lineNumber = value;
                    }
                }
            }

            public class Using : Writer
            {
                string us;

                public Using(Writer Writer, Linear Linear, string Using):base(Writer, Linear)
                {
                    us = Using;
                }

                internal override void Output()
                {
                    Reset();

                    Write("using " + us + ";");

                }
            }

            public class Function : Writer
            {
                string[] attributes, parameters;
                string type, name;

                public Function(Writer Parent, Linear Linear):base(Parent, Linear)
                {
                    // Calculate parameters
                    var linParams = Linear.Settings["parameters"];
                    var instrs = linParams.Instructions;
                    var param = new string[instrs.Count];
                    for (int p = 0; p < param.Length; p++)
                    {
                        var instr = instrs[p];
                        string par = "";

                        if (!String.IsNullOrEmpty(instr.Return))
                            par = instr.Return + " ";

                        par += instr.Name;

                        param[p] = par;
                    }

                    parameters = param;

                    // The rest
                    attributes = Linear.Attributes.ToArray();
                    type = Linear.Return;
                    name = Linear.Name;

                    // Type
                    if (type == "function")
                    {
                        type = "void"; //todo: var
                        if (name == "main" && Linear.component.Top.Type == Component.Types.File) 
                            type = "int";
                    }

                    Linear.component.WriteToCpp(this);
                }

                internal override void Output()
                {
                    Reset();

                    foreach (var attr in attributes)
                    {
                        Write(attr + " ");
                    }

                    Write(type + " ");
                    Write(name + " ");

                    Write("(");
                    for (int p = 0; p < parameters.Length; p++)
                    {
                        Write(parameters[p]);
                        if (p < parameters.Length - 1) Write(", ");
                    }
                    Write(")");

                    Write("{");
                    NewLine();

                    foreach (var w in writers)
                    {
                        Write(w.Compose());
                        NewLine();
                    }

                    Write("}");
                }
            }

            public class Call : Writer
            {
                string name;
                string[] parameters;

                public Call(Writer Parent, Linear Linear) : base(Parent, Linear)
                {
                    name = Linear.Name;
                    var args = Linear.component.push;

                    Component funComp = null;
                    try
                    {
                        funComp = Linear.component.Solve(name);
                    }
                    catch (Exception ex)
                    {
                        // todo
                        throw ex;
                    }

                    funComp.Top.called = true;

                    var funLin = funComp.Linear;
                    var Params = funLin.Settings["parameters"];
                    parameters = new string[args.Count];

                    var p = 0;
                    foreach (var par in Params.Instructions)
                    {
                        //todo: if arg is not setted
                        var arg = args[p];

                        string type = "var";
                        par.Parameters.TryGetValue("type", out type);
                        parameters[p] = ReadAttribute(arg);

                        p++;
                    }
                }

                internal override void Output()
                {
                    Reset();
                    Write(name);
                    Write("(");

                    for (int p=0; p<parameters.Length; p++)
                    {
                        Write(parameters[p]);
                        if (p < parameters.Length - 1)
                            Write(",");
                    }

                    Write(");");
                }
            }

            public class Import : Writer
            {
                Component impComp;

                public Import(Writer Parent, Linear lin, Component comp) : base(Parent, lin)
                {
                    string path = lin.Name.NoMark();

                    // Check for linking

                    if (!comp.importLink.TryGetValue(path, out impComp))
                    {
                        throw new Exception("Oops");
                    }
                }

                internal override void Output()
                {
                    Reset();

                    if(impComp.Called)
                    {
                        Write("#include \"");
                        Write(impComp.WriteLibrary());
                        Write("\"");
                    }                 
                }
            }

            public class Namespace : Writer
            {
                string name;
                public Namespace(Writer Parent, Linear lin) : base(Parent, lin)
                {
                    name = lin.Name;
                    lin.component.WriteToCpp(this);
                }

                internal override void Output()
                {
                    Reset();

                    Write("namespace ");
                    Write(name);
                    Write("{");
                    NewLine();

                    foreach (var w in writers)
                    {
                        Write(w.Compose());
                        NewLine();
                    }

                    Write("}");
                }
            }
            */
            #endregion

        }
    }
}
