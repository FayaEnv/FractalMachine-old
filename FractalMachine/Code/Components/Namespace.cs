using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class Namespace : Container
    {
        public Namespace(Component parent, Linear linear) : base(parent, linear)
        {
            type = Types.Namespace;
        }


        #region Writer 

        public override string WriteTo(Lang.Settings LangSettings)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
