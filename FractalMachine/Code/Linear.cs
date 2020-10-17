using System;
using System.Collections.Generic;

namespace FractalMachine.Code
{
    public class Linear
    {
        internal Linear parent;
        internal OrderedAst origin;

        internal List<Linear> Instructions = new List<Linear>();
        internal List<Linear> Settings = new List<Linear>();

        public string Op;
        public string Name;
        public List<string> Attributes = new List<string>();
        public string Assign;

        public Linear() { }

        public Linear(Linear Parent)
        {
            parent = Parent;
            //parent.Instructions.Add(this);
        }

        public Linear NewSetting()
        {
            var lin = new Linear();
            lin.parent = this;
            Settings.Add(lin);
            return lin;
        }

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
    }
}
