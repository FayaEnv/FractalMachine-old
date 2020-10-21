using FractalMachine.Classes;
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
        Light.OrderedAST orderedAst;
        Linear Linear;

        public static CPP OpenFile(string FileName)
        {
            var text = System.IO.File.ReadAllText(FileName);
            var light = new CPP();
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

        public override string Language
        {
            get { return "CPP"; }
        }

        public abstract class Writer
        {
            Writer parent;
            string content = "";
            List<Writer> writers = new List<Writer>();

            #region Write
            public void Write(string toWrite)
            {
                content += toWrite;
            }

            public void NewLine()
            {
                Write("\r\n");
            }

            void Reset()
            {
                content = "";
            }

            #endregion

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

            #region Subclasses

            public class Main : Writer
            {
                public Main() { }
                public Main(Main Parent)
                {
                    parent = Parent;
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
            }

            public class Function : Writer
            {
                string[] attributes, parameters;
                string type, name;

                public Function(Linear Linear)
                {
                    // Calculate parameters
                    var linParams = Linear.Settings[0];
                    var instrs = linParams.Instructions;
                    var param = new string[instrs.Count];
                    for (int p = 0; p < param.Length; p++)
                    {
                        param[p] = instrs[p].Name;
                    }

                    parameters = param;

                    // The rest
                    attributes = Linear.Attributes.ToArray();
                    type = Linear.Return;
                    name = Linear.Name;

                    // Type
                    if (type == "function")
                        type = "void"; //todo: var
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
                        content += w.Compose();
                        NewLine();
                    }

                    Write("}");
                }
            }

            public class Call : Writer
            {
                string name;
                string[] parameters;

                public Call(Linear Linear)
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

                    var funLin = funComp.linear;
                    var Params = funLin.Settings[0];
                    parameters = new string[args.Count];

                    var p = 0;
                    foreach (var par in Params.Instructions)
                    {
                        var arg = args[p];

                        string type = "var";
                        par.Parameters.TryGetValue("type", out type);

                        if (arg.HasStringMark())
                            parameters[p] = '"' + arg.NoMark() + '"';
                        else
                            parameters[p] = arg;

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

                public Import(Linear lin, Component comp)
                {
                    string path = lin.Attributes[0].NoMark();

                    // Check for linking
                    string link;
                    if (comp.importLink.TryGetValue(path, out link))
                        path = link;

                    // Check for compiled resource
                    comp.importedComponents.TryGetValue(path, out impComp);
                }

                internal override void Output()
                {
                    Reset();

                    if(impComp.called)
                    {
                        Write("#include \"");
                        Write(impComp.WriteLibrary());
                        Write("\"");
                    }                 
                }
            }

            #endregion
        }
    }
}
