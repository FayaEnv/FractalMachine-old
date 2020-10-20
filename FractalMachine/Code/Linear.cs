using System;
using System.Collections.Generic;
using static FractalMachine.Code.AST;

namespace FractalMachine.Code
{
    public class Linear
    {
        internal Linear parent;
        internal AST ast;

        internal List<Linear> Instructions = new List<Linear>();
        internal List<Linear> Settings = new List<Linear>();
        internal Dictionary<string, string> Parameters = new Dictionary<string, string>();

        public string Op;
        public string Name;
        public List<string> Attributes = new List<string>();
        public string Return;

        public Linear(AST ast)
        {
            this.ast = ast;
        }

        public Linear(Linear Parent, AST orderedAst) : this(orderedAst)
        {
            parent = Parent;
            //parent.Instructions.Add(this);
        }

        public Linear NewSetting(AST ast)
        {
            var lin = new Linear(ast);
            lin.parent = this;
            Settings.Add(lin);
            return lin;
        }

        #region Parent

        bool listed = false;
        public void List()
        {
            if (!listed)
                parent.Instructions.Add(this);
            listed = true;
        }

        public void Remove()
        {
            if (listed)
            {
                parent.Instructions.Remove(this);
                listed = false;
            }
        }

        public void SetParent(Linear Parent)
        {
            parent = Parent;
        }

        public void Add(Linear lin)
        {
            Instructions.Add(lin);
            lin.parent = this;
        }

        #endregion

        public Linear LastInstruction
        {
            get
            {
                var c = Instructions.Count;
                if (c > 0)
                    return Instructions[c - 1];
                return null;
            }
        }
    }
}
