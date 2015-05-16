using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Kerosene.Tools
{
	// ====================================================
	/// <summary>
	/// Represents a multi-level dynamic object whose members can also be dynamic ones, to any
	/// arbitrary depth.
	/// </summary>
	[Serializable]
	public class DeepObject
		: DynamicObject, IDisposableEx, ISerializable, ICloneable, IEquivalent<DeepObject>
	{
		/// <summary>
		/// Whether, by default, the names of the dynamic properties of DeepObject instances are
		/// considered as case sensitive or not.
		/// </summary>
		public const bool DEFAULT_CASESENSITIVE_NAMES = true;

		bool _IsDisposed = false;
		string _Name = null;
		bool _Indexed = false;
		bool _CaseSensitiveNames = DEFAULT_CASESENSITIVE_NAMES;
		DeepObject _Parent = null;
		List<DeepObject> _Members = new List<DeepObject>();
		object _Value = null;
		bool _HasValue = false;

		/// <summary>
		/// Generates a normalized member name based upon the given indexes.
		/// </summary>
		static string NameFromIndexes(object[] indexes)
		{
			return (indexes == null || indexes.Length == 0) ? "[]" : indexes.Sketch();
		}

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="caseSensitiveNames">Whether the names of the members of this instance
		/// are case sensitive (the default) or not (to permit non-convencional scenarios). </param>
		public DeepObject(bool caseSensitiveNames = DEFAULT_CASESENSITIVE_NAMES)
		{
			_CaseSensitiveNames = caseSensitiveNames;
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
			if (!IsDisposed) { OnDispose(true); GC.SuppressFinalize(this); }
		}

		~DeepObject()
		{
			if (!IsDisposed) OnDispose(false);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		protected virtual void OnDispose(bool disposing)
		{
			if (disposing)
			{
				if (_Parent != null && _Parent._Members != null) _Parent._Members.Remove(this);
				if (_Members != null)
				{
					var list = _Members.ToArray(); foreach (var member in list) member.Dispose();
				}
			}

			if (_Members != null) _Members.Clear(); _Members = null;
			_Parent = null;
			_Value = null; _HasValue = false;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("{");
			sb.Append(_Name); if (_Name == null && _Parent != null) sb.Append(".");

			if (_HasValue)
			{
				if (_Name != null) sb.Append("=");
				sb.AppendFormat("'{0}'", _Value.Sketch());
			}

			int count = DeepCount(); if (count != 0)
			{
				if (_Name != null || _HasValue) sb.Append(" ");

				sb.Append("["); for (int i = 0; i < count; i++)
				{
					if (i != 0) sb.Append(", ");
					sb.Append(_Members[i]);
				}
				sb.Append("]");
			}
			sb.Append("}");

			var str = sb.ToString();
			return IsDisposed ? string.Format("disposed::{0}({1})", GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// Call-back method required for custom serialization.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			info.AddValue("Name", _Name);
			info.AddValue("Indexed", _Indexed);
			info.AddValue("CaseSensitiveMembers", _CaseSensitiveNames);

			info.AddValue("HasValue", _HasValue);
			if (_HasValue) info.AddExtended("Value", _Value);

			info.AddExtended("Members", _Members);
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected DeepObject(SerializationInfo info, StreamingContext context)
		{
			_Name = info.GetString("Name");
			_Indexed = info.GetBoolean("Indexed");
			_CaseSensitiveNames = info.GetBoolean("CaseSensitiveMembers");

			_HasValue = info.GetBoolean("HasValue");
			if (_HasValue) _Value = info.GetExtended("Value");

			_Members = info.GetExtended<List<DeepObject>>("Members");
			foreach (var member in _Members) member._Parent = this;
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance that is a copy of the original one.</returns>
		public DeepObject Clone()
		{
			var cloned = new DeepObject();
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
			var temp = cloned as DeepObject;
			if (temp == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(DeepObject).EasyName()));

			temp._Name = _Name;
			temp._Indexed = _Indexed;

			temp._HasValue = _HasValue;
			if (_HasValue) temp._Value = _Value.TryClone();

			temp._CaseSensitiveNames = _CaseSensitiveNames;

			foreach (var member in _Members)
			{
				var item = member.Clone();
				item._Parent = temp; temp._Members.Add(item);
			}
		}

		/// <summary>
		/// Returns true if the state of this object can be considered as equivalent to the target
		/// one, based upon any arbitrary criteria implemented in this method.
		/// </summary>
		/// <param name="target">The target instance this one will be tested for equivalence against.</param>
		/// <returns>True if the state of this instance can be considered as equivalent to the
		/// target one, or false otherwise.</returns>
		public bool EquivalentTo(DeepObject target)
		{
			return OnEquivalentTo(target);
		}

		/// <summary>
		/// Invoked to test equivalence at this point of the inheritance chain.
		/// </summary>
		/// <param name="target">The target this instance will be tested for equivalence against.</param>
		/// <returns>True if at this level on the inheritance chain this instance can be considered
		/// equivalent to the target instance given.</returns>
		protected virtual bool OnEquivalentTo(object target)
		{
			if (object.ReferenceEquals(this, target)) return true;
			var temp = target as DeepObject; if (temp == null) return false;
			if (temp.IsDisposed) return false;
			if (IsDisposed) return false;

			if (_Indexed != temp._Indexed) return false;
			if (_CaseSensitiveNames != temp._CaseSensitiveNames) return false;
			if (string.Compare(_Name, temp._Name, !_CaseSensitiveNames) != 0) return false;

			if (_HasValue != temp._HasValue) return false;
			if (_HasValue && !_Value.IsEquivalentTo(temp._Value)) return false;

			if (DeepCount() != temp.DeepCount()) return false; if (DeepCount() != 0)
			{
				foreach (var member in _Members)
				{
					var item = temp.DeepFind(member._Name);
					if (item == null) return false;
					if (!member.EquivalentTo(item)) return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Gets the own name of this instance, or null.
		/// </summary>
		public string DeepName()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return _Name;
		}

		/// <summary>
		/// Gets the full name of this instance, consisting in its own name prepended by the
		/// full name of its host instance, if any.
		/// </summary>
		/// <returns></returns>
		public string DeepFullName()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (_Parent == null) return _Name;

			return "{0}{1}{2}".FormatWith(
				_Parent.DeepFullName(),
				_Indexed ? string.Empty : ".",
				_Name);
		}

		/// <summary>
		/// Gets whether this instance represents an indexed member or not.
		/// </summary>
		public bool DeepIndexed()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return _Indexed;
		}

		/// <summary>
		/// Gets whether this instance carries a value or not.
		/// </summary>
		public bool DeepHasValue()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return _HasValue;
		}

		/// <summary>
		/// Gets the value this instance is carrying.
		/// <para>Note that the value returned by this method is meaningless if this instance
		/// has been disposed, or if it does not carry a value.</para>
		/// </summary>
		/// <returns></returns>
		public object DeepValue()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (!_HasValue) throw new InvalidOperationException("This instance '{0}' does not carry a value.".FormatWith(this));
			return _Value;
		}

		/// <summary>
		/// Sets the value this instance will carry.
		/// </summary>
		/// <param name="value">The value this instance will carry.</param>
		public void DeepValue(object value)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			_Value = value;
			_HasValue = true;
		}

		/// <summary>
		/// Resets any value this instance might be carrying.
		/// </summary>
		public void DeepValueReset()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			_Value = null;
			_HasValue = false;
		}

		/// <summary>
		/// Gets the host parent instance of this one.
		/// </summary>
		/// <returns></returns>
		public DeepObject DeepParent()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return _Parent;
		}

		/// <summary>
		/// Gets the level of this instance, defined as 0 if it is a root instance, as 1 if it
		/// is a first-level member, and so on.
		/// </summary>
		/// <returns>The level of this instance.</returns>
		public int DeepLevel()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return _Parent == null ? 0 : (_Parent.DeepLevel() + 1);
		}

		/// <summary>
		/// Gets whether the names of the members of this instance are case sensitive.
		/// </summary>
		/// <returns></returns>
		public bool DeepCaseSensitiveNames()
		{
			return _CaseSensitiveNames;
		}

		/// <summary>
		/// Gets or set the value of the member whose name or indexes are given.
		/// <para>The getter throws an exception if no member with that name exist.</para>
		/// <para>The setter creates a new member with the given name if needed.</para>
		/// </summary>
		/// <param name="args">Either the name of the member or the values of the indexed of
		/// the member.</param>
		/// <returns>The value of the member whose name or indexes given</returns>
		public object this[params object[] args]
		{
			get
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());

				var member = DeepFind(args); if (member == null)
					throw new NotFoundException("Member '{0}' not found."
						.FormatWith(args.Sketch()));

				return member.DeepValue();
			}
			set
			{
				if (IsDisposed) throw new ObjectDisposedException(this.ToString());

				var member = DeepFind(args);
				if (member == null) member = DeepAdd(args);

				member.DeepValue(value);
			}
		}

		/// <summary>
		/// Gets the number of first-level members in this instance, or cero if there are no
		/// members or if this instance has been disposed.
		/// </summary>
		public int DeepCount()
		{
			return _Members == null ? 0 : _Members.Count;
		}

		/// <summary>
		/// Gets the collection of members in this instance.
		/// </summary>
		public IEnumerable<DeepObject> DeepMembers()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			return _Members;
		}

		/// <summary>
		/// Returns the member whose name or indexes are given, or null if not such member can
		/// be found.
		/// </summary>
		/// <param name="args">Either the name of the member or the values of the indexed of
		/// the member.</param>
		/// <returns>The requested member, or null.</returns>
		public DeepObject DeepFind(params object[] args)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (args != null && args.Length == 1 && args[0] is string)
			{
				var name = (args[0] as string).Validated("Name");
				return _Members.Find(x => string.Compare(name, x._Name, !_CaseSensitiveNames) == 0);
			}
			else
			{
				var name = NameFromIndexes(args);
				return _Members.Find(x => string.Compare(name, x._Name, !_CaseSensitiveNames) == 0);
			}
		}

		/// <summary>
		/// Adds into this instance and returns the new member created for either the given name
		/// or the given set of indexes.
		/// </summary>
		/// <param name="args">Either the name of the member or the values of the indexed of
		/// the member.</param>
		/// <returns>The new member added.</returns>
		public DeepObject DeepAdd(params object[] args)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var member = DeepFind(args); if (member != null)
				throw new DuplicateException(
					"Member '{0}' already exists in this '{1}'."
					.FormatWith(args.Sketch(), this));

			var name = (args != null && args.Length == 1 && args[0] is string)
				? (args[0] as string).Validated("Name")
				: NameFromIndexes(args);

			member = new DeepObject() { _Name = name, _CaseSensitiveNames = this._CaseSensitiveNames };
			member._Parent = this; _Members.Add(member);
			return member;
		}

		/// <summary>
		/// Removes the given member from this instance. Returns true if it has been removed
		/// succesfully, or false otherwise.
		/// </summary>
		/// <param name="member">The member to remove.	</param>
		/// <returns></returns>
		public bool DeepRemove(DeepObject member)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			if (member == null) throw new ArgumentNullException("member", "Member cannot be null.");

			bool r = _Members.Remove(member); if (r)
			{
				member._Parent = null;
				member._Name = null;
				member._Indexed = false;
			}
			return r;
		}

		/// <summary>
		/// Clears this instance by removing all its members and optionally disposing them.
		/// </summary>
		/// <param name="disposeMembers">True to dispose the members removed.</param>
		public void DeepClear(bool disposeMembers = true)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			if (disposeMembers)
			{
				var list = _Members.ToArray(); foreach (var member in list) member.Dispose();
			}

			_Members.Clear();
		}

		/// <summary>
		/// Gets the names of the members registered into this instance.
		/// </summary>
		/// <returns>A collection with the names of the members of this instance.</returns>
		public override IEnumerable<string> GetDynamicMemberNames()
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var list = new List<string>(_Members.Select(x => x.DeepName()));
			return list;
		}

		/// <summary>
		/// Gets the value of the requested member, or the member itself if it does not carry a
		/// value. If the member does not exist yet a new one is created.
		/// </summary>
		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var member = DeepFind(binder.Name); if (member == null)
			{
				var method = typeof(DeepObject).GetMethod(binder.Name);
				if (method != null) throw new InvalidOperationException(
					"TryGetMember is receiving method '{0}' as its argument.".FormatWith(method.Name));

				else result = DeepAdd(binder.Name);
			}
			else result = (member._HasValue ? member._Value : member);

			return true;
		}

		/// <summary>
		/// Sets the given value on the requested member. If the member does not exist yet a new one
		/// is created.
		/// </summary>
		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var member = DeepFind(binder.Name); if (member == null)
			{
				var method = typeof(DeepObject).GetMethod(binder.Name);
				if (method != null) throw new InvalidOperationException(
					"TrySetMember is receiving method '{0}' as its argument.".FormatWith(method.Name));

				member = DeepAdd(binder.Name);
			}
			member.DeepValue(value);

			return true;
		}

		/// <summary>
		/// Gets the value of the requested indexed member, or the member itself if it does not
		/// carry a value. If the member does not exist yet a new one is created.
		/// </summary>
		public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var member = DeepFind(indexes); if (member == null)
			{
				result = DeepAdd(indexes);
			}
			else result = member._HasValue ? member._Value : member;

			return true;
		}

		/// <summary>
		/// Sets the given on the requested indexed member. If the member does not exist yet a new
		/// one is created.
		/// </summary>
		public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			var member = DeepFind(indexes); if (member == null)
			{
				member = DeepAdd(indexes);
			}
			member.DeepValue(value);

			return true;
		}

		/// <summary>
		/// Tries to convert the value this member carries into the given type, or the member itself
		/// if it does not carry a value.
		/// </summary>
		public override bool TryConvert(ConvertBinder binder, out object result)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());

			result = (_HasValue ? _Value : this).ConvertTo(binder.ReturnType);
			return true;
		}
	}
}
