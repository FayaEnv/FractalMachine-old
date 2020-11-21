using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachineLib.Code.Components
{
    public class Block : Container
    {
        public Block(Function parent, Linear linear) : base(parent, null, linear)
        {
            containerType = ContainerTypes.Block;
        }
    }
}
