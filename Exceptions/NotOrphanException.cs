// ======================================================== NotOrphanException.cs
namespace Kerosene.Tools
{
	using System;

	// ==================================================== 
	/// <summary>
	/// Represents an attempt of using an object that cannot be considered as orphan when such is
	/// needed.
	/// </summary>
	[Serializable]
	public class NotOrphanException : Exception
	{
		/// <summary>
		/// Default constructor for an empty instance.
		/// </summary>
		public NotOrphanException() { }

		/// <summary>
		/// Initializes a new instance with the given message.
		/// </summary>
		/// <param name="message">An string containing a description of the error.</param>
		public NotOrphanException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance with the given message and a reference to the exception that
		/// can be considered the cause of this one.
		/// </summary>
		/// <param name="message">An string containing a description of the error.</param>
		/// <param name="inner">A reference to the exception that can be considered as the cause
		/// of this one, or null if this information is not available or needed.</param>
		public NotOrphanException(string message, Exception inner) : base(message, inner) { }
	}
}
// ======================================================== 
