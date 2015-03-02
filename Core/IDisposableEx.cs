// ======================================================== IDisposableEx.cs
namespace Kerosene.Tools
{
	using System;

	// ==================================================== 
	/// <summary>
	/// Extends the <see cref="IDisposable"/> interface.
	/// </summary>
	public interface IDisposableEx : IDisposable
	{
		/// <summary>
		/// Whether this instance has been disposed or not.
		/// </summary>
		bool IsDisposed { get; }
	}
}
// ======================================================== 
