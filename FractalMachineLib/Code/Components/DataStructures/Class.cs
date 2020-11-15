using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class Class : DataStructure
    {
        Function Constructor;

        public Class(Component parent, string name, Linear linear) : base(parent, name, linear)
        {            
            dataStructureType = DataStructureTypes.Class;
        }

        #region ReadLinear

        public override void ReadLinear_Operation(Linear lin)
        {
            base.ReadLinear_Operation(lin);
            
            // Extract constructor (for the moment for educational purposes)
            var constr = Solve(name);
            if (constr != null)
                Constructor = (Function)constr;
        }

        #endregion
        }
}