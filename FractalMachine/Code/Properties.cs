using System;
namespace FractalMachine.Code
{
    public static class Properties
    {
        //public static string[] DeclarationTypes = new string[] { "var", "function" };
        public static string[] FileImportExtensions = new string[] { ".h", ".light" };
        public static string[] Statements = new string[] { "import", "namespace", "#include" };
        public static string[] ContinuousStatements = new string[] { "namespace", "private", "public" };
        public static string[] Modifiers = new string[] { "private", "public", "protected" };
        public static string[] DeclarationOperations = new string[] { "declaration", "function" };

        public static string Mark = "^°*";
        public static string StringMark = "$$" + Mark;
        public static string AngularBracketsMark = "<>" + Mark;
    }
}
