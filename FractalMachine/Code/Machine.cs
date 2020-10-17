using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FractalMachine.Classes;

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
            orderedAst = AST.ToOrderedAst(Script);           
            linear = OrderedAst.ToLinear(orderedAst);
        }

        #endregion

    }

    /// <summary>
    /// An OrderedAst is an AST ready to a direct analysis and conversion to Linear.
    /// OrderedAst is used for extending AST without overload code and members in a unique class.
    /// </summary>
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

        #region ToLinear

        delegate void OnCallback();
        delegate void OnAddAttribute(string Attribute);

        static Linear outLin;
        static List<string> Params = new List<string>();

        public static Linear ToLinear(OrderedAst oAst)
        {
            orderedAstToLinear(oAst);
            return outLin;
        }

        static void orderedAstToLinear(OrderedAst oAst)
        {
            if (outLin == null)
                outLin = new Linear(oAst);

            Linear lin = null;
            OnCallback onEnd = null;

            bool enter = false;
            bool recordAttributes = true;

            OnAddAttribute onAddAttribute = delegate (string attr)
            {
                Params.Add(attr);
            };

            if (oAst.isDeclaration)
            {
                if (oAst.isBlockDeclaration)
                {
                    lin = new Linear(outLin, oAst);
                    lin.Op = oAst.declarationType;
                    lin.Name = Extensions.Pull(oAst.attributes);
                    lin.Attributes = oAst.attributes;

                    if (oAst.isFunction)
                    {
                        // Read parenthesis arguments
                        var parenthesis = Extensions.Pull(oAst.codes, 0);
                        injectDeclareFunctionParameters(lin, parenthesis);
                    }

                    outLin = lin;
                    enter = true;
                }
                else
                {
                    lin = new Linear(outLin, oAst);
                    lin.List();
                    lin.Op = "declare";
                    lin.Name = oAst.attributes[oAst.nAttrs - 1];
                    lin.Attributes = oAst.attributes;
                    Params.Add(lin.Name);
                }
            }
            else
            {
                if (oAst.ast.IsOperator)
                {
                    switch (oAst.Subject)
                    {
                        case "=":
                            lin = new Linear(outLin, oAst);
                            lin.Op = oAst.Subject;
                            lin.Attributes.Add(pullParams());

                            onEnd = delegate
                            {
                                lin.Attributes.Add(pullParams());
                            };

                            break;

                        case ".":
                            if (oAst.parent.Subject != ".")
                                oAst.attributes[0] = pullParams() + "." + oAst.attributes[0];
                            else
                                oAst.attributes[0] = oAst.parent.attributes[0] + "." + oAst.attributes[0];

                            recordAttributes = false;

                            break;

                        default:
                            lin = new Linear(outLin, oAst);
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
                    if (oAst.IsInFunctionParenthesis)
                    {
                        if (oAst.TopFunction.isDeclaration)
                        {
                            // bisognerebbe poter accedere alla lin della funzione...
                        }
                        else
                        {
                            onEnd = delegate ()
                            {
                                lin = new Linear(outLin, oAst);
                                lin.Op = "push";
                                lin.Attributes.Add(pullParams());
                            };
                        }
                    }
                }

                if (oAst.isFunction)
                {
                    lin = new Linear(outLin, oAst);
                    lin.Op = "call";
                    lin.Name = oAst.attributes[0];
                }
            }

            if (enter)
            {
                outLin = lin;
            }

            // Prepare attributes
            if (recordAttributes)
            {
                foreach (var s in oAst.attributes)
                {
                    onAddAttribute(s);
                }
            }

            ///
            /// Child analyzing
            ///

            foreach (var code in oAst.codes)
            {
                orderedAstToLinear(code);
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
                outLin = outLin.parent;
            }
        }

        static void injectDeclareFunctionParameters(Linear lin, OrderedAst oAst)
        {
            var sett = lin.NewSetting(oAst);
            sett.Op = "parameters";

            var oa = oAst.codes[0];
            Linear l = new Linear(sett, oa);
            l.List();

            while (oa != null)
            {
                if (oa.Subject == ",")
                {
                    l = new Linear(sett, oa);
                    l.List();
                }

                switch (oa.Subject)
                {
                    case "=":
                        // todo: creare settings al posto che attributes (oppure creare dictionary)
                        l.Attributes = oa.attributes;
                        break;

                    default:
                        l.Name = oa.attributes[0];
                        break;
                }

                if (oa.codes.Count == 1)
                    oa = oa.codes[0];
                else
                    oa = null;
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

        #endregion
    }
}
