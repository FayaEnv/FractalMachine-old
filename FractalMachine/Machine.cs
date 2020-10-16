using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FractalMachine
{
    public class Machine
    {
        Component main;

        #region ReadClassCode

        internal OrderedAst orderedAst;
        internal Linear linear;

        public void CreateClassCode(Light Script)
        {
            orderedAst = new OrderedAst();

            ReadAst(Script.AST);
            orderedAst.Revision();
            OrderedAstToLinear.ToLinear(orderedAst);
            linear = OrderedAstToLinear.OutLin;
        }

        void ReadAst(AST ast)
        {
            switch (ast.type)
            {
                case AST.Type.Attribute:
                    readAstAttribute(ast);
                    break;

                default:
                    readAstBlockOrInstruction(ast);
                    break;
            }
        }

        void readAstBlockOrInstruction(AST ast)
        {
            orderedAst = orderedAst.NewChildFromAst(ast);

            foreach (var child in ast.children)
            {
                ReadAst(child);
            }

            orderedAst = orderedAst.parent;
        }

        void readAstAttribute(AST ast)
        {
            orderedAst.ReadProperty(ast.subject);
        }

        #endregion

    }

    delegate void OnCallback();
    delegate void OnAddAttribute(string Attribute);

    public static class OrderedAstToLinear
    {
        public static Linear OutLin;
        static List<string> Params = new List<string>();

        public static void ToLinear(OrderedAst oAst)
        {
            if (OutLin == null)
                OutLin = new Linear();

            Linear lin = null;
            OnCallback onEnd = null;
            bool enter = false;

            OnAddAttribute onAddAttribute = delegate (string attr)
            {
                Params.Add(attr);
            };

            if (oAst.isDeclaration)
            {
                if (oAst.isFunction)
                {
                    lin = new Linear(OutLin);
                    lin.Op = oAst.declarationType;
                    lin.Name = oAst.attributes[oAst.nAttrs - 1];
                    lin.Attributes = oAst.attributes;

                    OutLin = lin;
                    enter = true;
                }
                else
                {
                    lin = new Linear(OutLin);
                    lin.List();
                    lin.Op = "declare";
                    lin.Name = oAst.attributes[oAst.nAttrs - 1];
                    lin.Attributes = oAst.attributes;
                    Params.Add(lin.Name);
                }
            }
            else
            {
                if (oAst.ast != null)
                {
                    if (oAst.ast.IsOperator)
                    {
                        switch (oAst.Subject)
                        {
                            case "=":
                                lin = new Linear(OutLin);
                                lin.Op = oAst.Subject;
                                lin.Attributes.Add(pullParams());

                                onEnd = delegate
                                {
                                    lin.Attributes.Add(pullParams());
                                };

                                break;

                            case ".":
                                //Params.Add(pullParams()+".")

                                break;

                            default:
                                lin = new Linear(OutLin);
                                lin.Op = oAst.Subject;
                                lin.Attributes.Add(pullParams());
                                lin.Assign = "$" + oAst.getTempVar();
                                Params.Add("$" + oAst.getTempVar());

                                onEnd = delegate
                                {
                                    lin.Attributes.Add(pullParams());
                                };

                                break;
                        }

                    }

                    if (oAst.ast.IsBlockParenthesis)
                    {
                        /*lin = new Linear(OutLin);
                        lin.Op = "dummy";
                        lin.List();

                        onEnd = delegate ()
                        {
                            lin.Remove();
                            lin = null;
                            onAddAttribute(pullParams());
                        };*/

                        // empty for the moment
                    }
                }

                if (oAst.isFunction)
                {
                    var op = new Linear(OutLin);
                    op.Op = "call";
                    op.Attributes.Add(pullParams());
                    op.Name = pullParams();
                }
            }

            if (enter)
            {
                OutLin = lin;
            }

            // Prepare attributes
            foreach (var s in oAst.attributes)
            {
                onAddAttribute(s);
            }

            ///
            /// Child analyzing
            ///

            foreach (var code in oAst.codes)
            {
                ToLinear(code);
            }


            ///
            /// Exit
            ///

            if (onEnd != null)
                onEnd();

            if (lin != null)
                lin.List();

            if (enter)
            {
                OutLin = OutLin.parent;
            }
        }

        static string pullParams()
        {
            var c = Params.Count - 1;

            if (c < 0)
                return "tolinParams EMPTY";

            string s = Params[c];
            Params.RemoveAt(c);
            return s;
        }
    }

    public static class Properties
    {
        public static string[] DeclarationTypes = new string[] { "var", "function" };
    }

    public class OrderedAst
    {
        internal AST ast;
        internal OrderedAst parent;
        internal List<OrderedAst> codes = new List<OrderedAst>();
        internal List<string> attributes = new List<string>();

        internal int tempVarCount = 0, tempVar = -1;

        internal string declarationType = "";
        internal int nAttrs;
        internal bool isFunction = false;

        internal bool isBlockParenthesis = false;
        internal bool isDeclaration = false;
        internal bool isBlockDeclaration = false;
        internal bool isComma = false;

        public OrderedAst NewChildFromAst(AST ast)
        {
            var cc = newClassCode();
            cc.linkAst(ast);
            codes.Add(cc);

            if (ast.IsBlockParenthesis)
            {
                if (this.ast.IsInstructionFree || this.ast.subject == ".")
                    isFunction = true;
            }

            return cc;
        }

        public void Revision()
        {
            if(ast != null)
            {
                isBlockParenthesis = ast.IsBlockParenthesis;
            }

            // Check attributes
            nAttrs = attributes.Count;
            if (nAttrs >= 2)
            {
                declarationType = attributes[nAttrs - 2];
                if (Properties.DeclarationTypes.Contains(declarationType))
                    isDeclaration = true;
            }

            // Check subcodes
            var ncodes = codes.Count;
            if (ncodes >= 1)
            {
                var last = codes[ncodes - 1];
                bool hasLastBlockBrackets = last.ast?.IsBlockBrackets ?? false;
                isBlockDeclaration = hasLastBlockBrackets && isDeclaration;
            }

            foreach (var c in codes)
                c.Revision();
        }

        void linkAst(AST ast)
        {
            this.ast = ast;
        }

        internal int getTempNum()
        {
            var num = tempVarCount;
            var par = parent;

            while(par != null)
            {
                num += par.tempVarCount;
                par = par.parent;
            }

            tempVarCount++;
            return num;
        }

        internal int getTempVar()
        {
            if(tempVar == -1)
                tempVar = getTempNum();
            return tempVar;
        }

        public void ReadProperty(string Property)
        {
            attributes.Add(Property);
        }
        
        internal OrderedAst newClassCode()
        {
            var cc = new OrderedAst();
            cc.parent = this;
            return cc;
        }

        internal bool IsInFunctionParenthesis
        {
            get
            {
                bool parenthesis = false;
                var a = this;
                while(a != null && !a.isFunction)
                {                    
                    parenthesis = a.ast?.IsBlockParenthesis ?? false || parenthesis;
                    a = a.parent;
                }

                return a != null && a.isFunction && parenthesis;
            }
        }

        internal string Subject
        {
            get
            {
                return ast.subject;
            }
        }

        internal OrderedAst TopFunction
        {
            get
            {
                var a = this;
                while (a != null && !a.isFunction)
                {
                    a = a.parent;
                }

                return a;
            }
        }
    }

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
            if(!listed)
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

        #region ConstructionZone
        /*internal int requestingAttributes = 0;

        internal Linear lastInstruction
        {
            get
            {
                if (Instructions.Count == 0) return null;
                return Instructions[Instructions.Count - 1];
            }
        }*/

        #endregion

    }
}
