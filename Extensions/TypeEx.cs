using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Kerosene.Tools
{
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
		/// <param name="genericNames">True to include the name of the generic type arguments, if any, or
		/// false to leave them blank.</param>
		/// <param name="nonGenericNames">True to include the name of the non-generic type arguments, if any,
		/// or false to leave them blank.</param>
		/// <returns>The easy name requested.</returns>
		public static string EasyName(
			this Type type,
			bool chain = false,
			bool genericNames = false,
			bool nonGenericNames = true)
		{
			if (type == null) throw new NullReferenceException("Type cannot be null.");

			var str = type.FullName ?? type.Name ?? string.Empty;

			var i = str.IndexOf('[');
			if (i >= 0) str = str.Substring(0, i); // CLR decoration not C# compliant...

			if (!type.IsGenericParameter)
			{
				var space = type.Namespace + ".";
				if (chain && !str.StartsWith(space)) str = space + str;
			}

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
						var name = (string)null;

						if (arg.IsGenericParameter)
						{
							if (genericNames) name = arg.EasyName(chain, genericNames, nonGenericNames);
						}
						else
						{
							if (nonGenericNames) name = arg.EasyName(chain, genericNames, nonGenericNames);
						}

						if ((name = name.NullIfTrimmedIsEmpty()) != null)
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

		/// <summary>
		/// Binding flags for public and hidden elements in the instance and in the hierarchy.
		/// </summary>
		public const BindingFlags FlattenInstancePublicAndHidden = InstancePublicAndHidden | BindingFlags.FlattenHierarchy;

		/// <summary>
		/// Returns whether the type implements the given base type, including base types with
		/// an arbitrary number of generic of non-generic arguments.
		/// </summary>
		/// <param name="type">The type to test.</param>
		/// <param name="parent">The base type.</param>
		/// <returns>True if the type implements the base type, false otherwise.</returns>
		public static bool Implements(this Type type, Type parent)
		{
			if (type == null) throw new ArgumentNullException("type", "Type to test cannot be null.");
			if (parent == null) throw new ArgumentNullException("parent", "Parent type cannot be null.");

			var stype = ImplementChain(type, true);
			var sbase = ImplementChain(parent, true);
			if (stype.StartsWith(sbase)) return true;

			stype = ImplementChain(type, false);
			if (stype.StartsWith(sbase)) return true;

			var temp = type.BaseType;
			if (temp != null && temp.Implements(parent)) return true;

			var ifaces = type.GetInterfaces();
			foreach (var iface in ifaces) if (iface.Implements(parent)) return true;

			return false;
		}

		/// <summary>
		/// Helper method to obtain the string representation of the inheritance chain.
		/// </summary>
		static string ImplementChain(Type type, bool nonGenericNames)
		{
			var parent = type.BaseType;
			var str = parent == null ? type.Namespace : ImplementChain(parent, nonGenericNames);

			var name = type.Name;
			var n = name.IndexOf('`'); if (n >= 0) name = name.Substring(0, n);
			str = string.Format("{0}.{1}", str, name);

			var args = type.GetGenericArguments(); if (args.Length != 0)
			{
				str += "<"; for (int i = 0; i < args.Length; i++)
				{
					if (i != 0) str += ",";

					var arg = args[i]; if (!arg.IsGenericParameter && nonGenericNames)
					{
						if (i != 0) str += " ";
						str += ImplementChain(arg, nonGenericNames);
					}
				}
				str += ">";
			}
			return str;
		}
	}
}

