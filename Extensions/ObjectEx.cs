// ======================================================== ObjectEx.cs
namespace Kerosene.Tools
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Options for the Sketch method.
	/// </summary>
	[Flags]
	public enum SketchOptions
	{
		/// <summary>
		/// Use default settings.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Force the usage of rounded brackets for enumerable objects.
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
	}

	// ==================================================== 
	/// <summary>
	/// Helpers and extensions for working with <see cref="System.Object"/> instances.
	/// </summary>
	public static class ObjectEx
	{
		/// <summary>
		/// Returns an alternate string representation of the given object.
		/// </summary>
		/// <param name="obj">The object to obtains its alternate string representation from.</param>
		/// <param name="op">Optional options to obtain the representation.</param>
		/// <returns>The requested alternate string representation.</returns>
		public static string Sketch(this object obj, SketchOptions op = SketchOptions.Default)
		{
			// Some pretty obvious cases...
			if (obj == null) return string.Empty;
			if (obj is string) return (string)obj;
			if (obj is char[]) return new string((char[])obj);

			// Enumerations...
			var type = obj.GetType();
			if (type.IsEnum) return obj.ToString();

			// Using an overriden ToString() method if such is available...
			var method = type.GetMethod("ToString", Type.EmptyTypes);
			if (method.DeclaringType != typeof(object)) return obj.ToString();

			// Elements to be used in the next sections...
			bool rounded = op.HasFlag(SketchOptions.RoundedBrackets);
			char ini = rounded ? '(' : '[';
			char end = rounded ? ')' : ']';
			StringBuilder sb = new StringBuilder();

			// IDictionary...
			if (obj is IDictionary)
			{
				var temp = (IDictionary)obj;

				sb.Append(ini); var first = true; foreach (DictionaryEntry kvp in temp)
				{
					if (first) first = false; else sb.Append(", ");
					sb.AppendFormat("{0} = {1}", kvp.Key.Sketch(op), kvp.Value.Sketch(op));
				}
				sb.Append(end);

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
					sb.Append(iter.Current.Sketch(op));
				}
				sb.Append(end);

				if (iter is IDisposable) ((IDisposable)iter).Dispose();
				return sb.ToString();
			}

			// Using members...
			List<MemberInfo> list = new List<MemberInfo>();
			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			if (op.HasFlag(SketchOptions.IncludePrivateMembers)) flags |= BindingFlags.NonPublic;
			if (op.HasFlag(SketchOptions.IncludeStaticMembers)) flags |= BindingFlags.Static;

			PropertyInfo[] props = type.GetProperties(flags); list.AddRange(props.Where(x => x.CanRead));
			FieldInfo[] fields = type.GetFields(flags); foreach (var field in fields)
			{
				// To avoid backing fields...
				if (field.CustomAttributes
					.Where(x => x.AttributeType == typeof(CompilerGeneratedAttribute))
					.Count() != 0)
					continue;

				list.Add(field);
			}

			if (list.Count != 0)
			{
				var first = true; sb.Append("{"); foreach (var info in list)
				{
					object v = info.MemberType == MemberTypes.Field
						? ((FieldInfo)info).GetValue(obj)
						: ((PropertyInfo)info).GetValue(obj);

					if (first) first = false; else sb.Append(", ");
					sb.AppendFormat("{0} = {1}", info.Name, v.Sketch(op));
				}
				sb.Append("}"); return sb.ToString();
			}

			// Default case...
			return type.EasyName();
		}

		/// <summary>
		/// Returns either a clone of the original object or, if it does not implement the
		/// <see cref="ICloneable"/> interface, the original instance itself.
		/// </summary>
		/// <param name="obj">The source object.</param>
		/// <param name="extended">If true then a parameterless 'Clone()' method used, if any
		/// exists in the object's type.</param>
		/// <returns>Either a clone of the source object or the original object itself.</returns>
		public static object TryClone(this object obj, bool extended = false)
		{
			if (obj == null) return null;
			if (obj is string) return obj;
			if (obj is ValueType) return obj;

			if (obj is ICloneable) return ((ICloneable)obj).Clone();
			if (extended)
			{
				var info = obj.GetType().GetMethod("Clone", Type.EmptyTypes);
				if (info != null && info.ReturnType != typeof(void))
					obj = info.Invoke(obj, null);
			}
			return obj;
		}

		/// <summary>
		/// Returns either a clone of the original object or, if it does not implement the
		/// <see cref="ICloneable"/> interface, the original instance itself.
		/// </summary>
		/// <typeparam name="T">The type of the object to clone and return.</typeparam>
		/// <param name="obj">The source object.</param>
		/// <param name="extended">If true then a parameterless 'Clone()' method used, if any
		/// exists in the object's type.</param>
		/// <returns>Either a clone of the source object or the original object itself.</returns>
		public static T TryClone<T>(this T obj, bool extended = false)
		{
			var temp = ((object)obj).TryClone(extended);
			return (T)temp;
		}

		static Delegate CreateConverterDelegate(Type sourceType, Type targetType)
		{
			// Creates the delegate to invoke when conversions are needed.
			// The following code is an adaptation of an original one of Richard Deeming.

			string name = string.Format("{0}--{1}", sourceType.FullName, targetType.FullName);
			Delegate ret = null;
			if (_Converters.TryGetValue(name, out ret)) return ret;

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

		/// <summary>
		/// Converts the source object into an instance of the target type given.
		/// </summary>
		/// <param name="obj">The source object.</param>
		/// <param name="type">The type to convert the source object to.</param>
		/// <returns>The converted instance, or the original one if no conversion is needed.</returns>
		public static object ConvertTo(this object obj, Type type)
		{
			if (type == null) throw new ArgumentNullException("type", "Target type cannot be null.");

			if (obj == null)
			{
				if (type.IsNullableType()) return null;
				throw new ArgumentException(
					"Cannot convert a null source into an instance of the not-nullable '{0}' type."
					.FormatWith(type.EasyName()));
			}

			Type source = obj.GetType();
			if (source == type) return obj;
			if (type.IsAssignableFrom(source)) return obj;

			Delegate converter = CreateConverterDelegate(source, type);
			return converter.DynamicInvoke(obj);
		}

		/// <summary>
		/// Converts the source object into an instance of the target type given.
		/// </summary>
		/// <typeparam name="T">The type to convert the source object to.</typeparam>
		/// <param name="obj">The source object.</param>
		/// <returns>The converted instance, or the original one if no conversion is needed.</returns>
		public static T ConvertTo<T>(this object obj)
		{
			var temp = ConvertTo(obj, typeof(T));
			return (T)temp;
		}
	}
}
// ======================================================== 
