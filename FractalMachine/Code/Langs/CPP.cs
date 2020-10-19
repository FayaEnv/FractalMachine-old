using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Langs
{
    public class CPP
    {
        AST AST;
        Light.OrderedAST orderedAst;

        public void Parse(string Script)
        {
            var amanuensis = new Light.Amanuensis();
            amanuensis.Read(Script);
            AST = amanuensis.GetAST;

            //orderedAst = Light.OrderedAst.FromAST(AST);
        }


    }
}
