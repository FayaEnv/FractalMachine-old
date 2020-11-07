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

        internal override void readLinear_declare(Linear instr)
        {
            //todo
        }

        internal override void readLinear_operation(Linear instr)
        {
            //todo
        }

        internal override void readLinear_call(Linear instr)
        {
            //todo
        }

        internal override void readLinear_import(Linear instr)
        {
            Import(instr.Name, instr.Parameters);
        }

        internal override void readLinear_function(Linear instr)
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

        internal override void readLinear_namespace(Linear instr)
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
