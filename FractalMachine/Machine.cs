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

            linear = orderedAst.ToLinear();
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

    public class OrderedAst
    {
        internal AST ast;
        internal OrderedAst parent, linkedCC;
        internal List<OrderedAst> preCodes = new List<OrderedAst>();
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
            bool preCalc = false;
          

            if(ast.type == AST.Type.Instruction)
            {
                if (ast.IsOperator)
                    preCalc = true;
            }

            if(ast.IsBlockParenthesis)
            {
                if (this.ast.IsInstructionFree || this.ast.subject == ".")
                    isFunction = true;

                preCalc = true;
            }

            if (preCalc)
            {
                cc.tempVar = getTempNum();
                preCodes.Add(cc);

                var ccc = newClassCode();
                ccc.linkedCC = cc;
                codes.Add(ccc);
            }
            else
                codes.Add(cc);

            return cc;
        }

        public string[] DeclarationTypes = new string[] { "var", "function" };

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
                if (DeclarationTypes.Contains(declarationType))
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
                c.Ensure.Revision();
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
                bool parenthesis = true;
                var a = this;
                while(a != null && !a.isFunction)
                {
                    a = a.parent;
                    parenthesis = a.ast.IsBlockParenthesis || parenthesis;
                }

                return a != null && a.isFunction && parenthesis;
            }
        }

        internal OrderedAst Ensure
        {
            get
            {
                return linkedCC ?? this;
            }
        }

        #region ToLinear

        public class ToLinearBag
        {
            public Linear Lin;
            public List<string> Params = new List<string>();
            public int lastTempVar = -1;

            internal string pullTolinParams()
            {
                var c = Params.Count - 1;

                if (c < 0)
                    return "tolinParams EMPTY";

                string s = Params[c];
                Params.RemoveAt(c);
                return s;
            }
        }

        public Linear ToLinear(ToLinearBag bag = null)
        {

            if (bag == null)
            {
                bag = new ToLinearBag();
            }

            bool enter = false;

            enter = isBlockParenthesis && !IsInFunctionParenthesis;

            /// Enter
            if (enter)
            {
                bag.Lin = new Linear(bag.Lin);

                if (tempVar >= 0)
                    bag.Lin.Assign = "#" + tempVar;
            }

            bag.Lin.origin = this;

            ///
            /// OrderedAst preparing
            ///

            if (isBlockParenthesis && IsInFunctionParenthesis)
            {
                var settings = bag.Lin.NewSetting();
                settings.Op = "parenthesis";
                bag.Lin = settings;
            }

            if (isDeclaration)
            {
                /* CONTINUARE
                 * isFunction ma in realtà vale in generale per i blocchi {}
                 * Il punto ora è elaborare le parentesi in base al tipo di funzione (function, if ...)
                 */
                if (isBlockDeclaration)
                {
                    var op = new Linear(bag.Lin);
                    op.Op = declarationType;
                    op.Name = attributes[nAttrs - 1];
                    op.Attributes = attributes;
                    bag.Params.Add(op.Name);

                    bag.Lin = op;
                    enter = true;
                }
                else
                {
                    var op = new Linear(bag.Lin);
                    op.Op = "declare";
                    op.Name = attributes[nAttrs - 1];
                    op.Attributes = attributes;
                    bag.Params.Add(op.Name);
                }
            }
            else
            {
                // Put attributes in the queue
                foreach (var s in attributes)
                    bag.Params.Add(s);
            }

            if (ast?.subject == ".")
            {
                var s = bag.pullTolinParams();
                bag.Params.Add(bag.pullTolinParams() + "." + s);
            }

            ///
            /// Execution
            ///

            /// PreCodes
            foreach (var preCc in preCodes)
            {
                preCc.ToLinear();
                bag.Params.Add("#" + preCc.tempVar);
            }

            /// OrderedAst analyzing
            if (ast != null)
            {
                if (ast.IsOperator)
                {
                    if (ast.subject != ".")
                    {
                        var op = new Linear(bag.Lin);
                        op.Op = ast.subject;
                        op.Attributes.Add(bag.pullTolinParams());
                        op.Attributes.Add(bag.pullTolinParams());
                        if (tempVar >= 0) op.Assign = "#" + tempVar.ToString();
                    }
                }
            }

            if (IsInFunctionParenthesis)
            {
                var op = new Linear(bag.Lin);
                op.Op = "push";
                op.Attributes.Add(bag.pullTolinParams());
            }

            if (isFunction && !isDeclaration)
            {
                var op = new Linear(bag.Lin);
                op.Op = "call";               
                op.Attributes.Add(bag.pullTolinParams());
                op.Name = bag.pullTolinParams();
            }

            /*if (oAst.isBlockDeclaration)
            {
                if (isFunction)
                {
                    var parenthesis = oAst.codes[ncodes - 2].Ensure;
                    functionParenthesisToLinear(tolin, parenthesis);
                }

                var block = oAst.codes[ncodes - 1];
                bracketsBlockToLinear(tolin, block);
            }*/


            /// Codes
            foreach (var code in codes)
            {
                if (code.linkedCC == null)
                {
                    code.ToLinear(bag);
                }
            }

            if (enter)
            {
                if (bag.lastTempVar >= 0)
                {
                    var lin = new Linear(bag.Lin);
                    lin.Op = "=";
                    lin.Attributes.Add("#" + tempVar);
                    lin.Attributes.Add(bag.pullTolinParams());
                    bag.lastTempVar = -1;
                }

                bag.Lin = bag.Lin.parent;
            }

            if (tempVar >= 0)
                bag.lastTempVar = tempVar;


            return bag.Lin;
        }

        void functionParenthesisToLinear(Linear to, OrderedAst oAst)
        {
            var settings = to.NewSetting();
            settings.Op = "parenthesis";

            string read = "here";
        }

        void bracketsBlockToLinear(Linear to, OrderedAst oAst)
        {

        }

        #endregion
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
            parent.Instructions.Add(this);
        }

        public Linear NewSetting()
        {
            var lin = new Linear();
            lin.parent = this;
            Settings.Add(lin);
            return lin;
        }

    }
}
