using System;
using System.Collections.Generic;

namespace FractalMachine.Classes
{
    public static class Extensions
    {
        /// <summary>
        /// Get the last (of specified in pos) element and remove it from the list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="Pos">If negative starts from the end</param>
        /// <returns></returns>
        public static T Pull<T>(List<T> obj, int Pos = -1)
        {
            var c = obj.Count;

            if (c > 0)
            {
                if(Pos < 0)
                    Pos = c + Pos;

                var s = obj[Pos];
                obj.RemoveAt(Pos);
                return s;
            }

            return default(T);
        }

    }
}
