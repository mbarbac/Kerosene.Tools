using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;

namespace Kerosene.Tools
{
	// ====================================================
	/// <summary>
	/// Helpers and extensions for working with serialization scenarios.
	/// </summary>
	public static class SerializationEx
	{
		/// <summary>
		/// Serializes the given object into this stream.
		/// </summary>
		/// <param name="sm">The stream.</param>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="formatter">The formatter to use.</param>
		public static void Serialize(this Stream sm, object obj, IFormatter formatter)
		{
			if (sm == null) throw new NullReferenceException("Stream cannot be null.");
			if (obj == null) throw new ArgumentNullException("obj", "Object cannot be null.");
			if (formatter == null) throw new ArgumentNullException("formatter", "IFormatter cannot be null.");

			formatter.Serialize(sm, obj);
		}

		/// <summary>
		/// Returns the object deserialized from this stream.
		/// </summary>
		/// <param name="sm">The stream.</param>
		/// <param name="formatter">The formatter to use.</param>
		public static object Deserialize(this Stream sm, IFormatter formatter)
		{
			if (sm == null) throw new NullReferenceException("Stream cannot be null.");
			if (formatter == null) throw new ArgumentNullException("formatter", "IFormatter cannot be null.");

			object obj = formatter.Deserialize(sm);
			return obj;
		}

		/// <summary>
		/// Serializes the given object into this stream, using a binary or text formatter.
		/// </summary>
		/// <param name="sm">The stream.</param>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="binary">True to use a binary formatter, False to use a Soap one.</param>
		public static void Serialize(this Stream sm, object obj, bool binary)
		{
			if (sm == null) throw new NullReferenceException("Stream cannot be null.");
			if (obj == null) throw new ArgumentNullException("obj", "Object cannot be null.");

			var formatter = binary ? (IFormatter)(new BinaryFormatter()) : (IFormatter)(new SoapFormatter());
			Serialize(sm, obj, formatter);
		}

		/// <summary>
		/// Returns the object deserialized from this stream, using a binary or text formatter.
		/// </summary>
		/// <param name="sm">The stream.</param>
		/// <param name="binary">True to use a binary formatter, False to use a Soap one.</param>
		public static object Deserialize(this Stream sm, bool binary)
		{
			if (sm == null) throw new NullReferenceException("Stream cannot be null.");

			var formatter = binary ? (IFormatter)(new BinaryFormatter()) : (IFormatter)(new SoapFormatter());
			return Deserialize(sm, formatter);
		}

		/// <summary>
		/// Serializes the given object into the file specified, using a binary or text formatter.
		/// </summary>
		/// <param name="path">The path of the file the object will be persisted into.</param>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="binary">True to use a binary formatter, False to use a Soap one.</param>
		public static void PathSerialize(this string path, object obj, bool binary)
		{
			path = path.Validated("Path");

			if (obj == null) throw new ArgumentNullException("obj", "Object cannot be null.");

			using (FileStream sm = new FileStream(path, FileMode.Create))
			{
				SerializationEx.Serialize(sm, obj, binary);
			}
		}

		/// <summary>
		/// Returns the object deserialized from the file specified, using a binary or text formatter.
		/// </summary>
		/// <param name="path">The path of the file the object is to be deserialized from.</param>
		/// <param name="binary">True to use a binary formatter, False to use a Soap one.</param>
		public static object PathDeserialize(this string path, bool binary)
		{
			path = path.Validated("Path");

			using (FileStream sm = new FileStream(path, FileMode.Open))
			{
				return SerializationEx.Deserialize(sm, binary);
			}
		}

		/// <summary>
		/// Adds into the serialization info the given entry, including its name, type and value.
		/// </summary>
		/// <param name="info">The serialization info.</param>
		/// <param name="name">The name to identify this entry.</param>
		/// <param name="value">The value of this entry.</param>
		public static void AddExtended(this SerializationInfo info, string name, object value)
		{
			var holder = new SerializationHolder(value);
			info.AddValue(name, holder, typeof(SerializationHolder));
		}

