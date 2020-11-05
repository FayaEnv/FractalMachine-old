using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components 
{
    abstract public class Container : Component
    {
        public Container(Component parent, Linear linear):base(parent, linear)
        {

        }

        public Container Parent
        {
            get { return (Container)parent; }
        }

    }
}
