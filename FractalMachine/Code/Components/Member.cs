using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    class Member : Component
    {
        public Member(Component parent, Linear linear) : base(parent, linear)
        {
            type = Types.Member;
        }

        public Container Parent
        {
            get { return (Container)parent; }
        }

        #region Writer 

        public override string WriteTo(Lang.Settings LangSettings)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
