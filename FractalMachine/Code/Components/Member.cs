using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    class Member : Component
    {
        MemberType memberType;

        internal InternalVariablesManager.InternalVariable iVar;
        internal bool typeToBeDefined = false;

        public Member(Component parent, Linear linear):base(parent, linear)
        {
            type = Types.Member;
            memberType = MemberType.Normal;
        }

        public Member(InternalVariablesManager.InternalVariable iVar):base(null, null)
        {
            this.iVar = iVar;
        }

        public enum MemberType
        {
            Normal // Variable
            //for the future: Properties
        }

        #region Modifiers

        public override bool IsPublic
        {
            get
            {
                if (iVar != null)
                    return true;

                return base.IsPublic; 
            }
        }

        #endregion

        public override string WriteTo(Lang Lang)
        {
            var ts = Lang.GetTypesSet;

            switch (_linear.Op)
            {
                case "declare":

                    writeToCont(ts.GetTypeCodeName(returnType));
                    writeToCont(" ");
                    writeToCont(_linear.Name);
                    writeToCont(";");

                    break;
            }

            return writeReturn(); 
        }
    }
}
