using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kerosene.Tools
{
	// =====================================================
	/// <summary>
	/// Options for the Object's 'Sketch()' method.
	/// </summary>
	[Flags]
	public enum SketchOptions
	{
		/// <summary>
		/// Use default settings.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Force the usage of rounded brackets for enumerable objects instead of squared ones.
		/// </summary>
		RoundedBrackets = 1,

		/// <summary>
		/// Include private members in case they are needed to generate the sketch of a given
		/// object.
		/// </summary>
		IncludePrivateMembers = 2,

		/// <summary>
		/// Include static members in case they are needed to generate the sketch of a given
		/// object.
		/// </summary>
		IncludeStaticMembers = 4,

		/// <summary>
		/// Include also fields and not only properties in case they are needed to generate the
		/// skectch of a given object.
		/// </summary>
		IncludeFields = 8,

		/// <summary>
		/// To include the type name sorrounding the contents.
		/// </summary>
		WithTypeName = 16
	}

	// =====================================================
	/// <summary>
	/// Helpers and extensions for working with 'Object' instances.
	/// </summary>
	public static class ObjectEx
	{
		/// <summary>
		/// Returns an alternate string representation of the given object.
		/// </summary>
		/// <param name="obj">The object to obtains its alternate string representation from.</param>
		/// <param name="ops">Optional options to obtain the representation.</param>
		/// <returns>The requested alternate string representation.</returns>
		public static string Sketch(this object obj, SketchOptions ops = SketchOptions.Default)
		{
			// Some pretty obvious cases...
			if (obj == null) return string.Empty;
			if (obj is string) return (string)obj;
			if (obj is char[]) return new string((char[])obj);
			if (obj is Type) return ((Type)obj).EasyName();

			// Enumerations...
			var type = obj.GetType();
			if (type.IsEnum) return obj.ToString();

			// Using an overriden ToString() method if such is available...
			var method = type.GetMethod("ToString", Type.EmptyTypes);
			if (method.DeclaringType != typeof(object)) return obj.ToString();

			// Elements to be used in the next sections...
			bool rounded = ops.HasFlag(SketchOptions.RoundedBrackets);
			char ini = rounded ? '(' : '[';
			char end = rounded ? ')' : ']';
			ops &= ~SketchOptions.RoundedBrackets; // Rounded brackest for this level only...

			StringBuilder sb = new StringBuilder();
			if (ops.HasFlag(SketchOptions.WithTypeName)) sb.AppendFormat("{0}(", type.EasyName());

			// IDictionary...
			if (obj is IDictionary)
			{
				var temp = (IDictionary)obj;

				sb.Append(ini); var first = true; foreach (DictionaryEntry kvp in temp)
				{
					if (first) first = false; else sb.Append(", ");
					sb.AppendFormat("{0} = {1}", kvp.Key.Sketch(ops), kvp.Value.Sketch(ops));
				}
				sb.Append(end);

				if (ops.HasFlag(SketchOptions.WithTypeName)) sb.Append(")");
				return sb.ToString();
			}

			// Other IEnumerable...
			if (obj is IEnumerable)
			{
				var temp = (IEnumerable)obj;
				var iter = temp.GetEnumerator();

				sb.Append(ini); var first = true; while (iter.MoveNext())
				{
					if (first) first = false; else sb.Append(", ");
					sb.Append(iter.Current.Sketch(ops));
				}
				sb.Append(end);

				if (iter is IDisposable) ((IDisposable)iter).Dispose();

				if (ops.HasFlag(SketchOptions.WithTypeName)) sb.Append(")");
				return sb.ToString();
			}


			// Using members...
			List<MemberInfo> list = new List<MemberInfo>();
			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			if (ops.HasFlag(SketchOptions.IncludePrivateMembers)) flags |= BindingFlags.NonPublic;
			if (ops.HasFlag(SketchOptions.IncludeStaticMembers)) flags |= BindingFlags.Static;

			PropertyInfo[] props = type.GetProperties(flags);
			list.AddRange(props.Where(x => x.CanRead));

			if (ops.HasFlag(SketchOptions.IncludeFields))
			{
				FieldInfo[] fields = type.GetFields(flags); foreach (var field in fields)
				{
					// Avoiding backing fields created for automatic properties...
					int n = field.CustomAttributes
						.Where(x => x.AttributeType == typeof(CompilerGeneratedAttribute)).Count();
					if (n == 0) list.Add(field);
				}
			}

			if (list.Count != 0)
			{
				var first = true; sb.Append("{"); foreach (var info in list)
				{
					object v = null; try
					{
						v = info.MemberType == MemberTypes.Field
						? ((FieldInfo)info).GetValue(obj)
						: ((PropertyInfo)info).GetValue(obj);
					}
					catch { }

					if (first) first = false; else sb.Append(", ");
					sb.AppendFormat("{0} = {1}", info.Name, v.Sketch(ops));
				}
				sb.Append("}");

				if (ops.HasFlag(SketchOptions.WithTypeName)) sb.Append(")");
				return sb.ToString();
			}

			// Default case...
			return type.EasyName();
		}

		/// <summary>
		/// Returns either a clone of the original object, if it implements the 'ICloneable' interface,
		/// or the original object itself.
		/// </summary>
		/// <param name="obj">The object to obtain a clone from.</param>
		/// <returns>Either a clone of the original object or the original one itself.</returns>
		public static object TryClone(this object obj)
		{
			if (obj == null) return null;
			if (obj is string) return obj;
			if (obj is ValueType) return obj;

			if (obj is ICloneable) return ((ICloneable)obj).Clone();
			return obj;
		}

		/// <summary>
		/// Returns either a clone of the original object, if it implements the 'ICloneable' interface,
		/// or the original object itself.
		/// </summary>
		/// <typeparam name="T">The type to cast to the result of this method.</typeparam>
		/// <param name="obj">The object to obtain a clone from.</param>
		/// <returns>Either a clone of the original object or the original one itself.</returns>
		public static T TryClone<T>(this T obj)
		{
			var temp = ((object)obj).TryClone();
			return (T)temp;
		}

		/// <summary>
		/// Converts the source object into an instance of the given target type.
		/// </summary>
		/// <param name="obj">The source object.</param>
		/// <param name="targetType">The type to convert the source object to.</param>
		/// <returns>The converted instance, or the original one if no conversion is needed.</returns>
		public static object ConvertTo(this object obj, Type targetType)
		{
			if (targetType == null) throw new ArgumentNullException("type", "Target type cannot be null.");

			if (obj == null)
			{
				if (targetType.IsNullableType()) return null;
				throw new ArgumentException(
					"Cannot convert a null source into an instance of the not-nullable '{0}' type."
					.FormatWith(targetType.EasyName()));
			}

			var sourceType = obj.GetType();
			if (sourceType == targetType) return obj;
			if (targetType.IsAssignableFrom(sourceType)) return obj;

			var converter = LocateConverterDelegate(sourceType, targetType);
			return converter.DynamicInvoke(obj);
		}

		/// <summary>
		/// Converts the source object into an instance of the given target type.
		/// </summary>
		/// <typeparam name="T">The type to convert the source object to.</typeparam>
		/// <param name="obj">The source object.</param>
		/// <returns>The converted instance, or the original one if no conversion is needed.</returns>
		public static T ConvertTo<T>(this object obj)
		{
			var temp = ConvertTo(obj, typeof(T));
			return (T)temp;
		}

		/// <summary>
		/// Creates the delegate to invoke when conversions are needed.
		/// </summary>
		/// <remarks>Following code is an adaptation of an original one proposed by Richard Deming.</remarks>
		static Delegate LocateConverterDelegate(Type sourceType, Type targetType)
		{
			string name = string.Format("{0}--{1}", sourceType.FullName, targetType.FullName);
			Delegate ret = null; if (_Converters.TryGetValue(name, out ret)) return ret;

			var input = Expression.Parameter(sourceType, "input");
			Expression body; try
			{
				body = Expression.Convert(input, targetType);
			}
			catch (InvalidOperationException)
			{
				var conversionType = Expression.Constant(targetType);
				body = Expression.Call(typeof(Convert), "ChangeType", null, input, conversionType);
			}
			var expr = Expression.Lambda(body, input);
			ret = expr.Compile();

			_Converters.Add(name, ret);
			return ret;
		}

		static Dictionary<string, Delegate> _Converters = new Dictionary<string, Delegate>();
	}
}
