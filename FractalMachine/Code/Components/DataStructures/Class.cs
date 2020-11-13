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
            Constructor = (Function)Solve(name);
        }

        #region ReadLinear

        #endregion
    }
}