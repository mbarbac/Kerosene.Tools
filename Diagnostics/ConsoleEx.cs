namespace Kerosene.Tools
{
	using System;

	// ====================================================
	/// <summary>
	/// Extends the 'Console' functionality.
	/// </summary>
	public static class ConsoleEx
	{
		/// <summary>
		/// Writes the given message into the debug listeners and, if needed, into the console,
		/// without adding a newline terminator.
		/// </summary>
		/// <param name="message">The message to write.</param>
		/// <param name="args">The arguments to include into the formatted output.</param>
		public static void Write(string message = null, params object[] args)
		{
			if (message == null) message = string.Empty;
			else if (args != null && args.Length != 0) message = string.Format(message, args);
#if DEBUG
			DebugEx.Write(message);
			if (!DebugEx.IsConsoleListenerRegistered)
#endif
				Console.Write(message);
		}

		/// <summary>
		/// Writes the given message into the debug listeners and, if needed, into the console,
		/// adding a newline terminator.
		/// </summary>
		/// <param name="message">The message to write.</param>
		/// <param name="args">The arguments to include into the formatted output.</param>
		public static void WriteLine(string message = null, params object[] args)
		{
			if (message == null) message = string.Empty;
			else if (args != null && args.Length != 0) message = string.Format(message, args);
#if DEBUG
			DebugEx.WriteLine(message);
			if (!DebugEx.IsConsoleListenerRegistered)
#endif
				Console.WriteLine(message);
		}

		/// <summary>
		/// Retrieves the next line of characters from the standard input stream, preceeded by
		/// the given message and arguments to format it.
		/// <para>If the current execution is not in an interactive mode then an empty string
		/// is returned instead.</para>
		/// </summary>
		/// <param name="header">If not null the message to show to the user.</param>
		/// <param name="args">The optional collection of arguments to use in the header message</param>
		/// <returns>A string containing the characters read.</returns>
		public static string ReadLine(string header = null, params object[] args)
		{
			if (header != null) Write(header, args);

			var str = Interactive ? Console.ReadLine() : string.Empty;
			if (!Interactive) WriteLine();

			return str;
		}

		/// <summary>
		/// Whether the current execution can be considered as an interactive one or not.
		/// </summary>
		public static bool Interactive
		{
			get { return _Interactive; }
			set { _Interactive = value; }
		}
		static bool _Interactive = false;

		/// <summary>
		/// Ask the console user whether to execute the program in interactive mode or not, and
		/// sets the <see cref="ConsoleEx.Interactive"/> flag correspondingly.
		/// </summary>
		/// <param name="header">The question presented to the user. If null a default one is
		/// used.</param>
		public static void AskInteractive(string header = null)
		{
			if (header == null) header = "\n=== Press [N] for non-interactive execution... ";

			Interactive = true;
			var str = ReadLine(header);

			if (str.ToUpper() == "N") Interactive = false;
		}
	}
}
