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

using System;
using System.Collections.Generic;
using System.Text;

namespace FractalMachine.Classes
{
	public class KeyLengthSortedDescDictionary<T> : SortedDictionary<string, T>
	{
		private class StringLengthComparer : IComparer<string>
		{
			public int Compare(string x, string y)
			{
				if (x == null) throw new ArgumentNullException(nameof(x));
				if (y == null) throw new ArgumentNullException(nameof(y));
				var lengthComparison = x.Length.CompareTo(y.Length) * -1;
				return lengthComparison == 0 ? string.Compare(x, y, StringComparison.Ordinal) : lengthComparison;
			}
		}

		public KeyLengthSortedDescDictionary() : base(new StringLengthComparer()) { }
	}

	public class ListString : List<string>
    {
		public string Pop()
        {
			if (Count == 0)
				return null;

			string v = this[Count - 1];
			RemoveAt(Count - 1);
			return v;
        }

		public void AddToLast(string str)
        {
			this[Count - 1] += str;
		}
    }
}
