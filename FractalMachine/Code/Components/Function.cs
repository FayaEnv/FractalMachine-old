using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    class Function : Container
    {
        public Function(Container parent, Linear linear) : base(parent, linear)
        {
            type = Types.Function;
        }

        new public Member Parent
        {
            get { return (Member)parent; }
        }

        #region Writer 

        override internal void writeToCont(string str)
        {
            if(parent.type == Types.Function)
            {

            }

            wtCont += str;
        }

        public override string WriteTo(Lang.Settings LangSettings)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
