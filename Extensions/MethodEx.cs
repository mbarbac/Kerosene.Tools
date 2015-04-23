namespace Kerosene.Tools
{
	using System;
	using System.Reflection;

	// ====================================================
	/// <summary>
	/// Helpers and extensions for working with 'Method' instances.
	/// </summary>
	public static class MethodEx
	{
		/// <summary>
		/// Returns the name of this method including the C#-alike name of the type where it has been
		/// declared.
		/// </summary>
		/// <param name="method">The method to obtain its easy name from.</param>
		/// <param name="chain">True to include the declaring chain of the type where the methos has
		/// been declared, or false to use only the type's own name.</param>
		/// <param name="generic">True to include the name of the generic type arguments, if any, or
		/// false to leave them blank.</param>
		/// <returns>The easy name requested.</returns>
		public static string EasyName(this MethodBase method, bool chain = false, bool generic = false)
		{
			if (method == null) throw new NullReferenceException("Method cannot be null.");

			var name = method.Name;
			var type = method.DeclaringType;

			if (type != null) name = string.Format("{0}.{1}", type.EasyName(chain, generic), name);
			return name;
		}
	}
}
