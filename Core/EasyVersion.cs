using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Kerosene.Tools
{
	// ====================================================
	/// <summary>
	/// Represents a version specification composed by an arbitrary number of numeric,
	/// alphanumeric or hybrid parts.
	/// </summary>
	[Serializable]
	public class EasyVersion : ISerializable, ICloneable, IComparable<EasyVersion>, IEquivalent<EasyVersion>
	{
		/// <summary>
		/// The default empty instance.
		/// </summary>
		public static EasyVersion Empty
		{
			get { return _Empty; }
		}
		static EasyVersion _Empty = new EasyVersion((string)null);

		List<VersionPart> _List = new List<VersionPart>();

		private EasyVersion() { }

		/// <summary>
		/// Initializes a new instance using the given string as the initial specification.
		/// <para>If the specification is null or empty, or it is composed by empty parts only,
		/// the new instance is equivalent to the standard 'Empty' one.</para>
		/// </summary>
		/// <param name="version"></param>
		public EasyVersion(string version)
		{
			OnInitialize(version);
		}

		/// <summary>
		/// Used both for the constructor and for the Clone() method
		/// </summary>
		void OnInitialize(string version)
		{
			version = version.NullIfTrimmedIsEmpty(); if (version == null) return;
			version = version.Replace(" ", string.Empty);

			var parts = version.Split('.');
			foreach (var part in parts) _List.Add(new VersionPart(part));

			var list = new List<VersionPart>(_List);
			for (int i = list.Count - 1; i >= 0; i--)
			{
				var obj = list[i];
				if (obj.Payload == null) _List.Remove(obj);
				else break;
			}
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the representation of this instance.</returns>
		public override string ToString()
		{
			return Payload ?? string.Empty;
		}

		/// <summary>
		/// Call-back method required for custom serialization.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddExtended("List", _List);
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected EasyVersion(SerializationInfo info, StreamingContext context)
		{
			_List = info.GetExtended<List<VersionPart>>("List");
		}

		/// <summary>
		/// Returns a new instance that is a copy of this one.
		/// </summary>
		/// <returns>A new instance that is a copy of this one.</returns>
		public EasyVersion Clone()
		{
			var cloned = new EasyVersion(); OnClone(cloned);
			return cloned;
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
			var temp = cloned as EasyVersion;
			if (temp == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(EasyVersion).EasyName()));

			OnInitialize(Payload);
		}

		/// <summary>
		/// Determines whether this object can be considered as the same one as the given one.
		/// </summary>
		public override bool Equals(object obj)
		{
			var temp = obj as EasyVersion;
			return temp == null ? false : (Compare(this, temp) == 0);
		}

		/// <summary>
		/// Compares the two given instances returning -1 if the left one can be considered
		/// smaller than the right one, +1 if it can be consider as bigger, or 0 if both can
		/// be considered equivalent.
		/// </summary>
		/// <param name="left">The left instance to compare.</param>
		/// <param name="right">The right instance to compare.</param>
		/// <returns>An interger expressing the relative order of the left instance when
		/// compared against the right one.</returns>
		public static int Compare(EasyVersion left, EasyVersion right)
		{
			if (object.ReferenceEquals(left, null)) return object.ReferenceEquals(right, null) ? 0 : -1;
			if (object.ReferenceEquals(right, null)) return object.ReferenceEquals(left, null) ? 0 : +1;

			var lcount = left.Count;
			var rcount = right.Count;

			for (int i = 0; i < lcount && i < rcount; i++)
			{
				var r = left._List[i].CompareTo(right._List[i]);
				if (r != 0) return r;
			}
			return lcount == rcount ? 0 : (lcount > rcount ? +1 : -1);
		}

		/// <summary>
		/// Serves as the hash function for this type.
		/// </summary>
		public override int GetHashCode()
		{
			var payload = Payload;
			return payload == null ? base.GetHashCode() : payload.GetHashCode();
		}

		public static bool operator >(EasyVersion left, EasyVersion right)
		{
			return Compare(left, right) > 0;
		}
		public static bool operator <(EasyVersion left, EasyVersion right)
		{
			return Compare(left, right) < 0;
		}
		public static bool operator >=(EasyVersion left, EasyVersion right)
		{
			return Compare(left, right) >= 0;
		}
		public static bool operator <=(EasyVersion left, EasyVersion right)
		{
			return Compare(left, right) <= 0;
		}
		public static bool operator ==(EasyVersion left, EasyVersion right)
		{
			return Compare(left, right) == 0;
		}
		public static bool operator !=(EasyVersion left, EasyVersion right)
		{
			return Compare(left, right) != 0;
		}

		/// <summary>
		/// Compares this instance against the given one returning -1 if this instance can be
		/// considered smaller than the given one, +1 if it can be consider as bigger, or 0 if
		/// both can be considered equivalent.
		/// </summary>
		/// <param name="target">The other instance to compare this one against to.</param>
		/// <returns>An interger expressing the relative order of this instance when compared
		/// against the target one.</returns>
		public int CompareTo(EasyVersion target)
		{
			return Compare(this, target);
		}

		/// <summary>
		/// Returns true if the state of this instance can be considered as equivalent to the
		/// target object given, or false otherwise.
		/// </summary>
		/// <param name="target">The target object to test for equivalence against.</param>
		/// <returns>True if the state of this instance can be considered as equivalent to the
		/// target object given, or false otherwise</returns>
		public bool EquivalentTo(EasyVersion target)
		{
			return Compare(this, target) == 0;
		}

		/// <summary>
		/// The number of portions contained in this part specification.
		/// </summary>
		internal int Count
		{
			get { return _List.Count; }
		}

		/// <summary>
		/// The payload this instance is carrying, or null if there is no contents.
		/// </summary>
		public string Payload
		{
			get
			{
				if (_List.Count == 0) return null;

				StringBuilder sb = new StringBuilder();
				bool empty = true;

				bool first = true; foreach (var obj in _List)
				{
					if (first) first = false; else sb.Append(".");
					var payload = obj.Payload;
					if (payload != null) { sb.Append(payload); empty = false; }
				}
				return empty ? null : sb.ToString();
			}
		}
	}

	// ====================================================
	/// <summary>
	/// Represents a part whithin a version specification.
	/// </summary>
	[Serializable]
	class VersionPart : ISerializable, IComparable<VersionPart>, IEquivalent<VersionPart>
	{
		List<PartItem> _List = new List<PartItem>();

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal VersionPart(string part)
		{
			part = part.NullIfTrimmedIsEmpty(); if (part == null) return;

			StringBuilder sb = new StringBuilder();
			bool numeric = char.IsDigit(part[0]);

			foreach (char c in part)
			{
				bool temp = char.IsDigit(c); if (temp != numeric)
				{
					_List.Add(new PartItem(numeric, sb.ToString()));
					sb.Clear();
					numeric = temp;
				}
				sb.Append(c);
			}
			if (sb.Length != 0) _List.Add(new PartItem(numeric, sb.ToString()));
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the representation of this instance.</returns>
		public override string ToString()
		{
			return Payload ?? string.Empty;
		}

		/// <summary>
		/// Call-back method required for custom serialization.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddExtended("List", _List);
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected VersionPart(SerializationInfo info, StreamingContext context)
		{
			_List = info.GetExtended<List<PartItem>>("List");
		}

		/// <summary>
		/// Compares the two given instances returning -1 if the left one can be considered
		/// smaller than the right one, +1 if it can be consider as bigger, or 0 if both can
		/// be considered equivalent.
		/// </summary>
		/// <param name="left">The left instance to compare.</param>
		/// <param name="right">The right instance to compare.</param>
		/// <returns>An interger expressing the relative order of the left instance when
		/// compared against the right one.</returns>
		public static int Compare(VersionPart left, VersionPart right)
		{
			if (object.ReferenceEquals(left, null)) return object.ReferenceEquals(right, null) ? 0 : -1;
			if (object.ReferenceEquals(right, null)) return object.ReferenceEquals(left, null) ? 0 : +1;

			var lcount = left.Count;
			var rcount = right.Count;

			for (int i = 0; i < lcount && i < rcount; i++)
			{
				var r = left._List[i].CompareTo(right._List[i]);
				if (r != 0) return r;
			}
			return lcount == rcount ? 0 : (lcount > rcount ? +1 : -1);
		}

		/// <summary>
		/// Compares this instance against the given one returning -1 if this instance can be
		/// considered smaller than the given one, +1 if it can be consider as bigger, or 0 if
		/// both can be considered equivalent.
		/// </summary>
		/// <param name="target">The other instance to compare this one against to.</param>
		/// <returns>An interger expressing the relative order of this instance when compared
		/// against the target one.</returns>
		public int CompareTo(VersionPart target)
		{
			return Compare(this, target);
		}

		/// <summary>
		/// Returns true if the state of this instance can be considered as equivalent to the
		/// target object given, or false otherwise.
		/// </summary>
		/// <param name="target">The target object to test for equivalence against.</param>
		/// <returns>True if the state of this instance can be considered as equivalent to the
		/// target object given, or false otherwise</returns>
		public bool EquivalentTo(VersionPart target)
		{
			return CompareTo(target) == 0;
		}

		/// <summary>
		/// The number of portions contained in this part specification.
		/// </summary>
		internal int Count
		{
			get { return _List.Count; }
		}

		/// <summary>
		/// The payload this instance is carrying, or null if there is no contents.
		/// </summary>
		public string Payload
		{
			get
			{
				if (_List.Count == 0) return null;

				StringBuilder sb = new StringBuilder();
				foreach (var obj in _List) sb.Append(obj.Payload ?? string.Empty);

				var str = sb.ToString();
				return str.Length == 0 ? null : str;
			}
		}
	}

	// ====================================================
	/// <summary>
	/// Represents a sub-item whithin a bersion part.
	/// </summary>
	[Serializable]
	class PartItem : ISerializable, IComparable<PartItem>, IEquivalent<PartItem>
	{
		bool _IsNumeric = false;
		string _Payload = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal PartItem(bool isnumeric, string payload)
		{
			_IsNumeric = isnumeric;
			_Payload = payload.NullIfTrimmedIsEmpty();

			if (_IsNumeric && _Payload != null)
			{
				uint value = 0;
				bool r = uint.TryParse(_Payload, out value); if (!r)
					throw new ArgumentNullException(
						"Part item '{0}' is not an unsigned int.".FormatWith(_Payload));

				_Payload = value.ToString();
			}
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the representation of this instance.</returns>
		public override string ToString()
		{
			return Payload ?? string.Empty;
		}

		/// <summary>
		/// Call-back method required for custom serialization.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Payload", _Payload);
			info.AddValue("IsNumeric", _IsNumeric);
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected PartItem(SerializationInfo info, StreamingContext context)
		{
			_Payload = info.GetString("Payload");
			_IsNumeric = info.GetBoolean("IsNumeric");
		}

		/// <summary>
		/// Compares the two given instances returning -1 if the left one can be considered
		/// smaller than the right one, +1 if it can be consider as bigger, or 0 if both can
		/// be considered equivalent.
		/// </summary>
		/// <param name="left">The left instance to compare.</param>
		/// <param name="right">The right instance to compare.</param>
		/// <returns>An interger expressing the relative order of the left instance when
		/// compared against the right one.</returns>
		public static int Compare(PartItem left, PartItem right)
		{
			if (object.ReferenceEquals(left, null)) return object.ReferenceEquals(right, null) ? 0 : -1;
			if (object.ReferenceEquals(right, null)) return object.ReferenceEquals(left, null) ? 0 : +1;

			if (left.IsNumeric && right.IsNumeric)
				return
					(left.Value == right.Value) ? 0 :
					(left.Value < right.Value ? -1 : +1);

			return string.Compare(left.Payload, right.Payload, ignoreCase: true);
		}

		/// <summary>
		/// Compares this instance against the given one returning -1 if this instance can be
		/// considered smaller than the given one, +1 if it can be consider as bigger, or 0 if
		/// both can be considered equivalent.
		/// </summary>
		/// <param name="target">The other instance to compare this one against to.</param>
		/// <returns>An interger expressing the relative order of this instance when compared
		/// against the target one.</returns>
		public int CompareTo(PartItem target)
		{
			return Compare(this, target);
		}

		/// <summary>
		/// Returns true if the state of this instance can be considered as equivalent to the
		/// target object given, or false otherwise.
		/// </summary>
		/// <param name="target">The target object to test for equivalence against.</param>
		/// <returns>True if the state of this instance can be considered as equivalent to the
		/// target object given, or false otherwise</returns>
		public bool EquivalentTo(PartItem target)
		{
			return CompareTo(target) == 0;
		}

		/// <summary>
		/// The payload this instance is carrying, or null if there is no contents.
		/// </summary>
		public string Payload
		{
			get { return _Payload; }
		}

		/// <summary>
		/// Whether the payload shall be interpreted as a numeric one or not.
		/// </summary>
		public bool IsNumeric
		{
			get { return _IsNumeric; }
		}

		/// <summary>
		/// The value of the payload as an uint.
		/// </summary>
		internal uint Value
		{
			get { return _Payload == null ? 0 : uint.Parse(_Payload); }
		}
	}
}
