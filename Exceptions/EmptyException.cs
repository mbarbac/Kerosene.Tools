﻿using System;

namespace Kerosene.Tools
{
	// ====================================================
	/// <summary>
	/// Represents an attempt of using an object that can be considered as empty when such is not
	/// allowed.
	/// </summary>
	[Serializable]
	public class EmptyException : Exception
	{
		/// <summary>
		/// Default constructor for an empty instance.
		/// </summary>
		public EmptyException() { }

		/// <summary>
		/// Initializes a new instance with the given message.
		/// </summary>
		/// <param name="message">An string containing a description of the error.</param>
		public EmptyException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance with the given message and a reference to the exception that
		/// can be considered the cause of this one.
		/// </summary>
		/// <param name="message">An string containing a description of the error.</param>
		/// <param name="inner">A reference to the exception that can be considered as the cause
		/// of this one, or null if this information is not available or needed.</param>
		public EmptyException(string message, Exception inner) : base(message, inner) { }
	}
}
