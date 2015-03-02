// ======================================================== MethodEx.cs
namespace Kerosene.Tools
{
	using System;
	using System.Diagnostics;
	using System.Reflection;

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="System.Reflection.MethodBase"/>
	/// instances.
	/// </summary>
	public static class MethodEx
	{
		/// <summary>
		/// Gets the method at the given position in the calling stack, or null if this
		/// information is not available.
		/// </summary>
		/// <param name="depth">The depth into the calling stack:
		/// <para>- 0: the current <see cref="MethodonStack"/> method.</para>
		/// <para>- 1: the method that has called this one.</para>
		/// <para>- 2: the method from which the caller of this one was invoked.</para>
		/// <para>Etc...</para>
		/// </param>
		/// <returns>The method reference, or null.</returns>
		public static MethodBase MethodOnStack(uint depth = 1)
		{
			var stack = new StackTrace();
			var frame = stack.GetFrame((int)depth);

			return frame == null ? null : frame.GetMethod();
		}

		/// <summary>
		/// Returns the name of this method, including the C#-alike name of the type that declares
		/// it.
		/// </summary>
		/// <param name="method"></param>
		/// <param name="depth">The depth of the declaring chain to be included in the name of
		/// type:
		/// <para>- 0: only to include the name of the type.</para>
		/// <para>- 1: to also include the name of the namespace or type where it is declared.</para>
		/// <para>- n: include to the nth-level the names in the declaring chain.</para>
		/// </param>
		/// <param name="genericNames">True to include the names of the generic type arguments, if
		/// any, or false to leave them blank.</param>
		/// <returns>The extended name of the method.</returns>
		public static string ExtendedName(this MethodBase method, int depth = 0, bool genericNames = false)
		{
			if (method == null) throw new NullReferenceException("Method cannot be null.");

			var name = method.Name;
			var type = method.DeclaringType;

			if (type != null) name = string.Format("{0}.{1}",
				type.EasyName(depth, genericNames),
				name);

			return name;
		}
	}
}
// ======================================================== 
