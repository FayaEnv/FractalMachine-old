using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    class Function : Component
    {
        internal List<Overload> overloads = new List<Overload>();

        public Function(Component parent, Linear linear) : base(parent, linear)
        {
            type = Types.Function;
        }

        public Container Parent
        {
            get { return (Container)parent; }
        }

        #region Read

        internal void addOverload(Linear instr)
        {

        }

        #endregion

        #region Writer 

        public override string WriteTo(Lang.Settings LangSettings)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    class Overload : Container
    {
        public Overload(Container parent, Linear linear) : base(parent, linear)
        {
            containerType = ContainerTypes.Overload;
        }

        new public Function Parent
        {
            get
            {
                if (parent.type != Types.Member)
                    return ((Overload)parent).Parent;

                return (Function)parent;
            }
        }

        #region ReadLinear

        internal override void readLinear_function(Linear instr)
        {
            //is a nested function
            //write the function as "brother of container"
            throw new Exception("todo");
        }

        #endregion

        #region Writer 

        override internal void writeToCont(string str)
        {
            wtCont += str;
        }

        public override string WriteTo(Lang.Settings LangSettings)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
