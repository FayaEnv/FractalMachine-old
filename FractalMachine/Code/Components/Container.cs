using FractalMachine.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Text;

namespace FractalMachine.Code.Components 
{
    abstract public class Container : Component
    {
        internal ContainerTypes containerType;
        internal List<Operation> operations = new List<Operation>();
        internal InternalVariablesManager ivarMan = new InternalVariablesManager();

        public Container(Component parent, string Name, Linear linear) : base(parent, Name, linear)
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
            DataStructure,
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
            /// Divided in two phases
            // First analyze structures
            ReadLinear_Struct(lin);
            // Then operations
            ReadLinear_Operation(lin);
        }

        public void ReadLinear_Struct()
        {
            ReadLinear_Struct(_linear);
        }

        public virtual void ReadLinear_Struct(Linear lin)
        {
            for (int i = 0; i < lin.Instructions.Count; i++)
            {
                var instr = lin[i];
                instr.Pos = i;

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

                    case "class":
                        readLinear_class(instr);
                        break;

                    case "struct":
                        readLinear_struct(instr);
                        break;

                    case "namespace":
                        readLinear_namespace(instr);
                        break;
                }

                if(instr.component != null && instr.component is Container)
                {
                    var cont = (Container)instr.component;
                    cont.ReadLinear_Struct();
                }
            }
        }


        public void ReadLinear_Operation()
        {
            ReadLinear_Operation(_linear);
        }

        public virtual void ReadLinear_Operation(Linear lin)
        {
            for (int i = 0; i < lin.Instructions.Count; i++)
            {
                var instr = lin[i];
                instr.Pos = i;

                switch (instr.Op)
                {        
                    /// Post Struct
                    case "function":
                    case "class":
                    case "struct":
                    case "namespace":
                        var cont = (Container)instr.component;
                        cont.ReadLinear_Operation();
                        break;

                    case "declare":
                    case "import":
                        break;

                    /// Only operations
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
                            throw new Exception("Unassigned operation");
                        break;
                }
            }
        }

        internal virtual void readLinear_cast(Linear instr)
        {
            var ts = instr.Lang.GetTypesSet;
            var op = new Operation(this, instr);
            operations.Add(op);
            op.returnType = ts.Get(instr.Type);
            ivarMan.Set(instr.Return, op);
        }

        internal virtual void readLinear_declare(Linear instr)
        {
            var member = new Member(this, instr.Name, instr);

            if (instr.Return == "var")
                member.typeToBeDefined = true;
            else
                member.returnType = instr.Lang.GetTypesSet.Get(instr.Return);
        }

        internal virtual void readLinear_operator(Linear instr)
        {
            var ts = instr.Lang.GetTypesSet;

            var op = new Operation(this, instr);
            operations.Add(op);

            ///
            /// Pratically: prepare variables involved in the operation
            ///

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
                        /// Assign probable type 
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

                    if(attr.Type == AttributeType.Types.Name && attr.AbsValue.IsInternalVariable())
                    {
                        var iv = ivarMan.Get(attr.AbsValue);

                        iv.Appears(instr); // is it still useful with assign shortuct?

                        // Assign shortcut: assign iv directly to assigned variable
                        iv.realVarName = instr.Return;
                        op.disabled = true;
                    }

                    break;

                default:
                    string v1, v2 = null;
                    Type t1, t2 = null;

                    v1 = instr.Attributes[0];
                    t1 = solveAttribute_type(v1);
                    if (v1.IsInternalVariable()) ivarMan.Appears(v1, instr);

                    if (instr.Op != "!")
                    {
                        v2 = instr.Attributes[1];
                        t2 = solveAttribute_type(v2);
                        if (v2.IsInternalVariable()) ivarMan.Appears(v2, instr);
                    }

                    Type retType = t1;

                    if (t2 != null)
                    {
                        retType = ts.CompareTypeCast(t1, t2);

                        if (v2.IsInternalVariable())
                        {
                            var iv = ivarMan.Get(v2);
                            iv.ReverseSet(retType);
                            iv.Appears(instr);
                        }
                    }

                    if (v1.IsInternalVariable())
                    {
                        var iv = ivarMan.Get(v1);
                        iv.ReverseSet(retType);
                        iv.Appears(instr);
                    }

                    op.returnType = retType;

                    ivarMan.Set(instr.Return, op);

                    break;
            }

        }

        internal virtual void readLinear_call(Linear instr)
        {
            var comp = Solve(instr.Name);
            comp._called = true;
            //todo: handle overloads

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
            instr.component = op;

            op.attached = Import(instr.Name, instr.Parameters); 
        }

        internal virtual void readLinear_class(Linear instr)
        {
            var cl = new Class(this, instr.Name, instr);
        }

        internal virtual void readLinear_struct(Linear instr)
        {
            throw new Exception("todo");
        }

        internal virtual void readLinear_function(Linear instr)
        {
            Function function = null;

            try
            {
                var comp = getComponent(instr.Name);

                if(comp != null && !(comp is Preload))
                    function = (Function)comp;
            }
            catch (Exception ex)
            {
                throw new Exception("Name used for another variable");
            }

            if (function == null)
            {
                function = new Function(this, instr.Name);
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
                ns = new Components.File(this, instr.Name, instr, null);
            }
            else
            {
                ns.InjectLinear(instr);
            }
        }

        #endregion

        #region Methods

        Type solveAttribute_type(string attr)
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
            // Logic is reading instructions by instructions for maintaining original order
            foreach (var lin in _linear.Instructions)
            {
                var comp = lin.component;

                if (comp.Called)
                {
                    writeToCont(comp.WriteTo(Lang));
                    writeNewLine(lin);
                }

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
