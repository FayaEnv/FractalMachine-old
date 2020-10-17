using System;
namespace FractalMachine.Code.Conversion
{
    public static class AstToOrderedAst
    {
        static OrderedAst orderedAst;

        public static OrderedAst ToOrderedAst(Light light)
        {
            orderedAst = new OrderedAst();
            readAst(light.AST);
            orderedAst.Revision();
            return orderedAst;
        }

        static void readAst(AST ast)
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

        static void readAstBlockOrInstruction(AST ast)
        {
            orderedAst = orderedAst.NewChildFromAst(ast);

            foreach (var child in ast.children)
            {
                readAst(child);
            }

            orderedAst = orderedAst.parent;
        }

        static void readAstAttribute(AST ast)
        {
            orderedAst.ReadProperty(ast.subject);
        }
    }
}
