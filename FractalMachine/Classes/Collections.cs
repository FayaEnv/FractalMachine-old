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
    }
}
