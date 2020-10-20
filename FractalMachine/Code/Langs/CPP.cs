using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

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

        public class Writer
        {
            Writer parent;
            string content = "";
            List<Writer> writers = new List<Writer>();
            string haveToClose = "";

            public Writer() { }
            public Writer(Writer Parent)
            {
                parent = Parent;
            }

            public Writer Add(Writer writer)
            {
                writer.parent = this;
                writers.Add(writer);
                return writer;
            }

            #region Write
            public void Write(string toWrite)
            {
                content += toWrite;
            }

            public void NewLine(string toWrite)
            {
                Write("\r\n");
            }

            void End()
            {
                Write("}");
            }

            #endregion

            public virtual string Output()
            {
                var o = content;

                foreach(var writer in writers)
                {
                    o += writer.Output();
                }

                o += haveToClose;

                return o;
            }

            #region Subclasses

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
                }

                public override string Output()
                {
                    content = "";

                    foreach (var attr in attributes)
                    {
                        Write(attr + " ");
                    }

                    Write(type + " ");
                    Write(name + " ");

                    for (int p = 0; p < parameters.Length; p++)
                    {
                        Write(parameters[p]);
                        if (p < parameters.Length - 1) Write(", ");
                    }

                    Write("{");

                    foreach (var w in writers)
                        content += w.Output();

                    Write("}");

                    return content;
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

                    var funLin = funComp.linear;
                    var param = funLin.Settings[0];
                }
            }

            #endregion
        }
    }
}
