using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class Operation : Component
    {
        internal InternalVariablesManager.InternalVariable returnVar;

        public Operation(Container parent, Linear linear) : base(parent, linear)
        {
            type = Types.Operation;
        }

        public Container Parent
        {
            get
            {
                return (Container)parent;
            }
        }

        override public string WriteTo(Lang.Settings LangSettings)
        {
            if(_linear.HasOperator)
            {

            }
            else if (_linear.IsCall)
            {

            }

            return writeReturn();
        }
    }
}
  