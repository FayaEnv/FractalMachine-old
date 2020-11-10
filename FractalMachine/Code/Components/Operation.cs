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

        override public string WriteTo(Lang Lang)
        {
            if(_linear.HasOperator)
            {

            }
            else if (_linear.IsCall)
            {
                var toCall = Parent.Solve(_linear.Name);

                writeToCont(toCall.GetName(parent));
                writeToCont("(");

                // Write parameters
                var ac = _linear.Attributes.Count;
                for (var a=0; a<ac; a++)
                {
                    var attr = _linear.Attributes[a];

                    var attrType = _linear.Lang.GetTypesSet.GetAttributeType(attr);
                    if (attrType.Type == AttributeType.Types.Name)
                    {
                        var var = Parent.ivarMan.HandleVarName(attr);
                    }
                    else
                    {
                        // Get type and convert attribute
                        var ts = Lang.GetTypesSet;
                        var val = ts.SolveAttributeType(attrType);
                        writeToCont(val);
                    }

                    if(a < ac-1)
                        writeToCont(",");
                }

            }

            return writeReturn();
        }
    }
}
  