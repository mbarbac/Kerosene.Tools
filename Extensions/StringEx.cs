namespace Kerosene.Tools
{
	using System;

	// ====================================================
	/// <summary>
	/// Helpers and extensions for working with 'String' instances.
	/// </summary>
	public static class StringEx
	{
		/// <summary>
		/// Returns a formatted string using the source one as the format specification, along with
		/// the given optional array of arguments, if any.
		/// </summary>
		/// <param name="source">The source instance.</param>
		/// <param name="args">An optional array of arguments to be used with the format specification.</param>
		/// <returns>A new formatted string.</returns>
		public static string FormatWith(this string source, params object[] args)
		{
			if (source == null) throw new NullReferenceException("Source string cannot be null.");
			
			if (args != null) source = string.Format(source, args);
			return source;
		}

		/// <summary>
		/// Returns null if the source string is null or empty; otherwise returns the trimmed version
		/// of the original one.
		/// </summary>
		/// <param name="source">The source instance.</param>
		/// <returns>Null or a new string being the trimmed original one.</returns>
		public static string NullIfTrimmedIsEmpty(this string source)
		{
			source = source == null || ((source = source.Trim()).Length == 0) ? null : source;
			return source;
		}

		/// <summary>
		/// Returns an empty string if the source one is null or empty; otherwise returns the trimmed
		/// version of the original one.
		/// </summary>
		/// <param name="source">The source instance.</param>
		/// <returns>The trimmed original string, or an empty one.</returns>
		public static string EmptyIfTrimmedIsNull(this string source)
		{
			source = source == null || ((source = source.Trim()).Length == 0) ? string.Empty : source;
			return source;
		}

		/// <summary>
		/// Returns a new string containing the n left-most characters of the source one.
		/// </summary>
		/// <param name="source">The source instance.</param>
		/// <param name="n">The number of characters to obtain.</param>
		/// <returns>The requested string.</returns>
		public static string Left(this string source, int n)
		{
			if (source == null) throw new NullReferenceException("Source string cannot be null.");
			if (n < 0) throw new ArgumentException("Number of characters '{0}' must be cero or bigger.".FormatWith(n));

			if (source.Length == 0) return string.Empty;
			if (n == 0) return string.Empty;

			var str = (n < source.Length) ? source.Substring(0, n) : source;
			return str;
		}

		/// <summary>
		/// Returns a new string containing the n right-most characters of the source one.
		/// </summary>
		/// <param name="source">The source instance.</param>
		/// <param name="n">The number of characters to obtain.</param>
		/// <returns>The requested string.</returns>
		public static string Right(this string source, int n)
		{
			if (source == null) throw new NullReferenceException("Source string cannot be null.");
			if (n < 0) throw new ArgumentException("Number of characters '{0}' must be cero or bigger.".FormatWith(n));

			if (source.Length == 0) return string.Empty;
			if (n == 0) return string.Empty;

			var str = (n < source.Length) ? source.Substring(source.Length - n) : source;
			return str;
		}

		/// <summary>
		/// Removes from the source string the first ocurrence of the target one, if any, returning
		/// a new string with the result.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="target">The target string to remove, or null.</param>
		/// <param name="comparisonType">The rules for searching for the target string.</param>
		/// <returns>A new string with the result.</returns>
		public static string Remove(this string source, string target, StringComparison comparisonType)
		{
			if (source == null) throw new NullReferenceException("Source string cannot be null.");

			if (target == null || target == string.Empty) return source;
			if (source == string.Empty) return string.Empty;

			int start = source.IndexOf(target, comparisonType); if (start < 0) return source;
			return source.Remove(start, target.Length);
		}

		/// <summary>
		/// Removes from the source string the first ocurrence of the target one, if any, returning
		/// a new string with the result, using the current culture for comparisons.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="target">The target string to remove, or null.</param>
		/// <returns>A new string with the result.</returns>
		public static string Remove(this string source, string target)
		{
			return source.Remove(target, StringComparison.CurrentCulture);
		}

		/// <summary>
		/// Removes from the source string the last ocurrence of the target one, if any, returning
		/// a new string with the result.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="target">The target string to remove, or null.</param>
		/// <param name="comparisonType">The rules for searching for the target string.</param>
		/// <returns>A new string with the result.</returns>
		public static string RemoveLast(this string source, string target, StringComparison comparisonType)
		{
			if (source == null) throw new NullReferenceException("Source string cannot be null.");

			if (target == null || target == string.Empty) return source;
			if (source == string.Empty) return string.Empty;

			int start = source.LastIndexOf(target, comparisonType); if (start < 0) return source;
			return source.Remove(start, target.Length);
		}

		/// <summary>
		/// Removes from the source string the last ocurrence of the target one, if any, returning
		/// a new string with the result, using the current culture for comparisons.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="target">The target string to remove, or null.</param>
		/// <returns>A new string with the result.</returns>
		public static string RemoveLast(this string source, string target)
		{
			return source.RemoveLast(target, StringComparison.CurrentCulture);
		}

		/// <summary>
		/// Returns the zero-based index of the first ocurrence of the given target character in
		/// the source string, or -1 if it is not found.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="target">The character to find.</param>
		/// <param name="comparisonType">The rules to perform the comparisons.</param>
		/// <returns>The zero-based index of the first ocurrence of the given target character in
		/// the source string.</returns>
		public static int IndexOf(this string source, char target, StringComparison comparisonType)
		{
			if (source == null) throw new NullReferenceException("Source string cannot be null.");
			return source.IndexOf(target.ToString(), comparisonType);
		}

		/// <summary>
		/// Returns the zero-based index of the first ocurrence of any of the given targets, or -1 if
		/// no target can be found in the source string.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="targets">The characters to find.</param>
		/// <param name="comparisonType">The rules to perform the comparisons.</param>
		/// <returns>The zero-based index of the first ocurrence of any of the given targets, or -1 if
		/// no target can be found in the source string.</returns>
		public static int IndexOfAny(this string source, char[] targets, StringComparison comparisonType)
		{
			if (source == null) throw new NullReferenceException("Source string cannot be null.");
			if (targets == null) throw new ArgumentNullException("targets", "Array of characters is null.");

			if (source.Length == 0) return -1;
			if (targets.Length == 0) return -1;

			for (int i = 0; i < targets.Length; i++)
			{
				int k = source.IndexOf(targets[i], comparisonType);
				if (k >= 0) return k;
			}

			return -1;
		}

		/// <summary>
		/// Returns the zero-based index of the first ocurrence in the source string that cannot be
		/// considered as a valid character.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="valids">The only valid characters.</param>
		/// <param name="comparisonType">The rules to perform the comparisons.</param>
		/// <returns>The zero-based index of the first ocurrence in the source string that cannot be
		/// considered as a valid character.</returns>
		public static int IndexOfNotValid(this string source, char[] valids, StringComparison comparisonType)
		{
			if (source == null) throw new NullReferenceException("Source string cannot be null.");
			if (valids == null) throw new ArgumentNullException("any", "Array of characters is null.");

			if (source.Length == 0) return -1; // Empty string has no invalid chars...
			if (valids.Length == 0) return 0; // Means no valid characters whatsoever...

			var temp = new string(valids); for (int i = 0; i < source.Length; i++)
			{
				int k = temp.IndexOf(source[i].ToString(), comparisonType);
				if (k < 0) return i;
			}

			return -1;
		}

		/// <summary>
		/// Returns the zero-based index of the first ocurrence in the source string that cannot be
		/// considered as a valid character.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="valids">The only valid characters.</param>
		/// <returns>The zero-based index of the first ocurrence in the source string that cannot be
		/// considered as a valid character.</returns>
		public static int IndexOfNotValid(this string source, char[] valids)
		{
			return source.IndexOfNotValid(valids, StringComparison.CurrentCulture);
		}

		/// <summary>
		/// Returns a new validated string using the rules given, or throws an exception if any
		/// rule is not met.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="desc">A description of the source string for presentation purposes.</param>
		/// <param name="canbeNull">True is null strings are considered valid.</param>
		/// <param name="canbeEmpty">True is empty strings are considered valid.</param>
		/// <param name="emptyAsNull">True to return null in case of an empty source string.</param>
		/// <param name="trim">True to trim the source string.</param>
		/// <param name="trimStart">True to trim the source string from the left.</param>
		/// <param name="trimEnd">True to trim the source string from the right.</param>
		/// <param name="padLeft">If not cero the character to use to left-pad the source string.</param>
		/// <param name="padRight">If not cero the character to use to right-pad the source string.</param>
		/// <param name="minLen">If possitive the minimum lenght of the resulting string.</param>
		/// <param name="maxLen">If possitive the maximun lenght of the resulting string.</param>
		/// <param name="valids">If not null an array containing explicitly the valid characters.</param>
		/// <param name="invalids">If not null an array containing explicitly the invalid characters.</param>
		/// <param name="comparisonType">The rules for performing comparisons.</param>
		/// <returns>The requested validated string.</returns>
		public static string Validated(
			this string source, string desc = null,
			bool canbeNull = false, bool canbeEmpty = false, bool emptyAsNull = false,
			bool trim = true, bool trimStart = false, bool trimEnd = false,
			char padLeft = '\0', char padRight = '\0',
			int minLen = -1, int maxLen = -1,
			char[] valids = null, char[] invalids = null,
			StringComparison comparisonType = StringComparison.CurrentCulture)
		{
			if ((desc = desc.NullIfTrimmedIsEmpty()) == null) desc = "Source";

			if (emptyAsNull) canbeNull = true;
			if (source == null)
			{
				if (!canbeNull) throw new ArgumentNullException("source", "{0} cannot be null".FormatWith(desc));
				return null;
			}

			if (!canbeEmpty || emptyAsNull) trim = true;
			if (trim) source = source.Trim();
			else
			{
				if (trimStart) source = source.TrimStart(' ');
				if (trimEnd) source = source.TrimEnd(' ');
			}
			if (minLen > 0)
			{
				if (padLeft != '\0') source = source.PadLeft(minLen, padLeft);
				if (padRight != '\0') source = source.PadRight(minLen, padRight);
			}
			if (maxLen > 0)
			{
				if (padLeft != '\0') source = source.PadLeft(maxLen, padLeft);
				if (padRight != '\0') source = source.PadRight(maxLen, padRight);
			}

			if (source.Length == 0)
			{
				if (emptyAsNull) return null;
				if (canbeEmpty) return string.Empty;
				throw new EmptyException("{0} cannot be empty.".FormatWith(desc));
			}

			if (minLen >= 0 && source.Length < minLen) throw new ArgumentException(
				"Lenght of {0} '{1}' is less than {2}."
				.FormatWith(desc, source, minLen));

			if (maxLen >= 0 && source.Length > maxLen) throw new ArgumentException(
				"Lenght of {0} '{1}' is bigger than {2}."
				.FormatWith(desc, source, maxLen));

			if (invalids != null)
			{
				int i = source.IndexOfAny(invalids, comparisonType);
				if (i >= 0) throw new ArgumentException(
					"{0} '{1}' contains invalid character '{2}'."
					.FormatWith(desc, source, source[i]));
			}
			if (valids != null)
			{
				int i = source.IndexOfNotValid(valids, comparisonType);
				if (i >= 0) throw new ArgumentException(
					"{0} '{1}' contains invalid character '{2}'."
					.FormatWith(desc, source, source[i]));
			}

			return source;
		}
	}
}
