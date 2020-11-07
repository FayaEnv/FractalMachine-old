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
            Project,
            File,            
            Class,
            Overload,
            Namespace
        }

        #region ReadLinear

        public void ReadLinear()
        {
            ReadLinear(_linear);
        }

        public virtual void ReadLinear(Linear lin)
        {
            for (int i = 0; i < lin.Instructions.Count; i++)
            {
                var instr = lin[i];

                switch (instr.Op)
                {
                    case "import":
                        readLinear_import(instr);
                        break;

                    case "declare":
                        readLinear_declare(instr);
                        break;

                    case "function":
                        readLinear_function(instr);
                        break;

                    case "namespace":
                        readLinear_namespace(instr);
                        break;

                    case "call":
                        readLinear_call(instr);
                        break;

                    default:
                        if (instr.Type == "operation")
                            readLinear_operation(instr);
                        else
                            throw new Exception("Unexpected instruction");
                        break;
                }
            }
        }

        internal virtual void readLinear_declare(Linear instr)
        {
            var member = new Member(this, instr);

            if (instr.Return == "var")
                member.typeToBeDefined = true;
            else
                member.langType = instr.Lang.GetTypesSet.Get(instr.Return);

            addComponent(instr.Name, member);

        }

        internal virtual void readLinear_operation(Linear instr)
        {

        }

        internal virtual void readLinear_call(Linear instr)
        {
            //todo
        }

        internal virtual void readLinear_import(Linear instr)
        {
            Import(instr.Name, instr.Parameters);
        }

        internal virtual void readLinear_function(Linear instr)
        {
            Function function;

            try
            {
                function = (Function)getComponent(instr.Name);
            }
            catch (Exception ex)
            {
                throw new Exception("Name used for another variable");
            }

            if (function == null)
            {
                function = new Function(this, null);
                addComponent(instr.Name, function);
            }

            function.addOverload(instr);
        }

        internal virtual void readLinear_namespace(Linear instr)
        {
            Components.File ns;

            try
            {
                ns = (Components.File)getComponent(instr.Name);
            }
            catch (Exception ex)
            {
                throw new Exception("Name used for another variable");
            }

            if (ns == null)
            {
                ns = new Components.File(this, null, null);
                addComponent(instr.Name, ns);
            }

            //Execute new linear in component
        }

        #endregion

        #region Import
        public Component Import(string Name, Dictionary<string, string> Parameters)
        {
            /*
            if (ToImport.HasMark())
            {
                /// Import as file/directory name
                //todo: ToImport.HasStringMark() || (angularBrackets = ToImport.HasAngularBracketMark())
                var fname = ToImport.NoMark();
                var dir = libsDir + "/" + fname;
                var c = importFileIntoComponent(dir, Parameters);
                importLink.Add(fname, c);
                //todo: importLink.Add(ResultingNamespace, dir);
            }
            */

            //todo: handle Parameters
            var comp = Solve(Name);

            foreach (var c in comp.components)
            {
                //todo: imported key yet exists
                this.components.Add(c.Key, c.Value);
            }

            return comp;
        }
        #endregion

    }
}
