using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine
{
    public class Machine
    {
        Component main;

        public void Execute(Light Script)
        {
            ReadAst(Script.AST);
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
            }
        }

        void readAstBlock(AST ast)
        {

        }

        void readAstInstruction(AST ast)
        {

        }
    }
}
