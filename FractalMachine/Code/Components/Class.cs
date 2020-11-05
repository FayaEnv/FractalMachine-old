using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class Class : Container
    {
        public Class(Component parent, Linear linear) : base(parent, linear)
        {
            type = Types.Class;
        }

        #region Writer 

        public override string WriteTo(Lang.Settings LangSettings)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
