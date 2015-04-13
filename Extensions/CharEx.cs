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
		/// Compares the source and target chars using the given rules.
		/// <para>Returns -1 if the source is smaller than the target, +1 if the source is bigger
		/// than the target, or 0 if both are equal.</para>
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="target">The target.</param>
		/// <param name="comparisonType">The rules for comparing the source and target.</param>
		/// <returns>-1 if the source is smaller than the target, +1 if the source is bigger than
		/// the target, or 0 if both are equal.</returns>
		public static int CompareTo(this char source, char target, StringComparison comparisonType)
		{
			var s = source.ToString();
			var t = target.ToString();
			return string.Compare(s, t, comparisonType);
		}
	}
}
// ======================================================== 
