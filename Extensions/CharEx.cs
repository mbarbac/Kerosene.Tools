using System;

namespace Kerosene.Tools
{
	// ====================================================
	/// <summary>
	/// Helpers and extensions for working with 'Char' instances.
	/// </summary>
	public static class CharEx
	{
		/// <summary>
		/// Compares the source and target chars using the given rules. Returns -1 if the source is
		/// smaller than the target, +1 if the source is bigger than the target, or 0 if both can be
		/// considered the same.
		/// </summary>
		/// <param name="source">The source instance.</param>
		/// <param name="target">The target instance.</param>
		/// <param name="comparisonType">The rules ro perform the comparison.</param>
		/// <returns>An integer than indicates the relationship between the two operands, being -1
		/// if the source is smaller than the target, +1 if the source is bigger than the target, or
		/// 0 if both can be considered the same.
		/// </returns>
		public static int CompareTo(this char source, char target, StringComparison comparisonType)
		{
			var s = source.ToString();
			var t = target.ToString();
			return string.Compare(s, t, comparisonType);
		}
	}
}
