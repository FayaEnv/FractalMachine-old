using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class InternalVariablesManager
    {
        Dictionary<int, InternalVariable> vars = new Dictionary<int, InternalVariable>();

        public InternalVariablesManager()
        {
        }

        public InternalVariable Set(string ivar, Operation op)
        {
            int n = getN(ivar);
            var iv = new InternalVariable(op);
            vars.Add(n, iv);
            return iv;
        }

        public void ReverseSet(string ivar, Type t)
        {
            int n = getN(ivar);
            var iv = vars[n];
            iv.type = t;
        }

        int getN(string ivar)
        {
            return Convert.ToInt32(ivar.Substring(Properties.InternalVariable.Length));
        }

        public class InternalVariable
        {
            public Operation op;
            public Type type;

            public InternalVariable(Operation Op)
            {
                op = Op;
                type = op.returnType;
            }
        }
    }
}
