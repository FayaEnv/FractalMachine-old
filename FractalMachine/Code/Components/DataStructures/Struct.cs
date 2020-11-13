using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class Struct : DataStructure
    {
        public Struct(Component parent, string name, Linear linear) : base(parent, name, linear)
        {
            dataStructureType = DataStructureTypes.Struct;
        }

        #region ReadLinear

        public override void ReadLinear(Linear lin)
        {
            for (int i = 0; i < lin.Instructions.Count; i++)
            {
                var instr = lin[i];
                instr.Pos = i;

                switch (instr.Op)
                {
                    case "declare":
                        readLinear_declare(instr);
                        break;

                    default:
                        throw new Exception("Operation not permitted");
                }
            }
        }

        #endregion

    }
}
