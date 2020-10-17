using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FractalMachine.Code.Conversion;

namespace FractalMachine.Code
{
    public class Machine
    {
        Component main;

        #region ReadClassCode

        internal OrderedAst orderedAst;
        internal Linear linear;

        public void CreateClassCode(Light Script)
        {
            orderedAst = AstConversion.ToOrderedAst(Script);           
            linear = OrderedAstConversion.ToLinear(orderedAst);
        }

        #endregion

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

        internal Linear lin;

        internal bool isBlockParenthesis = false;
        internal bool isDeclaration = false;
        internal bool isBlockDeclaration = false;
        internal bool isComma = false;

        public OrderedAst(AST ast)
        {
            linkAst(ast);
        }

        public OrderedAst NewChildFromAst(AST ast)
        {
            var cc = newClassCode(ast);
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
        
        internal OrderedAst newClassCode(AST ast)
        {
            var cc = new OrderedAst(ast);
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
}
