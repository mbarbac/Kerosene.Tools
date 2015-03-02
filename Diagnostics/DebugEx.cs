// ======================================================== DebugEx.cs
namespace Kerosene.Tools
{
	using System;
	using System.Diagnostics;

	// ==================================================== 
	/// <summary>
	/// Extends the capabilities of the debug environment.
	/// </summary>
	public static class DebugEx
	{
		/// <summary>
		/// The collection of listeners monitoring the debug output.
		/// </summary>
		public static TraceListenerCollection Listeners
		{
			get { return Debug.Listeners; }
		}

		/// <summary>
		/// Adds the standard console listener if it has not been added yet.
		/// </summary>
		public static void AddConsoleListener()
		{
			if (!IsConsoleListenerAdded()) Listeners.Add(new TextWriterTraceListener(Console.Out));
		}

		/// <summary>
		/// Returns whether the standard console listener has been already added or not.
		/// </summary>
		/// <returns>True if the standard console listener has been added, false otherwise.</returns>
		public static bool IsConsoleListenerAdded()
		{
			foreach (var listener in Listeners)
			{
				if (listener is TextWriterTraceListener)
				{
					var node = listener as TextWriterTraceListener;
					if (object.ReferenceEquals(node.Writer, Console.Out)) return true;
				}
			}
			return false;
		}

		/// <summary>
		/// The number of spaces in an indent.
		/// </summary>
		public static int IndentSize
		{
			get { return Debug.IndentSize; }
			set { Debug.IndentSize = value; }
		}

		/// <summary>
		/// Whether the writer buffer shall be flushed after each write.
		/// </summary>
		public static bool AutoFlush
		{
			get { return Debug.AutoFlush; }
			set { Debug.AutoFlush = value; }
		}

		/// <summary>
		/// The current indent level.
		/// </summary>
		public static int IndentLevel
		{
			get { return Debug.IndentLevel; }
			set { Debug.IndentLevel = value; }
		}

		/// <summary>
		/// Increases the indent level by one.
		/// </summary>
		[Conditional("DEBUG")]
		public static void Indent()
		{
			Debug.Indent();
		}

		/// <summary>
		/// Decreases the indent level by one.
		/// </summary>
		[Conditional("DEBUG")]
		public static void Unindent()
		{
			Debug.Unindent();
		}

		/// <summary>
		/// Writes the given message into the listeners without adding a newline terminator.
		/// <para>Intercepts appropriately any embedded newline characters.</para>
		/// </summary>
		/// <param name="message">The message to write.</param>
		/// <param name="args">The arguments to include into the formatted output.</param>
		[Conditional("DEBUG")]
		public static void Write(string message = null, params object[] args)
		{
			if (message == null) message = string.Empty;
			else if (args != null && args.Length != 0) message = string.Format(message, args);

			var parts = message.Split('\n');
			var temp = parts.Length - 1;

			for (int i = 0; i < parts.Length; i++)
			{
				Debug.Write(parts[i]);
				if (i < temp) Debug.WriteLine(string.Empty);
			}
		}

		/// <summary>
		/// Increases the indent level and then writes the given message into the listeners
		/// without adding a newline terminator.
		/// <para>Intercepts appropriately any embedded newline characters.</para>
		/// </summary>
		/// <param name="message">The message to write.</param>
		/// <param name="args">The arguments to include into the formatted output.</param>
		[Conditional("DEBUG")]
		public static void IndentWrite(string message = null, params object[] args)
		{
			Indent();
			Write(message, args);
		}

		/// <summary>
		/// Writes the given message into the listeners adding a newline terminator.
		/// <para>Intercepts appropriately any embedded newline characters.</para>
		/// </summary>
		/// <param name="message">The message to write.</param>
		/// <param name="args">The arguments to include into the formatted output.</param>
		[Conditional("DEBUG")]
		public static void WriteLine(string message = null, params object[] args)
		{
			Write(message, args);
			Debug.WriteLine(string.Empty);
		}

		/// <summary>
		///Increases the indent level and then writes the given message into the listeners
		///adding a newline terminator.
		/// <para>Intercepts appropriately any embedded newline characters.</para>
		/// </summary>
		/// <param name="message">The message to write.</param>
		/// <param name="args">The arguments to include into the formatted output.</param>
		[Conditional("DEBUG")]
		public static void IndentWriteLine(string message = null, params object[] args)
		{
			Indent();
			WriteLine(message, args);
		}
	}
}
// ======================================================== 
