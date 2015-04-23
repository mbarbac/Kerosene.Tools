namespace Kerosene.Tools
{
	using System;
	using System.Reflection;
	using System.Runtime.CompilerServices;

	// ====================================================
	/// <summary>
	/// Helpers and extensions for working with 'Type' instances.
	/// </summary>
	public static class TypeEx
	{
		/// <summary>
		/// returns the C#-alike name of the given type.
		/// </summary>
		/// <param name="type">The type to obtain its easy name from.</param>
		/// <param name="chain">True to include the declaring chain, or false to use only the type's
		/// own name.</param>
		/// <param name="generic">True to include the name of the generic type arguments, if any, or
		/// false to leave them blank.</param>
		/// <returns>The easy name requested.</returns>
		public static string EasyName(this Type type, bool chain = false, bool generic = false)
		{
			if (type == null) throw new NullReferenceException("Type cannot be null.");

			var str = type.FullName;
			if (str == null) str = generic ? type.Name : string.Empty;

			var i = str.IndexOf('[');
			if (i >= 0) str = str.Substring(0, i); // CLR decoration not C# compliant...

			var args = type.GetGenericArguments();
			var argx = 0;
			var parts = str.Split(new[] { '.', '+' }); for (int k = 0; k < parts.Length; k++)
			{
				i = parts[k].IndexOf('`'); if (i >= 0)
				{
					var temps = parts[k].Split(new[] { '`' });
					parts[k] = temps[0] + "<";

					var num = int.Parse(temps[1]); for (int j = 0; j < num; j++)
					{
						if (j != 0) parts[k] += ",";

						var arg = args[argx++];
						var name = arg.EasyName(chain, generic);

						if (name != string.Empty)
						{
							if (j != 0) parts[k] += " ";
							parts[k] += name;
						}
					}
					parts[k] += ">";
				}
			}

			str = chain ? string.Join(".", parts) : parts[parts.Length - 1];
			return str;
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
