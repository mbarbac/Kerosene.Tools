// ======================================================== ExceptionEx.cs
namespace Kerosene.Tools
{
	using System;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="System.Exception"/> instances.
	/// </summary>
	public static class ExceptionEx
	{
		/// <summary>
		/// Returns a string representation of the exception suitable to presentation purposes.
		/// </summary>
		/// <param name="e">The exception.</param>
		/// <returns>An alternate string representation suitable to presentation purposes.</returns>
		public static string ToDisplayString(this Exception e)
		{
			if (e == null) throw new NullReferenceException("Exception cannot be null.");

			StringBuilder sb = new StringBuilder(); while (e != null)
			{
				sb.Append(e.GetType().EasyName());
				if (e.Message != null) sb.AppendFormat(", {0}", e.Message);
				if (e.StackTrace != null) sb.AppendFormat("\n{0}", e.StackTrace);

				if ((e = e.InnerException) != null) sb.Append("\n----- Inner Exception: -----\n");
			}
			return sb.ToString();
		}
	}
}
// ======================================================== 
