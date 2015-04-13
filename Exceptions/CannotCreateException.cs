// ======================================================== CannotCreateException.cs
namespace Kerosene.Tools
{
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents a failed attempt of creating an arbitrary object.
	/// </summary>
	[Serializable]
	public class CannotCreateException : Exception
	{
		/// <summary>
		/// Default constructor for an empty instance.
		/// </summary>
		public CannotCreateException() { }

		/// <summary>
		/// Initializes a new instance with the given message.
		/// </summary>
		/// <param name="message">An string containing a description of the error.</param>
		public CannotCreateException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance with the given message and a reference to the exception that
		/// can be considered the cause of this one.
		/// </summary>
		/// <param name="message">An string containing a description of the error.</param>
		/// <param name="inner">A reference to the exception that can be considered as the cause
		/// of this one, or null if this information is not available or needed.</param>
		public CannotCreateException(string message, Exception inner) : base(message, inner) { }
	}
}
// ======================================================== 
