using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine
{
    public class Machine
    {
        Component main;

        #region ReadClassCode

        internal ClassCode wClassCode;
        internal Linear linear;

        public void CreateClassCode(Light Script)
        {
            wClassCode = new ClassCode();

            ReadAst(Script.AST);

            linear = new Linear(wClassCode);
        }

        void ReadAst(AST ast)
        {
            switch (ast.type)
            {
                case AST.Type.Block:
                    readAstBlock(ast);
                    break;

                case AST.Type.Instruction:
                    readAstInstruction(ast);
                    break;

                case AST.Type.Attribute:
                    readAstAttribute(ast);
                    break;
            }
        }

        void readAstBlock(AST ast)
        {
            wClassCode = wClassCode.NewChildFromAst(ast);

            foreach (var child in ast.children)
            {
                ReadAst(child);
            }

            wClassCode = wClassCode.parent;
        }

        void readAstInstruction(AST ast)
        {
            wClassCode = wClassCode.NewChildFromAst(ast);

            foreach (var child in ast.children)
            {
                ReadAst(child);
            }

            wClassCode = wClassCode.parent;
        }

        void readAstAttribute(AST ast)
        {
            wClassCode.ReadProperty(ast.subject);
        }

        #endregion
    }

    public class ClassCode
    {
        internal AST ast;
        internal ClassCode parent, linkedCC;
        internal List<ClassCode> preCodes = new List<ClassCode>();
        internal List<ClassCode> codes = new List<ClassCode>();
        internal List<string> properties = new List<string>();

        internal string subject;
        internal int tempVarCount = 0, tempVar = -1;

        public ClassCode NewChildFromAst(AST ast)
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
            subject = ast.subject;
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
            properties.Add(Property);
        }
        
        internal ClassCode newClassCode()
        {
            var cc = new ClassCode();
            cc.parent = this;
            return cc;
        }

        public enum Type
        {
            Instruction,
            Block
        }
    }

    public class Linear
    {
        internal Linear instrPointer;
        internal Linear parent;
        internal ClassCode cc;
        internal List<Linear> instructions = new List<Linear>();

        public string Op;
        public string Name;
        public List<string> Attributes = new List<string>();

        public Linear(ClassCode ClassCode)
        {
            fromClassCode(ClassCode);
        }

        public Linear(Linear Parent)
        {
            parent = Parent;
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
        }


    }
}
