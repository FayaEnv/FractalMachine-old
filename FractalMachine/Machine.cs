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

            if(ast.type == AST.Type.Block)
            {
                if(ast.subject == "(")
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

        public Linear ToLinear(OrderedAst oAst=null)
        {
            if (oAst == null)
            {
                oAst = this;
                tolin = new Linear();
            }

            bool enter = false;
            bool isBlockParenthesis = false;

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

            /// OrderedAst preparing
            /*foreach (var s in oAst.attributes)
                tolinParams.Add(s);*/

            /// PreCodes
            foreach (var preCc in oAst.preCodes)
            {
                ToLinear(preCc);
                oAst.attributes.Add("#" + preCc.tempVar);
            }

            /// OrderedAst analyzing
            if (oAst.ast != null)
            {
                var ast = oAst.ast;

                if (ast.IsOperator)
                {
                    var op = new Linear(tolin);
                    op.Op = ast.subject;
                    op.Attributes = collectAttributes(oAst);
                    tolinParams.Clear();
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
                tolin = tolin.parent;
            }


            return tolin;
        }

        string[] collectAttributes(OrderedAst oast, int levels = 2)
        {
            var coll = new List<string>();

            for(int i=0; i<levels; i++)
            {
                foreach (string a in oast.attributes)
                    coll.Add(a);

                oast = oast.parent;
                if (oast == null)
                    break;
            }

            return coll.ToArray();
        }

        #endregion
    }

    public class Linear
    {
        internal Linear parent;
        internal List<Linear> instructions = new List<Linear>();

        public string Op;
        public string Name;
        public string[] Attributes = new string[0];
        public string Assign;

        public Linear() { }

        public Linear(Linear Parent)
        {
            parent = Parent;
            parent.instructions.Add(this);
        }

        /*
        public Linear(ClassCode ClassCode)
        {
            fromClassCode(ClassCode);
        }

        void fromClassCode(ClassCode cc)
        {
            this.cc = cc;

            if (this.parent == null || cc.ast.type == AST.Type.Block)
                instrPointer = this;
            else
                instrPointer = this.parent.instrPointer;

            if (cc.ast != null)
            {
                Op = cc.ast.aclass;
                Name = cc.ast.subject;
            }

            Attributes = cc.properties;

            /// Analyze internal codes
            foreach(var preCc in cc.preCodes)
            {
                var instr = newInstruction();
                instr.fromClassCode(preCc);
            }

            foreach (var code in cc.codes)
            {
                if (code.linkedCC == null)
                {
                    var instr = newInstruction();
                    instr.fromClassCode(code);
                }
            }
        }

        Linear newInstruction()
        {
            var lin = new Linear(this);
            instrPointer.instructions.Insert(0, lin);
            return lin;
        }*/


    }
}
