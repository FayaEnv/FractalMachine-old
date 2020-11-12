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
            ///
            /// Special treatments
            ///

            // Temporary
            if(_linear.Op == "import")
            {
                TopFile.Include(Lang, _linear.component.attached);
                return "";
            }

            ///
            /// The others
            ///

            var ts = Lang.GetTypesSet;

            // Handle return
            if (!String.IsNullOrEmpty(_linear.Return)) // is it has a sense?
            {
                // Get return type
                var var = Parent.ivarMan.Get(_linear.Return);
                if (var != null)
                {
                    if (var.IsUsed(_linear))
                    {
                        var.setRealVar(Lang);
                        if (!String.IsNullOrEmpty(var.realVarType))
                        {
                            writeToCont(var.realVarType);
                            writeToCont(" ");
                        }
                        writeToCont(var.realVarName);
                        writeToCont("=");
                    }
                }
                else
                {
                    writeToCont(_linear.Return);
                    writeToCont("=");
                }
            }

            if(_linear.HasOperator)
            {
                if (_linear.Op == "=")
                {
                    var attrType = ts.GetAttributeType(_linear.Name);
                    writeToCont(ts.SolveAttributeType(attrType));
                }
                else
                {
                    string t1 = null, t2 = _linear.Attributes[0];

                    if (_linear.Op != "!")
                    {
                        t1 = t2;
                        t2 = _linear.Attributes[1];
                    }

                    if (t1 != null)
                        writeAttributeVariable(ts, t1);

                    writeToCont(_linear.Op);
                    writeAttributeVariable(ts, t2);
                }
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

                    writeAttributeVariable(ts, attr);

                    if (a < ac-1)
                        writeToCont(",");
                }

                writeToCont(")");

            }

            writeToCont(";");

            return writeReturn();
        }

        void writeAttributeVariable(TypesSet ts, string attr)
        {
            var attrType = _linear.Lang.GetTypesSet.GetAttributeType(attr);
            if (attrType.Type == AttributeType.Types.Name)
            {
                var var = Parent.ivarMan.Get(attr);
                if (var != null)
                    writeToCont(var.realVarName);
                else
                    writeToCont(attr);
            }
            else
            {
                // Get type and convert attribute
                var val = ts.SolveAttributeType(attrType);
                writeToCont(val);
            }
        }
    }
}
  