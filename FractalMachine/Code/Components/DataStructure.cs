using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public abstract class DataStructure : Container
    {
        internal DataStructureTypes dataStructureType;

        public DataStructure(Component parent, Linear linear) : base(parent, linear)
        {
            containerType = ContainerTypes.DataStructure;
        }

        public enum DataStructureTypes
        {
            Struct,
            Class
        }

        #region ReadLinear

        #endregion
    }
}
