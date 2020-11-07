using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    class Member : Component
    {
        Type LangType;
        MemberType memberType;

        public Member(Component parent, Linear linear):base(parent, linear)
        {
            type = Types.Member;
            memberType = MemberType.Normal;
        }

        public enum MemberType
        {
            Normal // Variable
        }
    }
}
