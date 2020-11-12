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
            var ol = new Overload(this, instr);
            overloads.Add(ol);
        }

        #endregion

        #region Writer 

        public override string WriteTo(Lang LangSettings)
        {
            foreach (var overload in overloads)
                writeToCont(overload.WriteTo(LangSettings));

            return writeReturn();
        }

        #endregion
    }

    class Overload : Container
    {
        public Overload(Function parent, Linear linear) : base(parent, linear)
        {
            containerType = ContainerTypes.Overload;
        }

        new public Function Parent
        {
            get
            {
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

        public override bool Called
        {
            get { return Parent.Called; }
        }

        #region Properties

        #endregion

        #region Writer 

        public override string WriteTo(Lang Lang)
        {
            var ts = Lang.GetTypesSet;
            var ots = _linear.Lang.GetTypesSet;

            // Handle return type
            var ret = _linear.Return;

            if (returnType == null)
            {
                // temporary way
                writeToCont("void");
            }
            else
            {
                writeToCont(ts.GetTypeCodeName(returnType));
            }
                
            writeToCont(" ");
            writeToCont(Parent.name);

            // Write parameters
            var pars = _linear.Settings["parameters"];
            writeToCont("(");
            foreach(var par in pars.Instructions)
            {
                string parType; // by default a dynamic type

                if (String.IsNullOrEmpty(par.Return))
                {
                    throw new Exception("todo: handle dynamic variables");
                }
                else
                {
                    var t = ots.Get(par.Return);
                    parType = ts.GetTypeCodeName(t);
                }

                writeToCont(parType);
                writeToCont(" ");
                writeToCont(par.Name);
            }
            writeToCont(")");

            writeToCont("{");
            writeNewLine(_linear);
            base.WriteTo(Lang, true);
            writeToCont("}");

            return writeReturn();
        }

        #endregion
    }
}
