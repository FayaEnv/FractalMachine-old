using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class Operation : Component
    {
        public Operation(Container parent, Linear linear) : base(parent, linear)
        {
            type = Types.Operation;
        }
    }
}
