using FractalMachine.Classes;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace FractalMachine.Code.Components
{
    public class InternalVariablesManager
    {
        Container parent;
        Dictionary<int, InternalVariable> vars = new Dictionary<int, InternalVariable>();
        Dictionary<Type, TypeContainer> typeContainers = new Dictionary<Type, TypeContainer>();

        public InternalVariablesManager(Container Parent)
        {
            parent = Parent;
        }

        public InternalVariable Set(string ivar, Operation op)
        {
            int n = getN(ivar);
            var iv = new InternalVariable(this, op);
            vars.Add(n, iv);
            op.returnVar = iv;
            return iv;
        }

        public void ReverseSet(string ivar, Type t)
        {
            int n = getN(ivar);
            var iv = vars[n];
            iv.type = t;
        }

        public void Appears(string ivar, Linear instr)
        {
            int n = getN(ivar);
            var iv = vars[n];
            iv.lastAppearance = instr.Pos;
        }

        public InternalVariable Get(string var)
        {
            if (!var.IsInternalVariable())
                return null;

            var n = getN(var);
            return vars[n];
        }

        int getN(string ivar)
        {
            return Convert.ToInt32(ivar.Substring(Properties.InternalVariable.Length));
        }

        public TypeContainer getTypeContainer(Type t)
        {
            TypeContainer ret;
            if(!typeContainers.TryGetValue(t, out ret))
            {
                ret = new TypeContainer() { type = t };
                typeContainers.Add(t, ret);
            }
            return ret;
        }

        public class TypeContainer
        {
            public Type type;
            public List<Instance> instances = new List<Instance>();

            public Instance GetInstance(InternalVariable ivar)
            {
                Instance i = null;

                foreach(var inst in instances)
                {
                    if (inst.lastPos < ivar.lastAppearance)
                        i = inst;
                }

                if (i == null)
                {
                    i = new Instance();
                    i.name = "iv_" + ivar.type.Name + "_" + instances.Count;
                    //todo: if type is array, [] should be substituted in the Name
                    //todo check parent ivarMan for checking accumulated instances (avoid name collisions)
                }
                else
                    i.recycled = true;

                i.lastPos = ivar.lastAppearance;

                return i;
            }

            public class Instance
            {
                public string name;
                public int lastPos;
                public bool recycled = false;
            }
        }

        public class InternalVariable
        {
            public InternalVariablesManager parent;
            public Operation op;

            public Type type;
            public int lastAppearance;

            public string realVarType;
            public string realVarName;

            public InternalVariable(InternalVariablesManager Parent, Operation Op)
            {
                op = Op;
                parent = Parent;
                type = op.returnType;
            }

            public void setRealVar(Lang lang)
            {
                var ts = lang.GetTypesSet;
                var newType = ts.Convert(type);
                var tc = parent.getTypeContainer(type);
                var inst = tc.GetInstance(this); 

                if(!inst.recycled)
                    realVarType = ts.GetTypeCodeName(newType);

                realVarName = inst.name;
            }
        }
    }
}
