using System;
using System.Collections.Generic;

namespace FractalMachine.Classes
{
    public static class Extensions
    {

        public static string Pull(this List<string> obj)
        {
            var c = obj.Count;
            if(c > 0)
            {
                var s = obj[c - 1];
                obj.RemoveAt(c - 1);
                return s;
            }

            return null;
        }
        
    }
}
