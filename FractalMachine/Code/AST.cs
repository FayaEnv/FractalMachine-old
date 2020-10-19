using FractalMachine.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FractalMachine.Code
{

    class Switch
    {
        public delegate void OnSwitchChangedDelegate();
        public delegate bool EnableInvokeDelegate();

        public OnSwitchChangedDelegate OnSwitchChanged;
        public EnableInvokeDelegate EnableInvoke;

        bool val;

        public bool Value
        {
            get
            {
                return val;
            }

            set
            {
                if (val != value && (EnableInvoke == null || EnableInvoke.Invoke()))
                    OnSwitchChanged?.Invoke();

                val = value;
            }
        }

        public void Toggle()
        {
            Value = !Value;
        }
    }

    public class AST
    {
        private AST parent;
        //private Dictionary<string, List<AST>> subs = new Dictionary<string, List<AST>>();
        internal List<AST> children = new List<AST>();

        // Instruction preview
        internal string subject, aclass; // variable name, if,
        internal Type type;
        internal int line, pos;

        // e creare un AST specializzato per ogni tipologia di istruzione?
        // no, ma crei un descrittore per ogni tipologia di AST, così da dare un ordine a childs

        public enum Type
        {
            Block,
            Instruction,
            Attribute
        }

        #region Constructor

        public AST(AST Parent, int Line, int Pos, Type type = Type.Block) : base()
        {
            parent = Parent;
            line = Line;
            pos = Pos;
            this.type = type;

            if (type == Type.Block)
                NewChild(Line, Pos, Type.Instruction);
        }

        public AST()
        {
        }

        #endregion

        #region Properties

        public AST Next
        {
            get
            {
                if (children.Count == 0)
                    return null;

                return children[children.Count - 1];
            }
        }
    
        public AST BeforeMe
        {
            get
            {
                if (parent == null)
                    return null;
                var pos = parent.children.IndexOf(this);

                if (pos == 0)
                    return null;

                return parent.children[pos - 1];
            }
        }

        public string MainSubject
        {
            get
            {
                if (subject != null)
                {
                    return subject;
                }
                else
                {
                    if (children.Count > 0 && children[0].type == Type.Attribute)
                        return children[0].subject;
                }

                return null;
            }
        }

        #endregion

        #region InternalProperties

        internal AST Instruction
        {
            get
            {
                var child = LastChild;

                while (child?.LastChild != null && child.LastChild.type == Type.Instruction)
                {
                    child = child.LastChild;
                }

                return child;
            }
        }

        internal AST LastChild
        {
            get
            {
                if (children.Count == 0)
                    return null;

                return children[children.Count - 1];
            }
        }

        internal AST GetTopBlock
        {
            get
            {
                var a = parent;
                while (a.type != Type.Block)
                    a = a.parent;
                return a;
            }
        }

        #endregion

        #region Methods

        internal AST NewChild(int Line, int Pos, Type type)
        {
            var child = new AST(this, Line, Pos, type);
            children.Add(child);
            return child;
        }

        internal AST NewInstruction(int Line, int Pos)
        {
            return NewChild(Line, Pos, Type.Instruction);
        }

        internal AST InsertAttribute(int Line, int Pos, string Content)
        {
            var ast = Instruction;
            var child = ast.NewChild(Line, Pos, Type.Attribute);
            child.subject = Content;
            return child;
        }

        #endregion

        public abstract class Amanuensis
        {
            internal int Cycle = 0;
        }

        public abstract class OrderedAST
        {
        }

        public class StatusSwitcher
        {
            public delegate void OnTriggeredDelegate(Triggers.Trigger Trigger);

            public OnTriggeredDelegate OnTriggered;
            public Triggers CurrentStatus;
            public Dictionary<string, Triggers> statuses = new Dictionary<string, Triggers>();
            public char[] IgnoreChars;

            internal Amanuensis Parent;

            public StatusSwitcher(Amanuensis Parent)
            {
                this.Parent = Parent;
            }

            public Triggers Define(string Name)
            {
                var status = new Triggers(this);

                if (Name == "default")
                {
                    CurrentStatus = status;
                    status.IsEnabled = true;
                }

                statuses.Add(Name, status);
                return status;
            }

            public void Cycle(char ch)
            {
                CurrentStatus.OnCharCycle?.Invoke(ch);
            }

            internal void Triggered(Triggers.Trigger trigger)
            {
                OnTriggered?.Invoke(trigger);
                CurrentStatus.OnTriggered?.Invoke(trigger);
                trigger.OnTriggered?.Invoke(trigger);

                if (trigger.ActivateStatus != null)
                {
                    SwitchStatus(trigger.ActivateStatus);
                }
            }

            public void UpdateCurrentStatus()
            {
                CurrentStatus.OnCycleEnd?.Invoke();
                CurrentStatus.OnNextCycleEnd?.Invoke();
            }

            public void SwitchStatus(string status)
            {
                CurrentStatus.IsEnabled = false;
                CurrentStatus.OnExit?.Invoke();
                CurrentStatus = statuses[status];
                CurrentStatus.IsEnabled = true;
                CurrentStatus.OnEnter?.Invoke();
            }
        }

        public class Triggers
        {
            public delegate void OnCycleDelegate();
            public delegate void OnCharCycleDelegate(char Char);

            public OnCycleDelegate OnCycleEnd, OnEnter, OnExit;
            public OnCharCycleDelegate OnCharCycle;
            public Dictionary<int, OnCycleDelegate> OnSpecificCycle = new Dictionary<int, OnCycleDelegate>();
            public StatusSwitcher.OnTriggeredDelegate OnTriggered;
            public bool IsEnabled = false;

            internal StatusSwitcher Parent;
            internal CharTree delimetersTree = new CharTree();
            List<Trigger> triggers = new List<Trigger>();


            public Triggers(StatusSwitcher Parent)
            {
                this.Parent = Parent;
            }

            public Trigger Add(Trigger Trigger)
            {
                if (Trigger.Delimiter != null)
                    Trigger.Delimiters = new string[] { Trigger.Delimiter };

                if (Trigger.Delimiters == null)
                    throw new Exception("No delimiter is specified");

                Trigger.Parent = this;
                triggers.Add(Trigger);

                foreach (var del in Trigger.Delimiters)
                    delimetersTree.Insert(del, Trigger);

                return Trigger;
            }

            public OnCycleDelegate OnNextCycleEnd
            {
                get
                {
                    var cycle = Parent.Parent.Cycle;
                    OnCycleDelegate onCycle;

                    if (OnSpecificCycle.TryGetValue(cycle, out onCycle))
                    {
                        OnSpecificCycle.Remove(cycle);
                        return onCycle;
                    }

                    return null;
                }

                set
                {
                    OnSpecificCycle.Add(Parent.Parent.Cycle + 1, value);
                }
            }

            public class Trigger
            {
                //public delegate void OnTriggeredDelegate();
                public delegate bool IsEnabledDelegate();

                public Triggers Parent;
                public StatusSwitcher.OnTriggeredDelegate OnTriggered;
                public IsEnabledDelegate IsEnabled;
                public string Delimiter;
                public string[] Delimiters;
                public string ActivateStatus;

                public string activatorDelimiter;

                public void Trig()
                {
                    OnTriggered?.Invoke(this);
                }
            }
        }
    }
}
