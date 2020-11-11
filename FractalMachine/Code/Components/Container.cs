using FractalMachine.Classes;
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
        internal InternalVariablesManager ivarMan = new InternalVariablesManager();

        public Container(Component parent, Linear linear) : base(parent, linear)
        {
            type = Types.Container;
            ivarMan.parent = this;

            if (linear != null)
            {
                var ots = linear.Lang.GetTypesSet;
                if (String.IsNullOrEmpty(linear.Return))
                    returnType = ots.Get(linear.Return);
            }
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

        public void InjectLinear(Linear instr)
        {
            if (_linear == null)
            {
                _linear = instr;
            }
            else
            {
                foreach (var i in instr.Instructions)
                    _linear.Instructions.Add(i);
            }

            ReadLinear(instr);

            instr.component = this;
        }

        #region ReadLinear

        public override void ReadLinear()
        {
            ReadLinear(_linear);
        }

        public virtual void ReadLinear(Linear lin)
        {
            for (int i = 0; i < lin.Instructions.Count; i++)
            {
                ReadSubLinear(lin, i);
            }
        }

        public virtual void ReadSubLinear(Linear lin, int pos)
        {
            var instr = lin[pos];
            instr.Pos = pos;
            ReadSubLinear(instr);
        }

        public virtual void ReadSubLinear(Linear instr)
        {
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

        internal virtual void readLinear_cast(Linear instr)
        {
            throw new Exception("todo");
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
                    var compName = Solve(instr.Return);

                    if (compName == null)
                        throw new Exception(instr.Return + " not declared");

                    try { name = (Member)compName; }
                    catch { throw new Exception(instr.Return + " is not assignable"); }

                    var attr = ts.GetAttributeType(instr.Name);
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
                        if (attr.Type == AttributeType.Types.Name)
                        {
                            if (attr.AbsValue.IsInternalVariable())
                                ivarMan.ReverseSet(attr.AbsValue, name.returnType);
                        }
                    }

                    break;

                default:
                    string v1, v2 = null;
                    Type t1, t2 = null;

                    v1 = instr.Attributes[0];
                    t1 = solveAttributeType(v1);
                    if (v1.IsInternalVariable()) ivarMan.Appears(v1, instr);

                    if (instr.Op != "!")
                    {
                        v2 = instr.Attributes[1];
                        t2 = solveAttributeType(v2);
                        if (v2.IsInternalVariable()) ivarMan.Appears(v2, instr);
                    }

                    Type retType = t1;

                    // Study return variable (it's always an InternalVariable)
                    var ret = instr.Return;

                    if (t2 != null)
                    {
                        retType = ts.CompareTypeCast(t1, t2);

                        if (v2.IsInternalVariable())
                            ivarMan.ReverseSet(v2, retType);
                    }

                    if (v1.IsInternalVariable())
                        ivarMan.ReverseSet(v1, retType);

                    op.returnType = retType;

                    ivarMan.Set(instr.Return, op);

                    break;
            }

        }

        internal virtual void readLinear_call(Linear instr)
        {
            var comp = Solve(instr.Name);
            comp._called = true;

            var op = new Operation(this, instr);
            operations.Add(op);
            ivarMan.Set(instr.Return, op);

            // Check appears
            foreach(var attr in instr.Attributes)
                if (attr.IsInternalVariable()) ivarMan.Appears(attr, instr);
        }

        internal virtual void readLinear_import(Linear instr)
        {
            // Add as operation
            var op = new Operation(this, instr);
            operations.Add(op);

            var c = instr.component = Import(instr.Name, instr.Parameters); 
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
                ns = new Components.File(this, instr, null);
                addComponent(instr.Name, ns);
            }
            else
            {
                ns.InjectLinear(instr);
            }
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
            var comp = (Container)Solve(Name);
            comp.TopFile.Load();

            foreach (var c in comp.components)
            {
                //todo: imported key yet exists
                this.components.Add(c.Key, c.Value);
            }

            return comp;
        }
        #endregion

        #region Writer

        override public string WriteTo(Lang Lang)
        {
            return WriteTo(Lang, false);
        }

        public string WriteTo(Lang Lang, bool DontReturn)
        {
            writeReset();

            // Logic is reading instructions by instructions for maintaining original order
            foreach (var lin in _linear.Instructions)
            {
                var comp = lin.component;

                if(comp.Called)
                    writeToCont(comp.WriteTo(Lang));

                /*if (lin.Op == "call" || lin.Type == "oprt")
                    writeTo_operation(LangSettings, lin);
                else switch (lin.Op)
                    {
                        case "import":
                            writeTo_import(LangSettings, lin);
                            break;

                        case "namespace":
                        case "class":
                        case "function":
                            writeToCont(lin.component.WriteTo(LangSettings));
                            break;

                        case "compiler":
                            writeTo_compiler(LangSettings, lin);
                            break;
                    }*/
            }

            if (DontReturn) return null;
            return writeReturn();
        }

        internal override void writeReset()
        {
            if (written)
            {
                foreach (var i in operations)
                    i.writeReset();
            }

            base.writeReset();
        }

        /*virtual public void writeTo_import(Lang.Settings LangSettings, Linear instr)
        {
            throw new Exception("To be overridden");
        }

        virtual public void writeTo_operation(Lang.Settings LangSettings, Linear instr)
        {
            var op = (Operation)instr.component;
            // x = y *OP* z

        }

        virtual public void writeTo_compiler(Lang.Settings LangSettings, Linear instr)
        {
            throw new Exception("To be overridden");
        }*/

        #endregion

    }
}