		/// <summary>
		/// Gets from the serialization info the given entry added using the extended method.
		/// </summary>
		/// <param name="info">The serialization info.</param>
		/// <param name="name">The name of the entry.</param>
		/// <returns>The de-serialized value.</returns>
		public static object GetExtended(this SerializationInfo info, string name)
		{
			var holder = (SerializationHolder)info.GetValue(name, typeof(SerializationHolder));
			var value = holder.HolderValue;
			return value;
		}

		/// <summary>
		/// Gets from the serialization info the given entry added using the extended method.
		/// </summary>
		/// <typeparam name="T">The type the de-serialized object will be casted to.</typeparam>
		/// <param name="info">The serialization info.</param>
		/// <param name="name">The name of the entry.</param>
		/// <returns>The de-serialized value.</returns>
		public static T GetExtended<T>(this SerializationInfo info, string name)
		{
			return (T)info.GetExtended(name);
		}
	}

	// ====================================================
	/// <summary>
	/// Used for easy serialization of complex types.
	/// </summary>
	[Serializable]
	class SerializationHolder : ISerializable
	{
		Type _Type = null;
		Object _Value = null;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		/// <param name="value">The object this instance refers to, or null.</param>
		internal SerializationHolder(object value)
		{
			_Value = value;
			_Type = value == null ? null : value.GetType();
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			return string.Format("{0}({1})",
				_Type == null ? string.Empty : _Type.EasyName(),
				_Value.Sketch());
		}

		/// <summary>
		/// The object this instance refers to, or null.
		/// </summary>
		internal Object HolderValue
		{
			get { return _Value; }
		}

		/// <summary>
		/// The type of the object this instance refers to, or null.
		/// </summary>
		internal Type HolderType
		{
			get { return _Type; }
		}

		/// <summary>
		/// Call-back method required for custom serialization.
		/// </summary>
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("HolderType", _Type == null ? null : _Type.AssemblyQualifiedName);
			if (_Type == null) return;

			var dict = _Value as IDictionary; if (dict != null)
			{
				int count = 0; foreach (DictionaryEntry entry in dict)
				{
					info.AddExtended("HolderKey" + count, entry.Key);
					info.AddExtended("HolderValue" + count, entry.Value);
					count++;
				}
				info.AddValue("HolderCount", count);
				info.AddValue("HolderManaged", "IDictionary");
				return;
			}

			var list = _Value as IList; if (list != null)
			{
				int count = 0; foreach (var entry in list)
				{
					info.AddExtended("HolderValue" + count, entry);
					count++;
				}
				info.AddValue("HolderCount", count);
				info.AddValue("HolderManaged", _Type.IsArray ? "Array" : "IList");
				return;
			}

			info.AddValue("HolderValue", _Value);
			info.AddValue("HolderManaged", ((string)null));
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected SerializationHolder(SerializationInfo info, StreamingContext context)
		{
			var name = info.GetString("HolderType");
			if (name == null) return;

			_Type = Type.GetType(name, throwOnError: true);

			var managed = info.GetString("HolderManaged");

			if (managed == "IDictionary")
			{
				_Value = Activator.CreateInstance(_Type);
				var item = (IDictionary)_Value;

				int count = (int)info.GetValue("HolderCount", typeof(int));
				for (int i = 0; i < count; i++)
				{
					var key = info.GetExtended("HolderKey" + i);
					var value = info.GetExtended("HolderValue" + i);
					item.Add(key, value);
				}
				return;
			}

			if (managed == "Array")
			{
				var count = (int)info.GetValue("HolderCount", typeof(int));
				_Value = Activator.CreateInstance(_Type, count);
				var item = (Array)_Value;

				for (int i = 0; i < count; i++)
				{
					var value = info.GetExtended("HolderValue" + i);
					item.SetValue(value, i);
				}
				return;
			}

			if (managed == "IList")
			{
				_Value = Activator.CreateInstance(_Type);
				var item = (IList)_Value;

				int count = (int)info.GetValue("HolderCount", typeof(int));
				for (int i = 0; i < count; i++)
				{
					var value = info.GetExtended("HolderValue" + i);
					item.Add(value);
				}
				return;
			}

			_Value = info.GetValue("HolderValue", _Type);
		}
	}
}
