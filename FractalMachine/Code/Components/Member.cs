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
        }

        public override string WriteTo(Lang Lang)
        {
            return ""; //todo?
        }
    }
}
