// ======================================================== DynamicParser.cs
namespace Kerosene.Tools
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Dynamic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using System.Runtime.Serialization;
	using System.Text;

	// ==================================================== 
	/// <summary>
	/// Represents the ability of parsing an arbitrary dynamic lambda expression (DLE - defined
	/// as a lambda expression where at least one of its arguments is a dynamic one) returning
	/// an instance of this class that holds both the result of that parsing and the arguments
	/// used.
	/// </summary>
	public class DynamicParser : IDisposableEx
	{
		/// <summary>
		/// Parsers the given dynamic (or regular) lambda expression and returns an instance that
		/// holds the result of the parsing along with the arguments used in it.
		/// <para>
		/// - The result of the parsing is held in the 'Result' property, and it can a regular
		///   object, including null references, or a 'DynamicNode' instance if the expression
		///   resolves into the definition of an arbitrary logic bounded to the dynamic arguments
		///   used in it.
		/// - Any not dynamic value or reference found in the expression, along with the result
		///   of any standard method invoked in it, is captured at the moment when the expression
		///   is parsed.
		/// </para>
		/// </summary>
		/// <param name="lambda">The lambda expression to parse.</param>
		/// <param name="concretes">An optional array containing the non-dynamic arguments to
		/// use with the expression.</param>
		/// <returns>A new DynamicParser instance that holds the result of the parsed expression
		/// and the arguments used in it.</returns>
		public static DynamicParser Parse(Delegate lambda, params object[] concretes)
		{
			if (lambda == null) throw new ArgumentNullException("lambda", "Lambda Expression cannot be null.");
			var parser = new DynamicParser();

			ParameterInfo[] pars = lambda.Method.GetParameters();
			int index = 0;
			foreach (var par in pars)
			{
				bool isDynamic =
					par.GetCustomAttributes(typeof(DynamicAttribute), true).Length != 0 ? true : false;

				if (isDynamic)
				{
					var dyn = new DynamicNode.Argument(par.Name) { Parser = parser };
					parser._Arguments.Add(dyn);
				}
				else
				{
					if (index >= concretes.Length) throw new ArgumentException(
						 "Not enough non-dynamic arguments in '{0}'.".FormatWith(concretes.Sketch()));

					parser._Arguments.Add(concretes[index]);
					index++;
				}
			}

			try
			{
				parser._TentativeResult = lambda.DynamicInvoke(parser._Arguments.ToArray());
			}
			catch (TargetInvocationException e)
			{
				if (e.InnerException != null) throw e.InnerException;
				else throw e;
			}

			return parser;
		}

		bool _IsDisposed = false;
		List<object> _Arguments = new List<object>();
		object _TentativeResult = null;
		DynamicNode _LastNode = null;

		private DynamicParser() { }

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

		~DynamicParser()
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
				if (_TentativeResult != null && _TentativeResult is DynamicNode)
				{
					((DynamicNode)_TentativeResult).Dispose(DynamicNode.DEFAULT_DISPOSE_PARENT);
				}
				if (_LastNode != null)
				{
					_LastNode.Dispose(DynamicNode.DEFAULT_DISPOSE_PARENT);
				}
				var args = DynamicArguments; if (args != null)
				{
					foreach (var arg in args) arg.Dispose(DynamicNode.DEFAULT_DISPOSE_PARENT);
				}
			}
			if (_Arguments != null) _Arguments.Clear(); _Arguments = null;
			_TentativeResult = null;
			_LastNode = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("("); bool first = true; if (_Arguments != null)
			{
				foreach (var arg in _Arguments)
				{
					if (first) first = false; else sb.Append(", ");
					sb.Append(arg.Sketch());
				}
			}
			sb.AppendFormat(") => {0}", Result.Sketch());

			var str = sb.ToString();
			return IsDisposed ? string.Format("disposed::{0}({1})", GetType().EasyName(), str) : str;
		}

		/// <summary>
		/// The collection of arguments used when declaring the dynamic lambda expression, if any
		/// was used,l or null if this instance is disposed.
		/// </summary>
		public IEnumerable<object> Arguments
		{
			get { return _Arguments; }
		}

		/// <summary>
		/// The collection of dynamic arguments used when declaring the dynamic lambda expression,
		/// if any were used, or null if this instance is disposed.
		/// </summary>
		public IEnumerable<DynamicNode.Argument> DynamicArguments
		{
			get { return _Arguments == null ? null : _Arguments.OfType<DynamicNode.Argument>(); }
		}

		/// <summary>
		/// The last node binded by the parsing engine, or null.
		/// </summary>
		internal DynamicNode LastNode
		{
			get { return _LastNode; }
			set { _LastNode = value; }
		}

		/// <summary>
		/// The result of the parsing of the dynamic lambda expression used to construct this
		/// instance.
		/// <para>- This result can be a regular value, a null value, an object reference, or a
		/// dynamic node instance containing the last binded node from which the tree of logical
		/// operations binded can be obtained.</para>
		/// </summary>
		public object Result
		{
			get
			{
				if (_Arguments == null) return null;
				int count = DynamicArguments.Count();

				if (count == 0) return _TentativeResult; // No dynamic arguments used...
				if (_LastNode == null) return _TentativeResult; // No dynamic bindings...

				return _LastNode;
			}
		}
	}

	// ==================================================== 
	/// <summary>
	/// Represents an abstract node in the tree of logic operations discovered when parsing a
	/// dynamic lambda expression.
	/// </summary>
	[Serializable]
	public class DynamicNode
		: IDynamicMetaObjectProvider, IDisposableEx, ISerializable, ICloneable, IEquivalent<DynamicNode>
	{
		internal const bool DEFAULT_DISPOSE_PARENT = false;
		bool _IsDisposed = false;
		DynamicNode _Host = null;
		DynamicParser _Parser = null;

		/// <summary>
		/// Returns the object responsible for binding the dynamic operations on this object.
		/// </summary>
		/// <param name="parameter">The expression tree representation of the runtime value.</param>
		public DynamicMetaObject GetMetaObject(Expression parameter)
		{
			DynamicMetaObject meta = new DynamicMetaNode(
				parameter,
				BindingRestrictions.GetInstanceRestriction(parameter, this),
				this);

			return meta;
		}

		/// <summary>
		/// Initializes a new empty instance.
		/// </summary>
		protected DynamicNode() { }

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
			Dispose(DEFAULT_DISPOSE_PARENT);
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		/// <param name="disposeParent">True to also dispose the parent instance this one is
		/// hosted by, if any.</param>
		public void Dispose(bool disposeParent)
		{
			if (!IsDisposed) { OnDispose(true, disposeParent); GC.SuppressFinalize(this); }
		}

		~DynamicNode()
		{
			if (!IsDisposed) OnDispose(false, DEFAULT_DISPOSE_PARENT);
		}

		/// <summary>
		/// Invoked when disposing or finalizing this instance.
		/// </summary>
		/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
		/// <param name="disposeHost">True to also dispose the parent instance this one is
		/// hosted by, if any.</param>
		protected virtual void OnDispose(bool disposing, bool disposeHost)
		{
			if (disposing)
			{
				if (disposeHost && _Host != null && !_Host.IsDisposed)
				{
					var host = _Host; _Host = null; // Avoid re-entrance
					host.Dispose(disposeHost);
				}
			}
			_Host = null;
			_Parser = null;

			_IsDisposed = true;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the standard representation of this instance.</returns>
		public override string ToString()
		{
			string str = GetType().EasyName();
			return IsDisposed ? string.Format("disposed::{0})", str) : str;
		}

		/// <summary>
		/// Call-back method required for custom serialization.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			info.AddExtended("Host", _Host);
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected DynamicNode(SerializationInfo info, StreamingContext context)
		{
			_Host = (DynamicNode)info.GetExtended("Host");
		}

		/// <summary>
		/// Returns a new instance that is a copy of the original one.
		/// </summary>
		/// <returns>A new instance that is a copy of the original one.</returns>
		public DynamicNode Clone()
		{
			var cloned = new DynamicNode();
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
			var temp = cloned as DynamicNode;
			if (cloned == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(DynamicNode).EasyName()));

			// Casting to ICloneable to force dynamic resolution...
			temp._Host = (DynamicNode)(_Host == null ? null : ((ICloneable)_Host).Clone());
		}

		/// <summary>
		/// Returns true if the state of this object can be considered as equivalent to the target
		/// one, based upon any arbitrary criteria implemented in this method.
		/// </summary>
		/// <param name="target">The target instance this one will be tested for equivalence against.</param>
		/// <returns>True if the state of this instance can be considered as equivalent to the
		/// target one, or false otherwise.</returns>
		public bool EquivalentTo(DynamicNode target)
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
			var temp = target as DynamicNode; if (temp == null) return false;
			if (temp.IsDisposed) return false;
			if (IsDisposed) return false;

			return true; // At this level in the hierarchy all cats look alike...
		}

		/// <summary>
		/// The host this instance depends on, or null if no host is available.
		/// </summary>
		public DynamicNode Host
		{
			get { return _Host; }
		}

		/// <summary>
		/// The actual parser associated with this instance, if any.
		/// </summary>
		internal DynamicParser Parser
		{
			get { return _Parser; }
			set { _Parser = value; }
		}

		/// <summary>
		/// Returns whether the given node is an ancestor of this instance.
		/// </summary>
		/// <param name="node">The node to test.</param>
		/// <returns>True if the given node is an ancestor of this instance.</returns>
		public bool IsNodeAncestor(DynamicNode node)
		{
			if (node != null)
			{
				DynamicNode parent = _Host; while (parent != null)
				{
					if (object.ReferenceEquals(parent, node)) return true;
					parent = parent._Host;
				}
			}
			return false;
		}

		/// <summary>
		/// Changes the host of this instance to the new reference given.
		/// <para>
		/// This method is provided to facilitate the manipulation of node trees, at the caller's
		/// risk: there is no checks on whether the new reference is null, or if setting it to the
		/// new value would create cycles in the tree, or on any other condition.
		/// </para>
		/// </summary>
		/// <param name="newHost">The new host of this instance.</param>
		public void ChangeHost(DynamicNode newHost)
		{
			if (IsDisposed) throw new ObjectDisposedException(this.ToString());
			_Host = newHost;
		}

		// ================================================
		/// <summary>
		/// Represents an argument in a dynamic lambda expression, as in 'x => ...'.
		/// </summary>
		[Serializable]
		public class Argument : DynamicNode, ISerializable, ICloneable, IEquivalent<Argument>
		{
			string _Name = null;

			private Argument() { }

			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			/// <param name="name">The name of the dynamic argument this instance represents.</param>
			public Argument(string name)
			{
				_Name = name.Validated("Name");
			}

			/// <summary>
			/// Invoked when disposing or finalizing this instance.
			/// </summary>
			/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
			/// <param name="disposeHost">True to also dispose the parent instance this one is
			/// hosted by, if any.</param>
			protected override void OnDispose(bool disposing, bool disposeHost)
			{
				base.OnDispose(disposing, disposeHost);
			}

			/// <summary>
			/// Returns the string representation of this instance.
			/// </summary>
			/// <returns>A string containing the standard representation of this instance.</returns>
			public override string ToString()
			{
				string str = _Name ?? string.Empty;
				return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
			}

			/// <summary>
			/// Call-back method required for custom serialization.
			/// </summary>
			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);

				info.AddValue("Name", _Name);
			}

			/// <summary>
			/// Protected initializer required for custom serialization.
			/// </summary>
			protected Argument(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
				_Name = info.GetString("Name");
			}

			/// <summary>
			/// Returns a new instance that is a copy of the original one.
			/// </summary>
			/// <returns>A new instance that is a copy of the original one.</returns>
			public new Argument Clone()
			{
				var cloned = new Argument();
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
			protected override void OnClone(object cloned)
			{
				base.OnClone(cloned);
				var temp = cloned as Argument;
				if (cloned == null) throw new InvalidCastException(
					"Cloned instance '{0}' is not a valid '{1}' one."
					.FormatWith(cloned.Sketch(), typeof(Argument).EasyName()));

				temp._Name = _Name;
			}

			/// <summary>
			/// Returns true if the state of this object can be considered as equivalent to the target
			/// one, based upon any arbitrary criteria implemented in this method.
			/// </summary>
			/// <param name="target">The target instance this one will be tested for equivalence against.</param>
			/// <returns>True if the state of this instance can be considered as equivalent to the
			/// target one, or false otherwise.</returns>
			public bool EquivalentTo(Argument target)
			{
				return OnEquivalentTo(target);
			}

			/// <summary>
			/// Invoked to test equivalence at this point of the inheritance chain.
			/// </summary>
			/// <param name="target">The target this instance will be tested for equivalence against.</param>
			/// <returns>True if at this level on the inheritance chain this instance can be considered
			/// equivalent to the target instance given.</returns>
			protected override bool OnEquivalentTo(object target)
			{
				if (base.OnEquivalentTo(target))
				{
					var temp = target as Argument; if (temp == null) return false;

					if (string.Compare(_Name, temp.Name) != 0) return false;
					return true;
				}
				return false;
			}

			/// <summary>
			/// The name of the argument this instance refers to.
			/// </summary>
			public string Name
			{
				get { return _Name; }
			}
		}

		// ================================================
		/// <summary>
		/// Represents a dynamic get member expression, as in 'x => x.Member'.
		/// </summary>
		[Serializable]
		public class GetMember : DynamicNode, ISerializable, ICloneable, IEquivalent<GetMember>
		{
			string _Name = null;

			private GetMember() { }

			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			/// <param name="host">The host where this member is binded.</param>
			/// <param name="name">The name of the dynamic member this instance represents.</param>
			public GetMember(DynamicNode host, string name)
			{
				if ((_Host = host) == null) throw new ArgumentNullException("host", "Host cannot be null.");
				if (_Host.IsDisposed) throw new ObjectDisposedException(_Host.ToString());

				_Name = name.Validated("Name");
			}

			/// <summary>
			/// Invoked when disposing or finalizing this instance.
			/// </summary>
			/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
			/// <param name="disposeHost">True to also dispose the parent instance this one is
			/// hosted by, if any.</param>
			protected override void OnDispose(bool disposing, bool disposeHost)
			{
				base.OnDispose(disposing, disposeHost);
			}

			/// <summary>
			/// Returns the string representation of this instance.
			/// </summary>
			/// <returns>A string containing the standard representation of this instance.</returns>
			public override string ToString()
			{
				string str = string.Format("{0}.{1}",
					_Host == null ? string.Empty : _Host.ToString(),
					_Name ?? string.Empty);

				return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
			}

			/// <summary>
			/// Call-back method required for custom serialization.
			/// </summary>
			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);

				info.AddValue("Name", _Name);
			}

			/// <summary>
			/// Protected initializer required for custom serialization.
			/// </summary>
			protected GetMember(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
				_Name = info.GetString("Name");
			}

			/// <summary>
			/// Returns a new instance that is a copy of the original one.
			/// </summary>
			/// <returns>A new instance that is a copy of the original one.</returns>
			public new GetMember Clone()
			{
				var cloned = new GetMember();
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
			protected override void OnClone(object cloned)
			{
				base.OnClone(cloned);
				var temp = cloned as GetMember;
				if (cloned == null) throw new InvalidCastException(
					"Cloned instance '{0}' is not a valid '{1}' one."
					.FormatWith(cloned.Sketch(), typeof(GetMember).EasyName()));

				temp._Name = _Name;
			}

			/// <summary>
			/// Returns true if the state of this object can be considered as equivalent to the target
			/// one, based upon any arbitrary criteria implemented in this method.
			/// </summary>
			/// <param name="target">The target instance this one will be tested for equivalence against.</param>
			/// <returns>True if the state of this instance can be considered as equivalent to the
			/// target one, or false otherwise.</returns>
			public bool EquivalentTo(GetMember target)
			{
				return OnEquivalentTo(target);
			}

			/// <summary>
			/// Invoked to test equivalence at this point of the inheritance chain.
			/// </summary>
			/// <param name="target">The target this instance will be tested for equivalence against.</param>
			/// <returns>True if at this level on the inheritance chain this instance can be considered
			/// equivalent to the target instance given.</returns>
			protected override bool OnEquivalentTo(object target)
			{
				if (base.OnEquivalentTo(target))
				{
					var temp = target as GetMember; if (temp == null) return false;

					if (string.Compare(_Name, temp.Name) != 0) return false;
					return true;
				}
				return false;
			}

			/// <summary>
			/// The name of the member this instance refers to.
			/// </summary>
			public string Name
			{
				get { return _Name; }
			}
		}

		// ================================================
		/// <summary>
		/// Represents a dynamic set member expression, as in 'x => x.Member = Value'.
		/// </summary>
		[Serializable]
		public class SetMember : DynamicNode, ISerializable, ICloneable, IEquivalent<SetMember>
		{
			string _Name = null;
			object _Value = null;

			private SetMember() { }

			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			/// <param name="host">The host where this member is binded.</param>
			/// <param name="name">The name of the dynamic member this instance represents.</param>
			/// <param name="value">The value to set into this member.</param>
			public SetMember(DynamicNode host, string name, object value)
			{
				if ((_Host = host) == null) throw new ArgumentNullException("host", "Host cannot be null.");
				if (_Host.IsDisposed) throw new ObjectDisposedException(_Host.ToString());

				_Name = name.Validated("Name");
				_Value = value;
			}

			/// <summary>
			/// Invoked when disposing or finalizing this instance.
			/// </summary>
			/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
			/// <param name="disposeHost">True to also dispose the parent instance this one is
			/// hosted by, if any.</param>
			protected override void OnDispose(bool disposing, bool disposeHost)
			{
				if (disposing)
				{
					if (_Value != null)
					{
						var node = _Value as DynamicNode; if (node != null)
						{
							if (disposeHost && node.IsNodeAncestor(this)) node._Host = null;
							node.Dispose(disposeHost);
						}
					}
				}
				_Value = null;

				base.OnDispose(disposing, disposeHost);
			}

			/// <summary>
			/// Returns the string representation of this instance.
			/// </summary>
			/// <returns>A string containing the standard representation of this instance.</returns>
			public override string ToString()
			{
				string str = string.Format("({0}.{1} = {2})",
					_Host == null ? string.Empty : _Host.ToString(),
					_Name ?? string.Empty,
					_Value.Sketch());

				return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
			}

			/// <summary>
			/// Call-back method required for custom serialization.
			/// </summary>
			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);

				info.AddValue("Name", _Name);
				info.AddExtended("Value", _Value);
			}

			/// <summary>
			/// Protected initializer required for custom serialization.
			/// </summary>
			protected SetMember(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
				_Name = info.GetString("Name");
				_Value = info.GetExtended("Value");
			}

			/// <summary>
			/// Returns a new instance that is a copy of the original one.
			/// </summary>
			/// <returns>A new instance that is a copy of the original one.</returns>
			public new SetMember Clone()
			{
				var cloned = new SetMember();
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
			protected override void OnClone(object cloned)
			{
				base.OnClone(cloned);
				var temp = cloned as SetMember;
				if (cloned == null) throw new InvalidCastException(
					"Cloned instance '{0}' is not a valid '{1}' one."
					.FormatWith(cloned.Sketch(), typeof(SetMember).EasyName()));

				temp._Name = _Name;
				temp._Value = _Value.TryClone();
			}

			/// <summary>
			/// Returns true if the state of this object can be considered as equivalent to the target
			/// one, based upon any arbitrary criteria implemented in this method.
			/// </summary>
			/// <param name="target">The target instance this one will be tested for equivalence against.</param>
			/// <returns>True if the state of this instance can be considered as equivalent to the
			/// target one, or false otherwise.</returns>
			public bool EquivalentTo(SetMember target)
			{
				return OnEquivalentTo(target);
			}

			/// <summary>
			/// Invoked to test equivalence at this point of the inheritance chain.
			/// </summary>
			/// <param name="target">The target this instance will be tested for equivalence against.</param>
			/// <returns>True if at this level on the inheritance chain this instance can be considered
			/// equivalent to the target instance given.</returns>
			protected override bool OnEquivalentTo(object target)
			{
				if (base.OnEquivalentTo(target))
				{
					var temp = target as SetMember; if (temp == null) return false;

					if (string.Compare(_Name, temp.Name) != 0) return false;
					if (!_Value.IsEquivalentTo(temp._Value)) return false;
					return true;
				}
				return false;
			}

			/// <summary>
			/// The name of the member this instance refers to.
			/// </summary>
			public string Name
			{
				get { return _Name; }
			}

			/// <summary>
			/// The value to set into the dynamic member this instance refers to.
			/// </summary>
			public object Value
			{
				get { return _Value; }
			}
		}

		// ================================================
		/// <summary>
		/// Represents a dynamic get indexed member operation, as in 'x => x.Member[...]'.
		/// </summary>
		[Serializable]
		public class GetIndexed : DynamicNode, ISerializable, ICloneable, IEquivalent<GetIndexed>
		{
			object[] _Indexes = null;

			private GetIndexed() { }

			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			/// <param name="host">The host where this member is binded.</param>
			/// <param name="indexes">The indexes to use to access this member.</param>
			public GetIndexed(DynamicNode host, object[] indexes)
			{
				if ((_Host = host) == null) throw new ArgumentNullException("host", "Host cannot be null.");
				if (_Host.IsDisposed) throw new ObjectDisposedException(_Host.ToString());

				if ((_Indexes = indexes) == null) throw new ArgumentNullException("indexes", "Indexes array cannot be null.");
				if (_Indexes.Length == 0) throw new ArgumentException("Indexes array cannot be empty.");
			}

			/// <summary>
			/// Invoked when disposing or finalizing this instance.
			/// </summary>
			/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
			/// <param name="disposeHost">True to also dispose the parent instance this one is
			/// hosted by, if any.</param>
			protected override void OnDispose(bool disposing, bool disposeHost)
			{
				if (disposing)
				{
					if (_Indexes != null)
					{
						for (int i = 0; i < _Indexes.Length; i++)
						{
							var node = _Indexes[i] as DynamicNode;
							if (node != null)
							{
								if (disposeHost && node.IsNodeAncestor(this)) node._Host = null;
								node.Dispose(disposeHost);
							}
						}
						Array.Clear(_Indexes, 0, _Indexes.Length);
					}
				}
				_Indexes = null;

				base.OnDispose(disposing, disposeHost);
			}

			/// <summary>
			/// Returns the string representation of this instance.
			/// </summary>
			/// <returns>A string containing the standard representation of this instance.</returns>
			public override string ToString()
			{
				string str = string.Format("{0}{1}",
					_Host == null ? string.Empty : _Host.ToString(),
					_Indexes == null ? "[]" : _Indexes.Sketch());

				return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
			}

			/// <summary>
			/// Call-back method required for custom serialization.
			/// </summary>
			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);

				info.AddExtended("Indexes", _Indexes);
			}

			/// <summary>
			/// Protected initializer required for custom serialization.
			/// </summary>
			protected GetIndexed(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
				_Indexes = info.GetExtended<object[]>("Indexes");
			}

			/// <summary>
			/// Returns a new instance that is a copy of the original one.
			/// </summary>
			/// <returns>A new instance that is a copy of the original one.</returns>
			public new GetIndexed Clone()
			{
				var cloned = new GetIndexed();
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
			protected override void OnClone(object cloned)
			{
				base.OnClone(cloned);
				var temp = cloned as GetIndexed;
				if (cloned == null) throw new InvalidCastException(
					"Cloned instance '{0}' is not a valid '{1}' one."
					.FormatWith(cloned.Sketch(), typeof(GetIndexed).EasyName()));

				int count = _Indexes == null ? 0 : _Indexes.Length;
				temp._Indexes = new object[count];
				for (int i = 0; i < count; i++) temp._Indexes[i] = _Indexes[i].TryClone();
			}

			/// <summary>
			/// Returns true if the state of this object can be considered as equivalent to the target
			/// one, based upon any arbitrary criteria implemented in this method.
			/// </summary>
			/// <param name="target">The target instance this one will be tested for equivalence against.</param>
			/// <returns>True if the state of this instance can be considered as equivalent to the
			/// target one, or false otherwise.</returns>
			public bool EquivalentTo(GetIndexed target)
			{
				return OnEquivalentTo(target);
			}

			/// <summary>
			/// Invoked to test equivalence at this point of the inheritance chain.
			/// </summary>
			/// <param name="target">The target this instance will be tested for equivalence against.</param>
			/// <returns>True if at this level on the inheritance chain this instance can be considered
			/// equivalent to the target instance given.</returns>
			protected override bool OnEquivalentTo(object target)
			{
				if (base.OnEquivalentTo(target))
				{
					var temp = target as GetIndexed; if (temp == null) return false;

					int thiscount = _Indexes == null ? 0 : _Indexes.Length;
					int tempcount = temp._Indexes == null ? 0 : temp._Indexes.Length;
					if (thiscount != tempcount) return false;
					for (int i = 0; i < thiscount; i++) if (!_Indexes[i].IsEquivalentTo(temp._Indexes[i])) return false;

					return true;
				}
				return false;
			}

			/// <summary>
			/// The indexes used to access the dynamic member this instance refers to.
			/// </summary>
			public object[] Indexes
			{
				get { return _Indexes; }
			}
		}

		// ================================================
		/// <summary>
		/// Represents a dynamic set indexed member operation, as in 'x => x.Member[...] = Value'.
		/// </summary>
		[Serializable]
		public class SetIndexed : DynamicNode, ISerializable, ICloneable, IEquivalent<SetIndexed>
		{
			object[] _Indexes = null;
			object _Value = null;

			private SetIndexed() { }

			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			/// <param name="host">The host where this member is binded.</param>
			/// <param name="indexes">The indexes to use to access this member.</param>
			/// <param name="value">The value to set into this member.</param>
			public SetIndexed(DynamicNode host, object[] indexes, object value)
			{
				if ((_Host = host) == null) throw new ArgumentNullException("host", "Host cannot be null.");
				if (_Host.IsDisposed) throw new ObjectDisposedException(_Host.ToString());

				if ((_Indexes = indexes) == null) throw new ArgumentNullException("indexes", "Indexes array cannot be null.");
				if (_Indexes.Length == 0) throw new ArgumentException("Indexes array cannot be empty.");

				_Value = value;
			}

			/// <summary>
			/// Invoked when disposing or finalizing this instance.
			/// </summary>
			/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
			/// <param name="disposeHost">True to also dispose the parent instance this one is
			/// hosted by, if any.</param>
			protected override void OnDispose(bool disposing, bool disposeHost)
			{
				if (disposing)
				{
					if (_Indexes != null)
					{
						for (int i = 0; i < _Indexes.Length; i++)
						{
							var node = _Indexes[i] as DynamicNode;
							if (node != null)
							{
								if (disposeHost && node.IsNodeAncestor(this)) node._Host = null;
								node.Dispose(disposeHost);
							}
						}
						Array.Clear(_Indexes, 0, _Indexes.Length);
					}
					if (_Value != null)
					{
						var node = _Value as DynamicNode;
						if (node != null)
						{
							if (disposeHost && node.IsNodeAncestor(this)) node._Host = null;
							node.Dispose(disposeHost);
						}
					}
				}
				_Indexes = null;
				_Value = null;

				base.OnDispose(disposing, disposeHost);
			}

			/// <summary>
			/// Returns the string representation of this instance.
			/// </summary>
			/// <returns>A string containing the standard representation of this instance.</returns>
			public override string ToString()
			{
				string str = string.Format("({0}{1} = {2})",
					_Host == null ? string.Empty : _Host.ToString(),
					_Indexes == null ? "[]" : _Indexes.Sketch(),
					_Value.Sketch());

				return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
			}

			/// <summary>
			/// Call-back method required for custom serialization.
			/// </summary>
			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);

				info.AddExtended("Indexes", _Indexes);
				info.AddExtended("Value", _Value);
			}

			/// <summary>
			/// Protected initializer required for custom serialization.
			/// </summary>
			protected SetIndexed(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
				_Indexes = info.GetExtended<object[]>("Indexes");
				_Value = info.GetExtended("Value");
			}

			/// <summary>
			/// Returns a new instance that is a copy of the original one.
			/// </summary>
			/// <returns>A new instance that is a copy of the original one.</returns>
			public new SetIndexed Clone()
			{
				var cloned = new SetIndexed();
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
			protected override void OnClone(object cloned)
			{
				base.OnClone(cloned);
				var temp = cloned as SetIndexed;
				if (cloned == null) throw new InvalidCastException(
					"Cloned instance '{0}' is not a valid '{1}' one."
					.FormatWith(cloned.Sketch(), typeof(SetIndexed).EasyName()));

				int count = _Indexes == null ? 0 : _Indexes.Length;
				temp._Indexes = new object[count];
				for (int i = 0; i < count; i++) temp._Indexes[i] = _Indexes[i].TryClone();

				temp._Value = _Value.TryClone();
			}

			/// <summary>
			/// Returns true if the state of this object can be considered as equivalent to the target
			/// one, based upon any arbitrary criteria implemented in this method.
			/// </summary>
			/// <param name="target">The target instance this one will be tested for equivalence against.</param>
			/// <returns>True if the state of this instance can be considered as equivalent to the
			/// target one, or false otherwise.</returns>
			public bool EquivalentTo(SetIndexed target)
			{
				return OnEquivalentTo(target);
			}

			/// <summary>
			/// Invoked to test equivalence at this point of the inheritance chain.
			/// </summary>
			/// <param name="target">The target this instance will be tested for equivalence against.</param>
			/// <returns>True if at this level on the inheritance chain this instance can be considered
			/// equivalent to the target instance given.</returns>
			protected override bool OnEquivalentTo(object target)
			{
				if (base.OnEquivalentTo(target))
				{
					var temp = target as SetIndexed; if (temp == null) return false;

					int thiscount = _Indexes == null ? 0 : _Indexes.Length;
					int tempcount = temp._Indexes == null ? 0 : temp._Indexes.Length;
					if (thiscount != tempcount) return false;
					for (int i = 0; i < thiscount; i++) if (!_Indexes[i].IsEquivalentTo(temp._Indexes[i])) return false;

					if (!_Value.IsEquivalentTo(temp._Value)) return false;

					return true;
				}
				return false;
			}

			/// <summary>
			/// The indexes used to access the dynamic member this instance refers to.
			/// </summary>
			public object[] Indexes
			{
				get { return _Indexes; }
			}

			/// <summary>
			/// The value to set into the dynamic member this instance refers to.
			/// </summary>
			public object Value
			{
				get { return _Value; }
			}
		}

		// ================================================
		/// <summary>
		/// Represents a dynamic method invocation operation, as in 'x => x.Method(...)'.
		/// </summary>
		[Serializable]
		public class Method : DynamicNode, ISerializable, ICloneable, IEquivalent<Method>
		{
			string _Name = null;
			object[] _Arguments = null;

			private Method() { }

			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			/// <param name="host">The host where this method is binded.</param>
			/// <param name="name">The name of the dynamic method this instance represents.</param>
			/// <param name="arguments">An array containing the arguments to use to invoke this
			/// method, or null if no arguments are used. An empty array is not captured and the
			/// property that holds the list of arguments becomes null.</param>
			public Method(DynamicNode host, string name, object[] arguments)
			{
				if ((_Host = host) == null) throw new ArgumentNullException("host", "Host cannot be null.");
				if (_Host.IsDisposed) throw new ObjectDisposedException(_Host.ToString());

				_Name = name.Validated("Name");
				_Arguments = (arguments == null || arguments.Length == 0) ? null : arguments;
			}

			/// <summary>
			/// Invoked when disposing or finalizing this instance.
			/// </summary>
			/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
			/// <param name="disposeHost">True to also dispose the parent instance this one is
			/// hosted by, if any.</param>
			protected override void OnDispose(bool disposing, bool disposeHost)
			{
				if (disposing)
				{
					if (_Arguments != null)
					{
						for (int i = 0; i < _Arguments.Length; i++)
						{
							var node = _Arguments[i] as DynamicNode;
							if (node != null)
							{
								if (disposeHost && node.IsNodeAncestor(this)) node._Host = null;
								node.Dispose(disposeHost);
							}
						}
						Array.Clear(_Arguments, 0, _Arguments.Length);
					}
				}
				_Arguments = null;

				base.OnDispose(disposing, disposeHost);
			}

			/// <summary>
			/// Returns the string representation of this instance.
			/// </summary>
			/// <returns>A string containing the standard representation of this instance.</returns>
			public override string ToString()
			{
				string str = string.Format("{0}.{1}{2}",
					_Host == null ? string.Empty : _Host.ToString(),
					_Name ?? string.Empty,
					_Arguments == null ? "()" : _Arguments.Sketch(SketchOptions.RoundedBrackets));

				return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
			}

			/// <summary>
			/// Call-back method required for custom serialization.
			/// </summary>
			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);

				info.AddValue("Name", _Name);
				info.AddExtended("Arguments", _Arguments);
			}

			/// <summary>
			/// Protected initializer required for custom serialization.
			/// </summary>
			protected Method(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
				_Name = info.GetString("Name");
				_Arguments = info.GetExtended<object[]>("Arguments");
			}

			/// <summary>
			/// Returns a new instance that is a copy of the original one.
			/// </summary>
			/// <returns>A new instance that is a copy of the original one.</returns>
			public new Method Clone()
			{
				var cloned = new Method();
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
			protected override void OnClone(object cloned)
			{
				base.OnClone(cloned);
				var temp = cloned as Method;
				if (cloned == null) throw new InvalidCastException(
					"Cloned instance '{0}' is not a valid '{1}' one."
					.FormatWith(cloned.Sketch(), typeof(Method).EasyName()));

				temp._Name = _Name;

				int count = _Arguments == null ? 0 : _Arguments.Length;
				temp._Arguments = count != 0 ? new object[count] : null;
				for (int i = 0; i < count; i++) temp._Arguments[i] = _Arguments[i].TryClone();
			}

			/// <summary>
			/// Returns true if the state of this object can be considered as equivalent to the target
			/// one, based upon any arbitrary criteria implemented in this method.
			/// </summary>
			/// <param name="target">The target instance this one will be tested for equivalence against.</param>
			/// <returns>True if the state of this instance can be considered as equivalent to the
			/// target one, or false otherwise.</returns>
			public bool EquivalentTo(Method target)
			{
				return OnEquivalentTo(target);
			}

			/// <summary>
			/// Invoked to test equivalence at this point of the inheritance chain.
			/// </summary>
			/// <param name="target">The target this instance will be tested for equivalence against.</param>
			/// <returns>True if at this level on the inheritance chain this instance can be considered
			/// equivalent to the target instance given.</returns>
			protected override bool OnEquivalentTo(object target)
			{
				if (base.OnEquivalentTo(target))
				{
					var temp = target as Method; if (temp == null) return false;

					if (string.Compare(_Name, temp.Name) != 0) return false;

					int thiscount = _Arguments == null ? 0 : _Arguments.Length;
					int tempcount = temp._Arguments == null ? 0 : temp._Arguments.Length;
					if (thiscount != tempcount) return false;
					for (int i = 0; i < thiscount; i++)
						if (!_Arguments[i].IsEquivalentTo(temp._Arguments[i])) return false;

					return true;
				}
				return false;
			}

			/// <summary>
			/// The name of the method this instance refers to.
			/// </summary>
			public string Name
			{
				get { return _Name; }
			}

			/// <summary>
			/// The arguments used to the method invocation this instance refers to, or null if
			/// no arguments were used or if the array of arguments was empty.
			/// </summary>
			public object[] Arguments
			{
				get { return _Arguments; }
			}
		}

		// ================================================
		/// <summary>
		/// Represents a dynamic direct invocation operation, as in 'x => x(...)'.
		/// </summary>
		[Serializable]
		public class Invoke : DynamicNode, ISerializable, ICloneable, IEquivalent<Invoke>
		{
			object[] _Arguments = null;

			private Invoke() { }

			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			/// <param name="host">The host that is invoked.</param>
			/// <param name="arguments">An array containing the arguments to use to invoke this
			/// host, or null if no arguments are used. An empty array is not captured and the
			/// property that holds the list of arguments becomes null.</param>
			public Invoke(DynamicNode host, object[] arguments)
			{
				if ((_Host = host) == null) throw new ArgumentNullException("host", "Host cannot be null.");
				if (_Host.IsDisposed) throw new ObjectDisposedException(_Host.ToString());

				_Arguments = (arguments == null || arguments.Length == 0) ? null : arguments;
			}

			/// <summary>
			/// Invoked when disposing or finalizing this instance.
			/// </summary>
			/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
			/// <param name="disposeHost">True to also dispose the parent instance this one is
			/// hosted by, if any.</param>
			protected override void OnDispose(bool disposing, bool disposeHost)
			{
				if (disposing)
				{
					if (_Arguments != null)
					{
						for (int i = 0; i < _Arguments.Length; i++)
						{
							var node = _Arguments[i] as DynamicNode;
							if (node != null)
							{
								if (disposeHost && node.IsNodeAncestor(this)) node._Host = null;
								node.Dispose(disposeHost);
							}
						}
						Array.Clear(_Arguments, 0, _Arguments.Length);
					}
				}
				_Arguments = null;

				base.OnDispose(disposing, disposeHost);
			}

			/// <summary>
			/// Returns the string representation of this instance.
			/// </summary>
			/// <returns>A string containing the standard representation of this instance.</returns>
			public override string ToString()
			{
				string str = string.Format("{0}{1}",
					_Host == null ? string.Empty : _Host.ToString(),
					_Arguments == null ? "()" : _Arguments.Sketch(SketchOptions.RoundedBrackets));

				return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
			}

			/// <summary>
			/// Call-back method required for custom serialization.
			/// </summary>
			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);

				info.AddExtended("Arguments", _Arguments);
			}

			/// <summary>
			/// Protected initializer required for custom serialization.
			/// </summary>
			protected Invoke(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
				_Arguments = info.GetExtended<object[]>("Arguments");
			}

			/// <summary>
			/// Returns a new instance that is a copy of the original one.
			/// </summary>
			/// <returns>A new instance that is a copy of the original one.</returns>
			public new Invoke Clone()
			{
				var cloned = new Invoke();
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
			protected override void OnClone(object cloned)
			{
				base.OnClone(cloned);
				var temp = cloned as Invoke;
				if (cloned == null) throw new InvalidCastException(
					"Cloned instance '{0}' is not a valid '{1}' one."
					.FormatWith(cloned.Sketch(), typeof(Invoke).EasyName()));

				int count = _Arguments == null ? 0 : _Arguments.Length;
				temp._Arguments = count != 0 ? new object[count] : null;
				for (int i = 0; i < count; i++) temp._Arguments[i] = _Arguments[i].TryClone();
			}

			/// <summary>
			/// Returns true if the state of this object can be considered as equivalent to the target
			/// one, based upon any arbitrary criteria implemented in this method.
			/// </summary>
			/// <param name="target">The target instance this one will be tested for equivalence against.</param>
			/// <returns>True if the state of this instance can be considered as equivalent to the
			/// target one, or false otherwise.</returns>
			public bool EquivalentTo(Invoke target)
			{
				return OnEquivalentTo(target);
			}

			/// <summary>
			/// Invoked to test equivalence at this point of the inheritance chain.
			/// </summary>
			/// <param name="target">The target this instance will be tested for equivalence against.</param>
			/// <returns>True if at this level on the inheritance chain this instance can be considered
			/// equivalent to the target instance given.</returns>
			protected override bool OnEquivalentTo(object target)
			{
				if (base.OnEquivalentTo(target))
				{
					var temp = target as Invoke; if (temp == null) return false;

					int thiscount = _Arguments == null ? 0 : _Arguments.Length;
					int tempcount = temp._Arguments == null ? 0 : temp._Arguments.Length;
					if (thiscount != tempcount) return false;
					for (int i = 0; i < thiscount; i++)
						if (!_Arguments[i].IsEquivalentTo(temp._Arguments[i])) return false;

					return true;
				}
				return false;
			}

			/// <summary>
			/// The arguments used to invoke this instance, or null if no arguments were used or
			/// if the array of arguments was empty.
			/// </summary>
			public object[] Arguments
			{
				get { return _Arguments; }
			}
		}

		// ================================================
		/// <summary>
		/// Represents a dynamic binary operation, as in 'x => Left op Right'.
		/// <para>The left argument must be a dynamic node.</para>
		/// </summary>
		[Serializable]
		public class Binary : DynamicNode, ISerializable, ICloneable, IEquivalent<Binary>
		{
			DynamicNode _Left = null;
			ExpressionType _Operation = ExpressionType.Default;
			object _Right = null;

			private Binary() { }

			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			/// <param name="left">The left node of the operation.</param>
			/// <param name="operation">The operation that binds both arguments.</param>
			/// <param name="right">The right node of the operation.</param>
			public Binary(DynamicNode left, ExpressionType op, object right)
			{
				if ((_Left = left) == null) throw new ArgumentNullException("left", "Left cannot be null.");
				if (_Left.IsDisposed) throw new ObjectDisposedException(_Left.ToString());

				if ((_Right = right) != null &&
					(_Right is DynamicNode) && ((DynamicNode)_Right).IsDisposed)
					throw new ObjectDisposedException(_Right.ToString());

				_Operation = op;
			}

			/// <summary>
			/// Invoked when disposing or finalizing this instance.
			/// </summary>
			/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
			/// <param name="disposeHost">True to also dispose the parent instance this one is
			/// hosted by, if any.</param>
			protected override void OnDispose(bool disposing, bool disposeHost)
			{
				if (disposing)
				{
					if (_Left != null)
					{
						if (disposeHost && _Left.IsNodeAncestor(this)) _Left._Host = null;
						_Left.Dispose(disposeHost);
					}
					if (_Right != null)
					{
						var node = _Right as DynamicNode;
						if (node != null)
						{
							if (disposeHost && node.IsNodeAncestor(this)) node._Host = null;
							node.Dispose(disposeHost);
						}
					}
				}
				_Left = null;
				_Right = null;

				base.OnDispose(disposing, disposeHost);
			}

			/// <summary>
			/// Returns the string representation of this instance.
			/// </summary>
			/// <returns>A string containing the standard representation of this instance.</returns>
			public override string ToString()
			{
				string str = string.Format("({0} {1} {2})",
					_Left == null ? string.Empty : _Left.ToString(),
					_Operation,
					_Right == null ? string.Empty : _Right.Sketch());

				return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
			}

			/// <summary>
			/// Call-back method required for custom serialization.
			/// </summary>
			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);

				info.AddExtended("Left", _Left);
				info.AddValue("Operation", _Operation);
				info.AddExtended("Right", _Right);
			}

			/// <summary>
			/// Protected initializer required for custom serialization.
			/// </summary>
			protected Binary(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
				_Left = (DynamicNode)info.GetExtended("Left");
				_Operation = (ExpressionType)info.GetValue("Operation", typeof(ExpressionType));
				_Right = info.GetExtended("Right");
			}

			/// <summary>
			/// Returns a new instance that is a copy of the original one.
			/// </summary>
			/// <returns>A new instance that is a copy of the original one.</returns>
			public new Binary Clone()
			{
				var cloned = new Binary();
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
			protected override void OnClone(object cloned)
			{
				base.OnClone(cloned);
				var temp = cloned as Binary;
				if (cloned == null) throw new InvalidCastException(
					"Cloned instance '{0}' is not a valid '{1}' one."
					.FormatWith(cloned.Sketch(), typeof(Binary).EasyName()));

				temp._Left = (DynamicNode)(_Left == null ? null : ((ICloneable)_Left).Clone()); // For casting...
				temp._Operation = _Operation;
				temp._Right = _Right.TryClone();
			}

			/// <summary>
			/// Returns true if the state of this object can be considered as equivalent to the target
			/// one, based upon any arbitrary criteria implemented in this method.
			/// </summary>
			/// <param name="target">The target instance this one will be tested for equivalence against.</param>
			/// <returns>True if the state of this instance can be considered as equivalent to the
			/// target one, or false otherwise.</returns>
			public bool EquivalentTo(Binary target)
			{
				return OnEquivalentTo(target);
			}

			/// <summary>
			/// Invoked to test equivalence at this point of the inheritance chain.
			/// </summary>
			/// <param name="target">The target this instance will be tested for equivalence against.</param>
			/// <returns>True if at this level on the inheritance chain this instance can be considered
			/// equivalent to the target instance given.</returns>
			protected override bool OnEquivalentTo(object target)
			{
				if (base.OnEquivalentTo(target))
				{
					var temp = target as Binary; if (temp == null) return false;

					if (_Operation != temp._Operation) return false;
					if (!_Left.IsEquivalentTo(temp._Left)) return false;
					if (!_Right.IsEquivalentTo(temp._Right)) return false;

					return true;
				}
				return false;
			}

			/// <summary>
			/// The left operand of the operation this instance refers to.
			/// </summary>
			public DynamicNode Left
			{
				get { return _Left; }
			}

			/// <summary>
			/// The binary operation this instance refers to.
			/// </summary>
			public ExpressionType Operation
			{
				get { return _Operation; }
			}

			/// <summary>
			/// The right operand of the operation this instance refers to.
			/// </summary>
			public object Right
			{
				get { return _Right; }
			}
		}

		// ================================================
		/// <summary>
		/// Represents a dynamic unary operation, as in 'x => op Target'.
		/// <para>The target operator must be a dynamic one.</para>
		/// </summary>
		[Serializable]
		public class Unary : DynamicNode, ISerializable, ICloneable, IEquivalent<Unary>
		{
			ExpressionType _Operation = ExpressionType.Default;
			DynamicNode _Target = null;

			private Unary() { }

			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			/// <param name="operation">The operation that binds the target argument.</param>
			/// <param name="target">The target node of the operation.</param>
			public Unary(ExpressionType op, DynamicNode target)
			{
				if ((_Target = target) == null) throw new ArgumentNullException("target", "Target cannot be null.");
				if (_Target.IsDisposed) throw new ObjectDisposedException(_Target.ToString());

				_Operation = op;
			}

			/// <summary>
			/// Invoked when disposing or finalizing this instance.
			/// </summary>
			/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
			/// <param name="disposeHost">True to also dispose the parent instance this one is
			/// hosted by, if any.</param>
			protected override void OnDispose(bool disposing, bool disposeHost)
			{
				if (disposing)
				{
					if (_Target != null)
					{
						if (disposeHost && _Target.IsNodeAncestor(this)) _Target._Host = null;
						_Target.Dispose(disposeHost);
					}
				}
				_Target = null;

				base.OnDispose(disposing, disposeHost);
			}

			/// <summary>
			/// Returns the string representation of this instance.
			/// </summary>
			/// <returns>A string containing the standard representation of this instance.</returns>
			public override string ToString()
			{
				string str = string.Format("({0} {1})",
					_Operation,
					_Target == null ? string.Empty : _Target.ToString());

				return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
			}

			/// <summary>
			/// Call-back method required for custom serialization.
			/// </summary>
			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);

				info.AddExtended("Target", _Target);
				info.AddValue("Operation", _Operation);
			}

			/// <summary>
			/// Protected initializer required for custom serialization.
			/// </summary>
			protected Unary(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
				_Target = (DynamicNode)info.GetExtended("Target");
				_Operation = (ExpressionType)info.GetValue("Operation", typeof(ExpressionType));
			}

			/// <summary>
			/// Returns a new instance that is a copy of the original one.
			/// </summary>
			/// <returns>A new instance that is a copy of the original one.</returns>
			public new Unary Clone()
			{
				var cloned = new Unary();
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
			protected override void OnClone(object cloned)
			{
				base.OnClone(cloned);
				var temp = cloned as Unary;
				if (cloned == null) throw new InvalidCastException(
					"Cloned instance '{0}' is not a valid '{1}' one."
					.FormatWith(cloned.Sketch(), typeof(Unary).EasyName()));

				temp._Target = (DynamicNode)(_Target == null ? null : ((ICloneable)_Target).Clone()); // For casting...
				temp._Operation = _Operation;
			}

			/// <summary>
			/// Returns true if the state of this object can be considered as equivalent to the target
			/// one, based upon any arbitrary criteria implemented in this method.
			/// </summary>
			/// <param name="target">The target instance this one will be tested for equivalence against.</param>
			/// <returns>True if the state of this instance can be considered as equivalent to the
			/// target one, or false otherwise.</returns>
			public bool EquivalentTo(Unary target)
			{
				return OnEquivalentTo(target);
			}

			/// <summary>
			/// Invoked to test equivalence at this point of the inheritance chain.
			/// </summary>
			/// <param name="target">The target this instance will be tested for equivalence against.</param>
			/// <returns>True if at this level on the inheritance chain this instance can be considered
			/// equivalent to the target instance given.</returns>
			protected override bool OnEquivalentTo(object target)
			{
				if (base.OnEquivalentTo(target))
				{
					var temp = target as Unary; if (temp == null) return false;

					if (_Operation != temp._Operation) return false;
					if (!_Target.IsEquivalentTo(temp._Target)) return false;

					return true;
				}
				return false;
			}

			/// <summary>
			/// The unary operation this instance refers to.
			/// </summary>
			public ExpressionType Operation
			{
				get { return _Operation; }
			}

			/// <summary>
			/// The target node of the operation this instance refers to.
			/// </summary>
			public DynamicNode Target
			{
				get { return _Target; }
			}
		}

		// ================================================
		/// <summary>
		/// Represents a dynamic conversion or cast operation, as in 'x => (type)x'.
		/// <para>The target object must be a dynamic node.</para>
		/// </summary>
		[Serializable]
		public class Convert : DynamicNode, ISerializable, ICloneable, IEquivalent<Convert>
		{
			Type _NewType = null;
			DynamicNode _Target = null;

			private Convert() { }

			/// <summary>
			/// Initializes a new instance.
			/// </summary>
			/// <param name="newType">The new type to convert the target to.</param>
			/// <param name="target">The target node of the operation.</param>
			public Convert(Type newType, DynamicNode target)
			{
				if ((_Target = target) == null) throw new ArgumentNullException("target", "Target cannot be null.");
				if (_Target.IsDisposed) throw new ObjectDisposedException(_Target.ToString());

				if ((_NewType = newType) == null) throw new ArgumentNullException("newType", "Target Type cannot be null.");
			}

			/// <summary>
			/// Invoked when disposing or finalizing this instance.
			/// </summary>
			/// <param name="disposing">True if the object is being disposed, false otherwise.</param>
			/// <param name="disposeHost">True to also dispose the parent instance this one is
			/// hosted by, if any.</param>
			protected override void OnDispose(bool disposing, bool disposeHost)
			{
				if (disposing)
				{
					if (_Target != null)
					{
						if (disposeHost && _Target.IsNodeAncestor(this)) _Target._Host = null;
						_Target.Dispose(disposeHost);
					}
				}
				_Target = null;
				_NewType = null;

				base.OnDispose(disposing, disposeHost);
			}

			/// <summary>
			/// Returns the string representation of this instance.
			/// </summary>
			/// <returns>A string containing the standard representation of this instance.</returns>
			public override string ToString()
			{
				string str = string.Format("({0} {1})",
					_NewType == null ? string.Empty : _NewType.EasyName(),
					_Target == null ? string.Empty : _Target.ToString());

				return IsDisposed ? "disposed::{0}({1})".FormatWith(GetType().EasyName(), str) : str;
			}

			/// <summary>
			/// Call-back method required for custom serialization.
			/// </summary>
			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);

				info.AddExtended("Target", _Target);
				info.AddExtended("NewType", _NewType);
			}

			/// <summary>
			/// Protected initializer required for custom serialization.
			/// </summary>
			protected Convert(SerializationInfo info, StreamingContext context)
				: base(info, context)
			{
				_Target = (DynamicNode)info.GetExtended("Target");
				_NewType = (Type)info.GetExtended("NewType");
			}

			/// <summary>
			/// Returns a new instance that is a copy of the original one.
			/// </summary>
			/// <returns>A new instance that is a copy of the original one.</returns>
			public new Convert Clone()
			{
				var cloned = new Convert();
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
			protected override void OnClone(object cloned)
			{
				base.OnClone(cloned);
				var temp = cloned as Convert;
				if (cloned == null) throw new InvalidCastException(
					"Cloned instance '{0}' is not a valid '{1}' one."
					.FormatWith(cloned.Sketch(), typeof(Convert).EasyName()));

				temp._NewType = _NewType;
				temp._Target = (DynamicNode)(_Target == null ? null : ((ICloneable)_Target).Clone()); // For casting...
			}

			/// <summary>
			/// Returns true if the state of this object can be considered as equivalent to the target
			/// one, based upon any arbitrary criteria implemented in this method.
			/// </summary>
			/// <param name="target">The target instance this one will be tested for equivalence against.</param>
			/// <returns>True if the state of this instance can be considered as equivalent to the
			/// target one, or false otherwise.</returns>
			public bool EquivalentTo(Convert target)
			{
				return OnEquivalentTo(target);
			}

			/// <summary>
			/// Invoked to test equivalence at this point of the inheritance chain.
			/// </summary>
			/// <param name="target">The target this instance will be tested for equivalence against.</param>
			/// <returns>True if at this level on the inheritance chain this instance can be considered
			/// equivalent to the target instance given.</returns>
			protected override bool OnEquivalentTo(object target)
			{
				if (base.OnEquivalentTo(target))
				{
					var temp = target as Convert; if (temp == null) return false;

					if (_NewType != temp._NewType) return false;
					if (!_Target.IsEquivalentTo(temp._Target)) return false;

					return true;
				}
				return false;
			}

			/// <summary>
			/// The new type to convert the target of this operation to.
			/// </summary>
			public Type NewType
			{
				get { return _NewType; }
			}

			/// <summary>
			/// The target node of the operation this instance refers to.
			/// </summary>
			public DynamicNode Target
			{
				get { return _Target; }
			}
		}
	}

	// ==================================================== 
	/// <summary>
	/// Helper class to bind the dynamic operations with its dynamic arguments or derived
	/// instances.
	/// </summary>
	internal class DynamicMetaNode : DynamicMetaObject
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		internal DynamicMetaNode(Expression parameter, BindingRestrictions rest, object value)
			: base(parameter, rest, value)
		{ }

		/// <summary>
		/// Returns the array of underlying objects from a meta objects' array.
		/// </summary>
		static object[] MetaList2List(DynamicMetaObject[] metaObjects)
		{
			if (metaObjects == null) return null;

			var temp = metaObjects.Select(x => x.Value).ToArray();
			return temp;
		}

		/// <summary>
		/// Binds a dynamic get member operation.
		/// </summary>
		public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
		{
			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.GetMember(obj, binder.Name) { Parser = obj.Parser };
			obj.Parser.LastNode = node;

			var par = Expression.Variable(typeof(DynamicNode), "ret");
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign(par, Expression.Constant(node))
				);

			return new DynamicMetaNode(exp, this.Restrictions, node);
		}

		/// <summary>
		/// Binds a dynamic set member operation.
		/// </summary>
		public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
		{
			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.SetMember(obj, binder.Name, value.Value) { Parser = obj.Parser };
			obj.Parser.LastNode = node;

			var par = Expression.Variable(typeof(DynamicNode), "ret");
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign(par, Expression.Constant(node))
				);

			return new DynamicMetaNode(exp, this.Restrictions, node);
		}

		/// <summary>
		/// Binds a dynamic get indexed member operation.
		/// </summary>
		public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
		{
			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.GetIndexed(obj, MetaList2List(indexes)) { Parser = obj.Parser };
			obj.Parser.LastNode = node;

			var par = Expression.Variable(typeof(DynamicNode), "ret");
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign(par, Expression.Constant(node))
				);

			return new DynamicMetaNode(exp, this.Restrictions, node);
		}

		/// <summary>
		/// Binds a dynamic set indexed member operation.
		/// </summary>
		public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
		{
			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.SetIndexed(obj, MetaList2List(indexes), value.Value) { Parser = obj.Parser };
			obj.Parser.LastNode = node;

			var par = Expression.Variable(typeof(DynamicNode), "ret");
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign(par, Expression.Constant(node))
				);

			return new DynamicMetaNode(exp, this.Restrictions, node);
		}

		/// <summary>
		/// Binds a dynamic method invocation operation.
		/// </summary>
		public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
		{
			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.Method(obj, binder.Name, MetaList2List(args)) { Parser = obj.Parser };
			obj.Parser.LastNode = node;

			var par = Expression.Variable(typeof(DynamicNode), "ret");
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign(par, Expression.Constant(node))
				);

			return new DynamicMetaNode(exp, this.Restrictions, node);
		}

		/// <summary>
		/// Binds a dynamic instance invocation operation.
		/// </summary>
		public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
		{
			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.Invoke(obj, MetaList2List(args)) { Parser = obj.Parser };
			obj.Parser.LastNode = node;

			var par = Expression.Variable(typeof(DynamicNode), "ret");
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign(par, Expression.Constant(node))
				);

			return new DynamicMetaNode(exp, this.Restrictions, node);
		}

		/// <summary>
		/// Binds a dynamic binary operation.
		/// </summary>
		public override DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg)
		{
			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.Binary(obj, binder.Operation, arg.Value) { Parser = obj.Parser };
			obj.Parser.LastNode = node;

			var par = Expression.Variable(typeof(DynamicNode), "ret");
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign(par, Expression.Constant(node))
				);

			return new DynamicMetaNode(exp, this.Restrictions, node);
		}

		/// <summary>
		/// Binds a dynamic unary operation.
		/// </summary>
		public override DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder)
		{
			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.Unary(binder.Operation, obj) { Parser = obj.Parser };
			obj.Parser.LastNode = node;

			// If operation is 'IsTrue' or 'IsFalse', we will return false to keep the engine working...
			object ret = node;
			if (binder.Operation == ExpressionType.IsTrue) ret = (object)false;
			if (binder.Operation == ExpressionType.IsFalse) ret = (object)false;

			var par = Expression.Variable(ret.GetType(), "ret"); // the type is now obtained from "ret"
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign(par, Expression.Constant(ret)) // the expression is now obtained from "ret"
				);

			return new DynamicMetaNode(exp, this.Restrictions, node);
		}

		/// <summary>
		/// Binds a dynamic convert or cast operation.
		/// </summary>
		public override DynamicMetaObject BindConvert(ConvertBinder binder)
		{
			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.Convert(binder.ReturnType, obj) { Parser = obj.Parser };
			obj.Parser.LastNode = node;

			// Reducing the object to return if this is an assignment node...
			object ret = obj;
			bool done = false; while (!done)
			{
				if (ret is DynamicNode.SetMember) ret = ((DynamicNode.SetMember)obj).Value;
				else if (ret is DynamicNode.SetIndexed) ret = ((DynamicNode.SetIndexed)obj).Value;
				else done = true;
			}

			// Creating an instance...
			if (binder.ReturnType == typeof(string)) ret = ret.ToString();
			else
			{
				try
				{
					if (TypeEx.IsNullableType(binder.ReturnType)) ret = null; // to avoid cast exceptions
					else ret = Activator.CreateInstance(binder.ReturnType, true); // true to allow non-public ctor as well
				}
				catch { ret = new object(); } // as the last resort scenario
			}

			var par = Expression.Variable(binder.ReturnType, "ret");
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign(par, Expression.Constant(ret, binder.ReturnType)) // specifying binder.ReturnType
				);

			return new DynamicMetaNode(exp, this.Restrictions, node);
		}
	}
}
// ======================================================== 
