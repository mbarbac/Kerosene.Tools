// ======================================================== TypeEx.cs
namespace Kerosene.Tools
{
	using System;
	using System.Reflection;
	using System.Runtime.CompilerServices;

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="System.Type"/> instances.
	/// </summary>
	public static class TypeEx
	{
		/// <summary>
		/// Returns the C#-alike name of the type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="depth">The depth of the declaring chain to be included in the name of
		/// tye type:
		/// <para>- 0: only to include the name of the type.</para>
		/// <para>- 1: to also include the name of the namespace or type where it is declared.</para>
		/// <para>- n: include to the nth-level the names in the declaring chain.</para>
		/// <para>- Use 'int.MaxValue' to assure the full path is included.</para>
		/// </param>
		/// <param name="genericNames">True to include the names of the generic type arguments, if
		/// any, or false to leave them blank.</param>
		/// <returns>The C#-alike name of the given type.</returns>
		public static string EasyName(this Type type, int depth = 0, bool genericNames = false)
		{
			if (type == null) throw new NullReferenceException("Type cannot be null.");
			if (depth < 0) depth = -1 * depth;
			if (depth >= int.MaxValue) depth = int.MaxValue - 2;

			var str = type.FullName;
			if (str == null) str = genericNames ? type.Name : string.Empty;

			var i = str.IndexOf('[');
			if (i >= 0) str = str.Substring(0, i); // CLR decoration not C# compliant...

			var args = type.GetGenericArguments();
			var index = 0;
			var parts = str.Split(".+".ToCharArray()); for (int k = 0; k < parts.Length; k++)
			{
				i = parts[k].IndexOf('`'); if (i >= 0)
				{
					var temps = parts[k].Split("`".ToCharArray());
					parts[k] = temps[0] + "<";

					var num = int.Parse(temps[1]); for (int j = 0; j < num; j++)
					{
						if (j != 0) parts[k] += ",";

						var arg = args[index++];
						var name = arg.EasyName(depth, genericNames);

						if (name != string.Empty)
						{
							if (j != 0) parts[k] += " ";
							parts[k] += name;
						}
					}
					parts[k] += ">";
				}
			}

			int start = parts.Length - 1 - depth; if (start < 0) start = 0;
			int count = parts.Length - start;
			return string.Join(".", parts, start, count);
		}

		/// <summary>
		/// Returns whether the given type is nullable or not.
		/// </summary>
		/// <param name="type">The type to test.</param>
		/// <returns>True if the type is nullable, false otherwise.</returns>
		public static bool IsNullableType(this Type type)
		{
			if (type == null) throw new NullReferenceException("Type cannot be null.");

			if (type.IsClass) return true;

			Type generic = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
			if (generic != null && generic.Equals(typeof(Nullable<>))) return true;

			return false;
		}

		/// <summary>
		/// Gets whether the given type is an anonymous one or not.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>True if the given type is an anonymous one.</returns>
		public static bool IsAnonymousType(this Type type)
		{
			if (type == null) throw new NullReferenceException("Type cannot be null.");

			return Attribute.IsDefined(
				type,
				typeof(CompilerGeneratedAttribute),
				false);
		}

		/// <summary>
		/// Binding flags for elements that are public and hidden.
		/// </summary>
		public const BindingFlags PublicAndHidden = BindingFlags.Public | BindingFlags.NonPublic;

		/// <summary>
		/// Binding flags for instance elements that are public and hidden.
		/// </summary>
		public const BindingFlags InstancePublicAndHidden = BindingFlags.Instance | PublicAndHidden;

		/// <summary>
		/// Binding flags for instance and static elements.
		/// </summary>
		public const BindingFlags InstanceAndStatic = BindingFlags.Instance | BindingFlags.Static;
	}
}
// ======================================================== 
