using System;
using System.Collections.Generic;
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

    }
}
