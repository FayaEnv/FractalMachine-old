using FractalMachine.Code;
using System;
using System.Collections.Generic;

namespace FractalMachine.Classes
{
    public static class Extensions
    {
        #region List

        /// <summary>
        /// Get the last (of specified in pos) element and remove it from the list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="Pos">If negative starts from the end</param>
        /// <returns></returns>
        public static T Pull<T>(this List<T> obj, int Pos = -1, bool Remove = true)
        {
            var c = obj.Count;

            if (c > 0)
            {
                int p = Pos;
                if (Pos < 0)
                    p = c + Pos;

                var s = obj[p];
                if (Remove) obj.RemoveAt(p);
                return s;
            }

            return default(T);
        }

        #endregion

        #region Marks

        public static bool HasMark(this string obj)
        {
            return obj.Length >= Properties.StringMark.Length && obj.Substring(2, Properties.Mark.Length) == Properties.Mark;
        }

        public static bool HasStringMark(this string obj)
        {
            return obj.StartsWith(Properties.StringMark);
        }

        public static bool HasAngularBracketMark(this string obj)
        {
            return obj.StartsWith(Properties.AngularBracketsMark);
        }

        public static string NoMark(this string obj)
        {
            if (obj.HasMark())
                return obj.Substring(Properties.StringMark.Length);
            else
                return obj;
        }

        #endregion
    }
}
