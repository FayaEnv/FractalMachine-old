using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public abstract class DataStructure : Container
    {
        internal DataStructureTypes dataStructureType;

        public DataStructure(Component parent, string name, Linear linear) : base(parent, name, linear)
        {
            containerType = ContainerTypes.DataStructure;
        }

        public enum DataStructureTypes
        {
            Struct,
            Class
        }

        #region Write

        public override string WriteTo(Lang Lang)
        {
            writeToCont(dataStructureType.ToString().ToLower());
            writeToCont(" ");
            writeToCont(name);
            writeToCont("{");
            writeNewLine(_linear);
            base.WriteTo(Lang);
            writeToCont("}");

            return writeReturn();
        }

        #endregion
    }
}
