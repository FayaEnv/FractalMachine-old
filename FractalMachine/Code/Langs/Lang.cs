using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Langs
{
    public abstract class Lang
    {
        public abstract AST.OrderedAst GetOrderedAst();
        public abstract Linear GetLinear();
    }
}
