// ======================================================== CharEx.cs
namespace Kerosene.Tools
{
	using System;

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="System.Char"/> instances.
	/// </summary>
	public static class CharEx
	{
		/// <summary>
		/// Returns a number that indicates the position of the source instance in the sort
		/// order in relation to the target one given: -1 if the source one is smaller than
		/// the target one, +1 if the source one is bigger than the target one, and 0 if both
		/// can be considered equal.
		/// </summary>
		/// <param name="source">The source instance.</param>
		/// <param name="target">The target instance to compare the source one with.</param>
		/// <param name="comparisonType">The rules for comparing the values.</param>
		/// <returns>Returns -1 if the source instance is smaller than the target one, +1 if
		/// the source instance is bigger than the target one, and 0 if both can be considered
		/// equal.</returns>
		public static int CompareTo(this char source, char target, StringComparison comparisonType)
		{
			var s = source.ToString();
			var t = target.ToString();
			return string.Compare(s, t, comparisonType);
		}
	}
}
// ======================================================== 
