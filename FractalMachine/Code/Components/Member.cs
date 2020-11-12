using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    class Member : Component
    {
        MemberType memberType;

        internal bool typeToBeDefined = false;

        public Member(Component parent, Linear linear):base(parent, linear)
        {
            type = Types.Member;
            memberType = MemberType.Normal;
        }

        public enum MemberType
        {
            Normal // Variable
            //for the future: Properties
        }

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
