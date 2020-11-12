using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class Class : Container
    {
        Function Constructor;

        public Class(Component parent, Linear linear) : base(parent, linear)
        {            
            containerType = ContainerTypes.Class;

            Constructor = new Function(this);
        }

        #region Writer 

        public override string WriteTo(Lang Lang)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}