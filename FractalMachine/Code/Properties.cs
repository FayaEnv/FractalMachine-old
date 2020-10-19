using System;
namespace FractalMachine.Code
{
    public static class Properties
    {
        //public static string[] DeclarationTypes = new string[] { "var", "function" };
        public static string[] Statements = new string[] { "import" };
        public static string[] FileImportExtensions = new string[] { ".h", ".light" };

        public static string Mark = "^°*";
        public static string StringMark = "$$" + Mark;
        public static string AngularBracketsMark = "<>" + Mark;
    }
}
