using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class Class : DataStructure
    {
        Function Constructor;

        public Class(Component parent, Linear linear) : base(parent, linear)
        {            
            dataStructureType = DataStructureTypes.Class;
            Constructor = new Function(this);      
        }

        #region ReadLinear

        #endregion

        #region Writer 

        public override string WriteTo(Lang Lang)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}