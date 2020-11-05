using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components 
{
    abstract public class Container : Component
    {
        internal ContainerTypes containerType;

        public Container(Component parent, Linear linear):base(parent, linear)
        {
            type = Types.Container;
        }

        public Container Parent
        {
            get { return (Container)parent; }
        }

        public enum ContainerTypes
        {
            File,
            Class,
            Overload,
            Namespace
        }

    }
}
