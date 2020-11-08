using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace FractalMachine.Code.Components 
{
    abstract public class Container : Component
    {
        internal ContainerTypes containerType;
        internal List<Operation> operations = new List<Operation>();

        public Container(Component parent, Linear linear) : base(parent, linear)
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

                    case "cast":
                        readLinear_cast(instr);
                        break;

                    default:
                        if (instr.Type == "oprt")
                            readLinear_operator(instr);
                        else
                            throw new Exception("Unexpected instruction");
                        break;
                }
            }
        }

        internal virtual void readLinear_cast(Linear instr)
        {

        }

        internal virtual void readLinear_declare(Linear instr)
        {
            var member = new Member(this, instr);

            if (instr.Return == "var")
                member.typeToBeDefined = true;
            else
                member.returnType = instr.Lang.GetTypesSet.Get(instr.Return);

            addComponent(instr.Name, member);

        }

        internal virtual void readLinear_operator(Linear instr)
        {
            var ts = instr.Lang.GetTypesSet;

            var op = new Operation(this, instr);
            operations.Add(op);

            switch (instr.Op)
            {             
                case "=":
                    Member name;
                    var compName = Solve(instr.Name);

                    if (compName == null)
                        throw new Exception(instr.Name + " not declared");

                    try { name = (Member)compName; }
                    catch { throw new Exception(instr.Name + " is not assignable"); }

                    var attr = ts.GetAttributeType(instr.Attributes[0]);
                    if (name.typeToBeDefined)
                    {
                        if (attr.Type == AttributeType.Types.Type)
                        {
                            name.returnType = ts.Get(attr.TypeRef);
                        }
                        else
                        {
                            var attrComp = Solve(attr.AbsValue);
                            name.returnType = attrComp.returnType;
                        }
                    }
                    else
                    {
                        // Else verify that the assign have to be casted
                        // or calculate expected data type ie float test = 1/2 as (float)1/(float)2
                    }

                    break;

                default:
                    string v1, v2;
                    Type t1, t2 = null;

                    v1 = instr.Attributes[0];
                    t1 = solveAttributeType(v1);

                    if (instr.Op != "!")
                    {
                        v2 = instr.Attributes[1];
                        t2 = solveAttributeType(v2);
                    }

                    Type retType = t1;

                    // Study return variable (it's always an InternalVariable)
                    var ret = instr.Return;

                    if (t2 != null)
                        retType = ts.CompareTypeCast(t1, t2);

                    op.returnType = retType;

                    break;
            }
            
        }

        internal virtual void readLinear_call(Linear instr)
        {
            var op = new Operation(this, instr);
            operations.Add(op);
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

        #region Methods

        Type solveAttributeType(string attr)
        {
            var ts = _linear.Lang.GetTypesSet;
            var attrType = ts.GetAttributeType(attr);

            if (attrType.Type == AttributeType.Types.Name)
                return Solve(attr).returnType;
            else
                return attrType.GetLangType;
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
