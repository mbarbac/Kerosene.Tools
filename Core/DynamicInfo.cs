// ======================================================== DynamicInfo.cs
namespace Kerosene.Tools
{
	using Microsoft.CSharp.RuntimeBinder;
	using System;
	using System.Dynamic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Runtime.CompilerServices;

	// ==================================================== 
	/// <summary>
	/// Represent an unified way to treat both properties and fields of a given instance when
	/// its structure and members are non known at compile time, or when they have to be
	/// discovered at run-time.
	/// </summary>
	public static partial class DynamicInfo
	{
		/// <summary>
		/// Returns the name of the element the dynamic lambda expression resolves into.
		/// </summary>
		/// <param name="element">The dynamic lambda expression that resolves into the name of
		/// the element.</param>
		/// <returns>The name of the requested element.</returns>
		public static string ParseName(Func<dynamic, object> element)
		{
			string name = null;
			Exception e = TryParseName(element, out name);

			if (e != null) throw e;
			return name;
		}

		/// <summary>
		/// Reads from the given host the value of the element whose name is obtained parsing the
		/// dynamic lambda expression given.
		/// </summary>
		/// <param name="host">The instance that ultimately hosts the member to read.</param>
		/// <param name="element">A dynamic lambda expression that resolves into the name of the
		/// element.</param>
		/// <param name="flags">The binding flags to use to find the element.</param>
		/// <returns>The value read from the requested element.</returns>
		public static object Read(object host, Func<dynamic, object> element, BindingFlags flags = TypeEx.InstancePublicAndHidden)
		{
			object value = null;
			Exception e = TryRead(host, element, out value, flags);

			if (e != null) throw e;
			return value;
		}

		/// <summary>
		/// Writes into the given host the value of the element whose name is obtained parsing the
		/// dynamic lambda expression given.
		/// </summary>
		/// <param name="host">The instance that ultimately hosts the member to write into.</param>
		/// <param name="element">A dynamic lambda expression that resolves into the name of the
		/// element.</param>
		/// <param name="value">The value to write into the element.</param>
		/// <param name="flags">The binding flags to use to find the element.</param>
		public static void Write(object host, Func<dynamic, object> element, object value, BindingFlags flags = TypeEx.InstancePublicAndHidden)
		{
			Exception e = TryWrite(host, element, value, flags);
			if (e != null) throw e;
		}
	}

	// ==================================================== 
	public static partial class DynamicInfo
	{
		/// <summary>
		/// Tries to parse the element name the dynamic lambda expression resolves into, setting
		/// output string name argument and returning null in case of success, or returning an
		/// exception if any error has happened.
		/// </summary>
		/// <param name="element">The dynamic lambda expression that resolves into the name of
		/// element.</param>
		/// <param name="name">The output string parameter to hold the result of the parsing.</param>
		/// <returns>Null in case of success, or an exception describing the error.</returns>
		public static Exception TryParseName(Func<dynamic, object> element, out string name)
		{
			name = null;

			if (element == null) return new ArgumentNullException("element", "Element specification cannot be null.");

			DynamicParser parser = null;
			object result = null;

			try { parser = DynamicParser.Parse(element); }
			catch (Exception e)
			{
				if (parser != null) parser.Dispose();
				return e;
			}
			if ((result = parser.Result) == null)
			{
				parser.Dispose();
				return new EmptyException("Expression '{0}' cannot resolve into null.".FormatWith(parser));
			}

			while (true)
			{
				if (result is string)
				{
					name = ((string)result).NullIfTrimmedIsEmpty(); if (name == null)
					{
						parser.Dispose();
						return new EmptyException("Element name cannot resolve into an empty one.");
					}
					break;
				}

				if (result is DynamicNode.GetMember)
				{
					var node = (DynamicNode.GetMember)result;
					name = name == null ? node.Name : string.Format("{0}.{1}", node.Name, name);

					result = node.Host; if (result is DynamicNode.Argument) break;
					continue;
				}

				parser.Dispose();
				return new ArgumentException("Invalid name expression '{0}'.".FormatWith(parser));
			}

			var list = parser.DynamicArguments.ToList();
			var tag = list[0].Name;
			if (name == tag)
			{
				parser.Dispose();
				return new ArgumentException("Expression '{0}' cannot resolve into its argument name.".FormatWith(parser));
			}

			tag = tag + ".";
			if (name.StartsWith(tag)) name = name.Substring(tag.Length);

			parser.Dispose();
			return null;
		}

