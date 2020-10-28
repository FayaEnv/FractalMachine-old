/*
   Copyright 2020 (c) Riccardo Cecchini
   
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using FractalMachine.Code;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

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

        public static List<T> Clone<T>(this List<T> listToClone) 
        {
            return listToClone.Select(item => item).ToList();
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

        #region JSON.Net

        public static JObject ToJObject<T>(this Dictionary<string, T> dict)
        {
            JObject res = new JObject();
            foreach(var kp in dict)
                res[kp.Key] = new JObject(kp.Value);
            return res;
        }

        #endregion
    }
}
