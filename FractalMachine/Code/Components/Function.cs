using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class Function : Component
    {
        internal List<Overload> overloads = new List<Overload>();
        internal bool isEntryPoint = false;

        public Function(Component parent, string Name) : base(parent, Name, null)
        {
            type = Types.Function;

            if (name == Properties.EntryPointFunction && TopFile.isMain)
            {
                name = "main"; //for c++ (todo: make it a property reference)
                isEntryPoint = true;
            }
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

        public override string GetRealName(Component relativeTo = null)
        {
            if (parent == null || isEntryPoint) // in case of native functions
                return name;

            //todo: circa (to improve)
            string apex = "";
            if(Parent is File)
                apex = TopFile.GetPath("_") + "_";

            if (Parent is Class)
                return name;
            else
                return apex + name; 

        }

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
        public Overload(Function parent, Linear linear) : base(parent, null, linear)
        {
            containerType = ContainerTypes.Overload;

            ///
            /// Analyze parameters
            /// 
            var pars = linear.Settings["parameters"];
            foreach (var par in pars.Instructions)
            {
                readLinear_declare(par);
            }

            /// Analyze function
            if(Parent.isEntryPoint)
                returnType = linear.Lang.GetTypesSet.Get("int"); 
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
            var cont = Parent.Parent;
            var ts = Lang.GetTypesSet;
            var ots = _linear.Lang.GetTypesSet;

            bool isConstructor = cont is Class && name == cont.name;

            /// Handle return type
            if (!isConstructor)
            {
                if (returnType == null)
                {
                    // temporary way
                    writeToCont("void");
                    //todo: check if there is a return 
                }
                else
                    writeToCont(ts.GetTypeCodeName(returnType));
            }
                
            writeToCont(" ");

            /// Function name
            if (isConstructor)
                writeToCont(cont.GetRealName());
            else
                writeToCont(Parent.GetRealName());

            /// Write parameters
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

            /// Write content
            writeToCont("{");
            writeNewLine(_linear);
            base.WriteTo(Lang, true);
            writeToCont("}");

            return writeReturn();
        }

        #endregion
    }
}
