using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class Struct : Container
    {
        public Struct(Component parent, Linear linear) : base(parent, linear)
        {
            //containerType = ContainerTypes.Class;
        }

        public override void ReadSubLinear(Linear instr)
        {
            switch (instr.Op)
            {
                case "declare":
                    readLinear_declare(instr);
                    break;

                default:
                    throw new Exception("Operation not permitted");
            }
        }

        public override string WriteTo(Lang Lang)
        {
            throw new NotImplementedException();
        }

    }
}
