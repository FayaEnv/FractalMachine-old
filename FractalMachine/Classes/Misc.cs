using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FractalMachine.Classes
{
    public class Misc
    {
        public static string DirectoryToFile(string dir)
        {
            return dir.Replace("/", "-").Replace("\\", "-").Replace(":", "");
        }
    }
}
