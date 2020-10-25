using System;
namespace FractalMachine.Code
{
    public static class Properties
    {
        public static string TempDir = "temp/";
        public static string CygwinDownloadUrl = "https://srv-store2.gofile.io/download/iITx0L/cygwin64-light.zip";
        public static int MaxRepositoryDays = 7;

        public static string[] FileImportExtensions = new string[] { ".h", ".light" };
        public static string InternalVariable = "£";

        public static string Mark = "*°";
        public static string StringMark = "$$" + Mark;
        public static string AngularBracketsMark = "<>" + Mark;

        /// <summary>
        /// Init things that depends on one of these properties
        /// </summary>
        public static void Init()
        {
            Resources.CreateDirIfNotExists(TempDir);
        }
    }
}
