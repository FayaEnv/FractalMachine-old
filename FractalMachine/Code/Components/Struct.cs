using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class Struct : Function
    {
        public Struct(Component parent, Linear linear) : base(parent, linear)
        {
            //containerType = ContainerTypes.Class;
        }
    }
}
