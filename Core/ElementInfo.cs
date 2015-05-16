using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Kerosene.Tools
{
	// ====================================================
	/// <summary>
	/// Provides an symetric way for treating both the fields and properties of a given type,
	/// collectively known as 'members' for the operations of this class and related ones.
	/// </summary>
	public partial class ElementInfo : IDisposableEx, ICloneable
	{
		private const bool DEFAULT_DISPOSE_PARENT = false;
		bool _IsDisposed = false;
		MemberInfo _MemberInfo = null;
		ElementInfo _Parent = null;

		private ElementInfo() { }

		/// <summary>
		/// Validates that the given member info is a field or a non-indexed property, throwing
		/// an exception otherwise.
		/// </summary>
		private void ValidateMemberType(MemberInfo info)
		{
			if (info.MemberType == MemberTypes.Property)
			{
				var pars = ((PropertyInfo)info).GetIndexParameters();
				if (pars.Length != 0) throw new NotSupportedException(
					"Indexed properties are not supported: '{0}'.".FormatWith(info.Name));

				return;
			}
			if (info.MemberType == MemberTypes.Field)
			{
				return;
			}
			throw new ArgumentException(
				"Element '{0}' is not a property or a field.".FormatWith(info.Name));
		}

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="info">The member's info instance this one refers to.</param>
		public ElementInfo(MemberInfo info)
		{
			ValidateMemberType(info);
			_MemberInfo = info;
		}

		/// <summary>
		/// Initializes a new multipart instance.
		/// </summary>
		/// <remarks>This is an internal constructor and no checks are made about if the element
		/// belongs to the parent instance or not.</remarks>
		internal ElementInfo(ElementInfo parent, MemberInfo info)
			: this(info)
		{
			if (parent == null) throw new ArgumentNullException("parent", "Parent Info cannot be null.");
			if (parent.IsDisposed) throw new ObjectDisposedException(parent.ToString());
			_Parent = parent;
		}

		/// <summary>
		/// Whether this instance has been disposed or not.
		/// </summary>
		public bool IsDisposed
		{
			get { return _IsDisposed; }
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		public void Dispose()
		{
			Dispose(disposeParent: DEFAULT_DISPOSE_PARENT);
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		/// <param name="disposeParent">True to also dispose the parent instance, if any.</param>
		public void Dispose(bool disposeParent)
		{
			if (!IsDisposed) { OnDispose(true, disposeParent); GC.SuppressFinalize(this); }
		}

		~ElementInfo()
		{
			if (!IsDisposed) OnDispose(false, disposeParent: DEFAULT_DISPOSE_PARENT);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		/// <param name="disposeParent">True to also dispose the parent instance, if any.</param>
		protected virtual void OnDispose(bool disposing, bool disposeParent)
		{
			if (disposing)
			{
				if (disposeParent && _Parent != null && !_Parent.IsDisposed) _Parent.Dispose(true);
			}
			_MemberInfo = null; // We want not to lock assemblies capturing their types
			_Parent = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			string str = FullName ?? string.Empty;
			return IsDisposed ? string.Format("disposed::{0}({1})", GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance that is a copy of the original one.</returns>
		public ElementInfo Clone()
		{
			var cloned = new ElementInfo();
			OnClone(cloned); return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Invoked when cloning this object to set its state at this point of the inheritance
		/// chain.
		/// </summary>
		/// <param name="cloned">The cloned object.</param>
		protected virtual void OnClone(object cloned)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			var temp = cloned as ElementInfo;
			if (temp == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(ElementInfo).EasyName()));

			temp._Parent = _Parent == null ? null : _Parent.Clone();
			temp._MemberInfo = _MemberInfo;
		}

		/// <summary>
		/// The name of the element this instance refers to.
		/// </summary>
		public string Name
		{
			get { return _MemberInfo == null ? null : _MemberInfo.Name; }
		}

		/// <summary>
		/// The full name of the element this instance refers to, prepending its proper name with
		/// its parent full name, if any.
		/// </summary>
		public string FullName
		{
			get { return _Parent == null ? Name : string.Format("{0}.{1}", _Parent.FullName, Name); }
		}

		/// <summary>
		/// Whether this instance refers to a multipart specification or not.
		/// </summary>
		public bool IsMultipart
		{
			get { return _Parent != null; }
		}

		/// <summary>
		/// The parent of this instance, or null if it is a not-multipart one.
		/// </summary>
		public ElementInfo Parent
		{
			get { return _Parent; }
		}

		/// <summary>
		/// The underlying MemberInfo instance this object refers to, or null if this instance is
		/// disposed.
		/// </summary>
		public MemberInfo MemberInfo
		{
			get { return _MemberInfo; }
		}

		/// <summary>
		/// The PropertyInfo this instance refers to, or null if this instance is disposed, or if
		/// it does not refer to a property.
		/// </summary>
		public PropertyInfo PropertyInfo
		{
			get
			{
				return (_MemberInfo == null || _MemberInfo.MemberType != MemberTypes.Property)
					? null
					: (PropertyInfo)_MemberInfo;
			}
		}

		/// <summary>
		/// The FieldInfo this instance refers to, or null if this instance is disposed, or if
		/// it does not refer to a field.
		/// </summary>
		public FieldInfo FieldInfo
		{
			get
			{
				return (_MemberInfo == null || _MemberInfo.MemberType != MemberTypes.Field)
					? null
					: (FieldInfo)_MemberInfo;
			}
		}

		/// <summary>
		/// The type of the element this instance refers to, or null if it is disposed.
		/// </summary>
		public Type ElementType
		{
			get
			{
				var property = PropertyInfo; if (property != null) return property.PropertyType;
				var field = FieldInfo; if (field != null) return field.FieldType;
				return null;
			}
		}

		/// <summary>
		/// The type this element is declared from, or null if this instance is disposed.
		/// </summary>
		public Type DeclaringType
		{
			get { return MemberInfo == null ? null : MemberInfo.DeclaringType; }
		}

		/// <summary>
		/// Whether this instance refers to a property.
		/// </summary>
		public bool IsProperty
		{
			get { return PropertyInfo != null; }
		}

		/// <summary>
		/// Whether this instance refers to a field.
		/// </summary>
		public bool IsField
		{
			get { return FieldInfo != null; }
		}

		/// <summary>
		/// Whether the contents of this element can be read.
		/// </summary>
		public bool CanRead
		{
			get
			{
				var property = PropertyInfo; if (property != null) return property.CanRead;
				var field = FieldInfo; if (field != null) return true;
				return false;
			}
		}

		/// <summary>
		/// Whether the contents of this element can be written.
		/// </summary>
		public bool CanWrite
		{
			get
			{
				var property = PropertyInfo; if (property != null) return property.CanWrite;
				var field = FieldInfo; if (field != null) return true;
				return false;
			}
		}

		/// <summary>
		/// Gets the value of this element refering to the given host.
		/// </summary>
		/// <param name="host">The host instance this element depend from. If this one refers to
		/// a multipart element, the host instance is interpreted as the root-most host in the
		/// declaring chain.</param>
		/// <returns>The value this instance contains.</returns>
		public object GetValue(object host)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (host == null) throw new ArgumentNullException("host",
				"Host instance of this element '{0}' cannot be null.".FormatWith(this));

			if (!CanRead) throw new InvalidOperationException(
				"This element '{0}' cannot be read.".FormatWith(this));

			// If this is a multipart instance we assume that 'host' refers to the top-most object,
			// so we need to obtain the closes parent for this instance...
			if (Parent != null)
			{
				host = Parent.GetValue(host); // recursive..

				if (host == null && Parent.ElementType.IsClass) throw new EmptyException(
					"Value of parent element '{0}' is null for this element '{1}'."
					.FormatWith(Parent, this));
			}

			if (IsProperty) return PropertyInfo.GetValue(host);
			else if (IsField) return FieldInfo.GetValue(host);
			else throw new InvalidOperationException(
				"Element '{0}' is not a property or a field.".FormatWith(this));
		}

		/// <summary>
		/// Sets the value of this element refering to the given host.
		/// </summary>
		/// <param name="host">The host instance this element depend from. If this one refers to
		/// a multipart element, the host instance is interpreted as the root-most host in the
		/// declaring chain.</param>
		/// <param name="value">The value to set into this element.</param>
		public void SetValue(object host, object value)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (host == null) throw new ArgumentNullException("host",
				"Host instance of this element '{0}' cannot be null.".FormatWith(this));

			if (!CanWrite) throw new InvalidOperationException(
				"This element '{0}' cannot be writen.".FormatWith(this));

			// If this is a multipart instance we assume that 'host' refers to the top-most object,
			// so we need to obtain the closes parent for this instance...
			if (Parent != null)
			{
				host = Parent.GetValue(host); // recursive..

				if (host == null && Parent.ElementType.IsClass) throw new EmptyException(
					"Value of parent element '{0}' is null for this element '{1}'."
					.FormatWith(Parent, this));
			}

			if (IsProperty) PropertyInfo.SetValue(host, value);
			else if (IsField) FieldInfo.SetValue(host, value);
			else throw new InvalidOperationException(
				"Element '{0}' is not a property or a field.".FormatWith(this));
		}
	}

	// ====================================================
	public partial class ElementInfo
	{
		/// <summary>
		/// Gets a list containing the elements (properties and fields) found on the given type.
		/// If the flags contains the 'FlattenHierarchy' one then the interfaces it may implement
		/// are also taken into consideration.
		/// </summary>
		/// <param name="type">The type to obtain its elements from.</param>
		/// <param name="flags">The flags to use to find the elements.</param>
		/// <returns>A list with the elements found.</returns>
		public static List<ElementInfo> GetElements(Type type, BindingFlags flags)
		{
			if (type == null) throw new ArgumentNullException("type", "Type cannot be null.");
			List<ElementInfo> list = new List<ElementInfo>();

			var props = type.GetProperties(flags); foreach (var prop in props)
			{
				if (list.Find(x => x.Name == prop.Name) == null) list.Add(new ElementInfo(prop));
			}

			var fields = type.GetFields(flags); foreach (var field in fields)
			{
				// Avoiding backing fields created for automatic properties...
				int n = field.CustomAttributes.Where(x => x.AttributeType == typeof(CompilerGeneratedAttribute)).Count();
				if (n != 0) continue;

				if (list.Find(x => x.Name == field.Name) == null) list.Add(new ElementInfo(field));
			}

			if (flags.HasFlag(BindingFlags.FlattenHierarchy))
			{
				var ifaces = type.GetInterfaces(); foreach (var iface in ifaces)
				{
					var temp = GetElements(iface, flags);
					foreach (var item in temp)
						if (list.Find(x => x.Name == item.Name) == null) list.Add(item);

					temp.Clear();
				}
			}

			return list;
		}

		/// <summary>
		/// Returns the name of the element the given expression resolves to. Multipart names
		/// are allowed.
		/// </summary>
		/// <typeparam name="T">The type where to find the element.</typeparam>
		/// <param name="element">The expression that resolves into the element.</param>
		/// <returns>A string containing the name of the requested element.</returns>
		public static string ParseName<T>(Expression<Func<T, object>> element)
		{
			if (element == null) throw new ArgumentException("element", "Element expression cannot be null.");

			var name = element.ToString();
			var body = element.Body;

			var tag = element.Parameters[0].ToString();
			var pre = string.Format("{0} => {0}", tag);
			if (name == pre) throw new ArgumentException(
				"Expressions that resolve into themselves are not allowed: '{0}'.".FormatWith(name));

			if (body is UnaryExpression) body = ((UnaryExpression)body).Operand;
			if (body is MemberExpression)
			{
				name = body.ToString();
				name = name.Substring(tag.Length + 1);
				return name;
			}

			throw new ArgumentException(
				"Expression '{0}' does not resolve into a valid member name.".FormatWith(name));
		}

		/// <summary>
		/// Creates a new ElementInfo instance that refers to the element whose name is given.
		/// </summary>
		/// <param name="type">The type where the element is declared or, if this is a multipart
		/// one, the root-most one in the declaring chain.</param>
		/// <param name="name">The potentially multipart name of the element.</param>
		/// <param name="raise">If true an exception is thrown if the element, or any of its parts,
		/// is not found. If false null is returned.</param>
		/// <param name="flags">The binding flags to use to find the element.</param>
		/// <returns>A new ElementInfo instance, or null.</returns>
		public static ElementInfo Create(Type type, string name, bool raise = true, BindingFlags flags = TypeEx.InstancePublicAndHidden)
		{
			if (type == null) throw new ArgumentNullException("type", "Declaring type cannot be null.");
			name = name.Validated("Element name");

			ElementInfo parent = null; int index = name.LastIndexOf('.'); if (index >= 0)
			{
				string pname = name.Left(index);
				parent = Create(type, pname, raise, flags);

				type = parent.ElementType;
				name = name.Right(name.Length - index - 1);
			}

			MemberInfo info = null; Type other = type; while (other != null)
			{
				info = ((MemberInfo)other.GetProperty(name, flags)) ?? ((MemberInfo)other.GetField(name, flags));
				if (info != null)
				{
					ElementInfo obj = parent == null ? new ElementInfo(info) : new ElementInfo(parent, info);
					return obj;
				}
				other = other.BaseType;
			}

			if (parent != null) parent.Dispose();
			if (!raise) return null;
			throw new NotFoundException("Element '{0}' not found in '{1}'.".FormatWith(name, type.EasyName()));
		}

		/// <summary>
		/// Creates a new ElementInfo instance that refers to the element whose name is given.
		/// </summary>
		/// <typeparam name="T">The type where the element is declared. If it is a multipart one,
		/// this type must be the root-most one in the declaring chain.</typeparam>
		/// <param name="name">The potentially multipart name of the element.</param>
		/// <param name="raise">If true an exception is thrown if the element, or any of its parts,
		/// is not found. If false null is returned.</param>
		/// <param name="flags">The binding flags to use to find the element.</param>
		/// <returns>A new ElementInfo instance, or null.</returns>
		public static ElementInfo Create<T>(string name, bool raise = true, BindingFlags flags = TypeEx.InstancePublicAndHidden)
		{
			return Create(typeof(T), name, raise, flags);
		}

		/// <summary>
		/// Creates a new ElementInfo instance that refers to the element whose name is obtained
		/// from parsing the expression given.
		/// </summary>
		/// <typeparam name="T">The type where the element is declared. If it is a multipa
		/// <param name="element">The expression that resolves into the element.</param>
		/// <param name="raise">If true an exception is thrown if the element, or any of its parts,
		/// is not found. If false null is returned.</param>
		/// <param name="flags">The binding flags to use to find the element.</param>
		/// <returns>A new ElementInfo instance, or null.</returns>
		public static ElementInfo Create<T>(Expression<Func<T, object>> element, bool raise = true, BindingFlags flags = TypeEx.InstancePublicAndHidden)
		{
			var name = ParseName<T>(element);
			return Create<T>(name, raise, flags);
		}
	}
}
