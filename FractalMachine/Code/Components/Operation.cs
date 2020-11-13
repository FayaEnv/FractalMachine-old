using FractalMachine.Classes;
using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class Operation : Component
    {
        internal bool disabled = false;
        internal InternalVariablesManager.InternalVariable returnVar;

        public Operation(Container parent, Linear linear) : base(parent, null, linear)
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
            if (disabled)
                return "";

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
                ///
                /// Get return type
                /// 
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

            ///
            /// Operation analyzing
            ///
            if(_linear.HasOperator)
            {
                if (_linear.Op == "=")
                {
                    writeAttributeVariable(ts, _linear.Name, returnType);
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
                        writeAttributeVariable(ts, t1, returnType);

                    writeToCont(_linear.Op);
                    writeAttributeVariable(ts, t2, returnType);
                }
            }
            else if (_linear.IsCall)
            {
                var toCall = Parent.Solve(_linear.Name);

                writeToCont(toCall.GetRealName(parent));
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
            else if (_linear.IsCast)
            {
                throw new Exception("todo");
            }

            writeToCont(";");

            return writeReturn();
        }

        void checkAttributeTypeAccessibility(AttributeType attrType)
        {
            if (attrType.Type == AttributeType.Types.Name)
            {
                // Check modifier
                var v = Solve(attrType.AbsValue);
                if (!v.IsPublic && !v.CanAccess(Parent))
                    throw new Exception("Variable is inaccessible");
            }
        }

        void writeAttributeVariable(TypesSet ts, string attr, Type requestedType = null)
        {
            var attrType = _linear.Lang.GetTypesSet.GetAttributeType(attr);
            checkAttributeTypeAccessibility(attrType);

            if (attrType.Type == AttributeType.Types.Name)
            {
                checkAttributeTypeAccessibility(attrType);
                if (attrType.AbsValue.IsInternalVariable())
                {
                    var iv = Parent.ivarMan.Get(attrType.AbsValue);
                    writeToCont(iv.realVarName);
                }
                else
                    writeToCont(attrType.AbsValue); //todo: handle complex var tree (ie Namespace.Var)
            }
            else
            {
                writeToCont(ts.SolveAttributeType(attrType, requestedType));
            }
        }

        #region Override

        public override Component Solve(string str, bool DontPanic = false)
        {
            return parent.Solve(str, DontPanic);
        }

        #endregion
    }
}
  