		/// <summary>
		/// Tries to read the value of the element whose name is obtained by parsing the dynamic
		/// lambda expression given. Sets the output value argument and returns null in case of
		/// success, or returns an exception if any error has happened.
		/// </summary>
		/// <param name="host">The instance that ultimately hosts the element.</param>
		/// <param name="element">A dynamic lambda expression that resolves into the name of the
		/// element.</param>
		/// <param name="value">The output value argument to hold the result of the reading.</param>
		/// <param name="flags">The binding flags to use to find the element.</param>
		/// <returns>Null in case of success, or an exception describing the error.</returns>
		public static Exception TryRead(object host, Func<dynamic, object> element, out object value, BindingFlags flags = TypeEx.InstancePublicAndHidden)
		{
			value = null;
			if (host == null) return new ArgumentNullException("host", "Host instance cannot be null.");

			var type = host.GetType();
			string name = null;
			Exception e = TryParseName(element, out name); if (e != null) return e;

			int index = name.LastIndexOf('.'); if (index >= 0)
			{
				string pname = name.Left(index);
				e = TryRead(host, x => pname, out host, flags);
				if (e != null) return e;
				if (host == null) return new EmptyException("Parent element '{0}' is empty.".FormatWith(pname));

				type = host.GetType();
				name = name.Right(name.Length - index - 1);
			}

			Type other = type; while (other != null)
			{
				var prop = other.GetProperty(name, flags);
				if (prop != null)
				{
					if (!prop.CanRead) return new CannotExecuteException("Element '{0}' cannot be read.".FormatWith(name));
					value = prop.GetValue(host);
					return null;
				}

				var field = other.GetField(name, flags);
				if (field != null)
				{
					value = field.GetValue(host);
					return null;
				}

				other = other.BaseType;
			}

			if (host is IDynamicMetaObjectProvider)
			{
				var node = host as IDynamicMetaObjectProvider;
				var par = Expression.Parameter(type);
				var names = node.GetMetaObject(par).GetDynamicMemberNames().ToList();

				if (names.Contains(name))
				{
					var args = new[] { CSharpArgumentInfo.Create(0, null) };
					var member = Microsoft.CSharp.RuntimeBinder.Binder.GetMember(0, name, type, args);
					var site = CallSite<Func<CallSite, object, object>>.Create(member);

					value = site.Target(site, host);
					return null;
				}
			}

			return new NotFoundException(
				"Element '{0}' not found in {1}({2}).".FormatWith(name, type.EasyName(), host.Sketch()));
		}

		/// <summary>
		/// Tries to write into the element whose name is obtained by parsing the dynamic lambda
		/// lambda expression given the given value. Returns null in case of success, or returns
		/// an exception if any error has happened.
		/// </summary>
		/// <param name="host">The instance that ultimately hosts the element.</param>
		/// <param name="element">A dynamic lambda expression that resolves into the name of the
		/// element.</param>
		/// <param name="value">The value to write into the element.</param>
		/// <param name="flags">The binding flags to use to find the element.</param>
		/// <returns>Null in case of success, or an exception describing the error.</returns>
		public static Exception TryWrite(object host, Func<dynamic, object> element, object value, BindingFlags flags = TypeEx.InstancePublicAndHidden)
		{
			if (host == null) return new ArgumentNullException("host", "Host instance cannot be null.");
			var type = host.GetType();

			string name = null;
			Exception e = TryParseName(element, out name); if (e != null) return e;

			int index = name.LastIndexOf('.'); if (index >= 0)
			{
				string pname = name.Left(index);
				e = TryRead(host, x => pname, out host, flags);
				if (e != null) return e;
				if (host == null) return new EmptyException("Parent element '{0}' is empty.".FormatWith(pname));

				type = host.GetType();
				name = name.Right(name.Length - index - 1);
			}

			Type other = type; while (other != null)
			{
				var prop = other.GetProperty(name, flags);
				if (prop != null)
				{
					if (!prop.CanWrite) return new CannotExecuteException("Element '{0}' cannot be written.".FormatWith(name));
					prop.SetValue(host, value);
					return null;
				}

				var field = other.GetField(name, flags);
				if (field != null)
				{
					field.SetValue(host, value);
					return null;
				}

				other = other.BaseType;
			}

			if (host is IDynamicMetaObjectProvider)
			{
				var node = host as IDynamicMetaObjectProvider;
				var par = Expression.Parameter(type);
				var names = node.GetMetaObject(par).GetDynamicMemberNames().ToList();

				if (names.Contains(name))
				{
					var args = new[] { CSharpArgumentInfo.Create(0, null), CSharpArgumentInfo.Create(0, null) };
					var member = Microsoft.CSharp.RuntimeBinder.Binder.SetMember(0, name, type, args);
					var site = CallSite<Func<CallSite, object, object, object>>.Create(member);

					site.Target(site, host, value);
					return null;
				}
			}

			return new NotFoundException(
				"Element '{0}' not found in {1}({2}).".FormatWith(name, type.EasyName(), host.Sketch()));
		}
	}
}
// ======================================================== 
