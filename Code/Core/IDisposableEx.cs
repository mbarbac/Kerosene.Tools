using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kerosene.Tools
{
	// =====================================================
	/// <summary>
	/// Extends the 'IDisposable' interface.
	/// </summary>
	public interface IDisposableEx : IDisposable
	{
		/// <summary>
		/// Whether this instance has been disposed or not.
		/// </summary>
		bool IsDisposed { get; }
	}
}
