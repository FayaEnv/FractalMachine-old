using System;
using System.Collections.Generic;
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

        internal bool isFunction = false;

        public OrderedAst NewChildFromAst(AST ast)
        {
            var cc = newClassCode();
            cc.linkAst(ast);
            bool preCalc = false;
          

            if(ast.type == AST.Type.Instruction)
            {
                if (ast.aclass == "operator" && ast.subject != ".")
                    preCalc = true;
            }

            if(ast.IsBlockParenthesis)
            {
                if (this.ast.IsInstructionFree)
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

        #region ToLinear

        Linear tolin;
        List<string> tolinParams = new List<string>();
        int lastTempVar = -1;

        public Linear ToLinear(OrderedAst oAst=null)
        {
            if (oAst == null)
            {
                oAst = this;
                tolin = new Linear();
            }

            bool enter = false;
            bool isBlockParenthesis = false;
            bool isDeclaration = false;
            bool isFunction = oAst.isFunction;

            if (oAst.ast != null)
            {
                var ast = oAst.ast;
                isBlockParenthesis = ast.IsBlockParenthesis;
            }

            enter = isBlockParenthesis;

            if (enter)
            {
                tolin = new Linear(tolin);
                tolin.Assign = "#" + oAst.tempVar;
            }

            tolin.origin = oAst;

            /// OrderedAst preparing
            var nAttrs = oAst.attributes.Count;
            if (nAttrs >= 2)
            {
                if (oAst.attributes[nAttrs - 2] == "var") //todo: ... o un tipo
                    isDeclaration = true;
            }

            if (isDeclaration)
            {
                if (!isFunction)
                {
                    var op = new Linear(tolin);
                    op.Op = "declare";
                    op.Name = oAst.attributes[nAttrs - 1];
                    op.Attributes = oAst.attributes;
                    tolinParams.Add(oAst.attributes[nAttrs - 1]);
                }
            }
            else
            {
                foreach (var s in oAst.attributes)
                    tolinParams.Add(s);
            }
            

            /// PreCodes
            foreach (var preCc in oAst.preCodes)
            {
                ToLinear(preCc);
                tolinParams.Add("#" + preCc.tempVar);
            }

            /// OrderedAst analyzing
            if (oAst.ast != null)
            {
                var ast = oAst.ast;

                if (ast.IsOperator)
                {
                    var op = new Linear(tolin);
                    op.Op = ast.subject;
                    op.Attributes.Add(pullTolinParams());
                    op.Attributes.Add(pullTolinParams());
                    if(oAst.tempVar >= 0) op.Assign = "#"+oAst.tempVar.ToString();
                }
            }

            

            /// Codes
            foreach (var code in oAst.codes)
            {
                if (code.linkedCC == null)
                {
                    ToLinear(code);
                }
            }

            if (enter)
            {
                if (lastTempVar >= 0)
                {
                    var lin = new Linear(tolin);
                    lin.Op = "=";
                    lin.Attributes.Add("#" + oAst.tempVar);
                    lin.Attributes.Add(pullTolinParams());
                    lastTempVar = -1;
                }

                tolin = tolin.parent;
            }

            if (oAst.tempVar >= 0)
                lastTempVar = oAst.tempVar;


            return tolin;
        }

        string pullTolinParams()
        {
            var c = tolinParams.Count - 1;

            if (c < 0)
                return "tolinParams EMPTY";

            string s = tolinParams[c];
            tolinParams.RemoveAt(c);
            return s;
        }

        #endregion
    }

    public class Linear
    {
        internal Linear parent;
        internal List<Linear> instructions = new List<Linear>();
        internal OrderedAst origin;

        public string Op;
        public string Name;
        public List<string> Attributes = new List<string>();
        public string Assign;

        public Linear() { }

        public Linear(Linear Parent)
        {
            parent = Parent;
            parent.instructions.Add(this);
        }

    }
}
