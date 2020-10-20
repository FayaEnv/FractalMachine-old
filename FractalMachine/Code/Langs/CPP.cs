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

            public Writer() { }
            public Writer(Writer Parent)
            {
                parent = Parent;
            }

            public Writer CreateFunction(string[] Attributes, string Type, string Name, string[] Parameters)
            {
                var ret = new Writer(this);

                foreach (var attr in Attributes)
                {
                    ret.Write(attr + " ");
                }

                ret.Write(Type + " ");
                ret.Write(Name + " ");

                for (int p = 0; p < Parameters.Length; p++)
                {
                    ret.Write(Parameters[p]);
                    if (p < Parameters.Length - 1) ret.Write(", ");
                }

                ret.Write("{");

                return ret;
            }

            public void Write(string toWrite)
            {
                content += toWrite;
            }

            public void NewLine(string toWrite)
            {
                content += "\r\n";
            }
        }
    }
}
