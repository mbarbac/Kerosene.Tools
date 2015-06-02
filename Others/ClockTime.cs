using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Kerosene.Tools
{
	// =====================================================
	/// <summary>
	/// Represents a given moment in a day in a 24-hours clock format.
	/// </summary>
	[Serializable]
	public class ClockTime : ICloneable, ISerializable, IComparable<ClockTime>, IEquivalent<ClockTime>
	{
		int _Hour = 0;
		int _Minute = 0;
		int _Second = 0;
		int _Millisecond = 0;

		/// <summary>
		/// Validates that the given hour value is valid.
		/// <para>Valid values are from 0 to 23.</para>
		/// </summary>
		/// <param name="hour">The hour value.</param>
		public static void ValidateHour(int hour)
		{
			if (hour < 0) throw new ArgumentException("Hour '{0}' cannot be less than 0.".FormatWith(hour));
			if (hour > 23) throw new ArgumentException("Hour '{0}' cannot be greater than 23.".FormatWith(hour));
		}

		/// <summary>
		/// Validates that the given minute value is valid.
		/// <para>Valid values are from 0 to 59.</para>
		/// </summary>
		/// <param name="minute">The minute value.</param>
		public static void ValidateMinute(int minute)
		{
			if (minute < 0) throw new ArgumentException("Minute '{0}' cannot be less than 0.".FormatWith(minute));
			if (minute > 59) throw new ArgumentException("Minute '{0}' cannot be greater than 59.".FormatWith(minute));
		}

		/// <summary>
		/// Validates that the given value for seconds is valid.
		/// <para>Valid values are from 0 to 59.</para>
		/// </summary>
		/// <param name="second">The seconds value.</param>
		public static void ValidateSecond(int second)
		{
			if (second < 0) throw new ArgumentException("Second '{0}' cannot be less than 0.".FormatWith(second));
			if (second > 59) throw new ArgumentException("Second '{0}' cannot be greater than 59.".FormatWith(second));
		}

		/// <summary>
		/// Validates that the given value for milliseconds is valid.
		/// <para>Valid values are from 0 to 999.</para>
		/// </summary>
		/// <param name="millisecond">The milliseconds value.</param>
		public static void ValidateMillisecond(int millisecond)
		{
			if (millisecond < 0) throw new ArgumentException("Millisecond '{0}' cannot be less than 0.".FormatWith(millisecond));
			if (millisecond > 999) throw new ArgumentException("Millisecond '{0}' cannot be greater than 999.".FormatWith(millisecond));
		}

		private ClockTime() { }

		/// <summary>
		/// Initializes a new instance using the parameters given.
		/// </summary>
		/// <param name="hour">The hour.</param>
		/// <param name="minute">The minute.</param>
		/// <param name="second">The second.</param>
		/// <param name="millisecond">The millisecond.</param>
		public ClockTime(int hour, int minute, int second, int millisecond = 0)
		{
			ValidateHour(hour); _Hour = hour;
			ValidateMinute(minute); _Minute = minute;
			ValidateSecond(second); _Second = second;
			ValidateMillisecond(millisecond); _Millisecond = millisecond;
		}

		/// <summary>
		/// Inializes a new instance with the hour, minute, second and millisecond values obtained
		/// from the given DateTime instance.
		/// </summary>
		/// <param name="dt">The source DateTime instance.</param>
		public ClockTime(DateTime dt)
		{
			if (dt == null) throw new ArgumentNullException("dt", "DateTime cannot be null.");

			ValidateHour(dt.Hour); _Hour = dt.Hour;
			ValidateMinute(dt.Minute); _Minute = dt.Minute;
			ValidateSecond(dt.Second); _Second = dt.Second;
			ValidateMillisecond(dt.Millisecond); _Millisecond = dt.Millisecond;
		}

		/// <summary>
		/// Inializes a new instance with the hour, minute, second and millisecond values obtained
		/// from the given TimeSpan instance.
		/// </summary>
		/// <param name="ts">The source TimeSpan instance.</param>
		public ClockTime(TimeSpan ts)
		{
			if (ts == null) throw new ArgumentNullException("ts", "TimeSpan cannot be null.");

			ValidateHour(ts.Hours); _Hour = ts.Hours;
			ValidateMinute(ts.Minutes); _Minute = ts.Minutes;
			ValidateSecond(ts.Seconds); _Second = ts.Seconds;
			ValidateMillisecond(ts.Milliseconds); _Millisecond = ts.Milliseconds;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// <para>Second and Milliseconds are only included if needed.</para>
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			string s = string.Format("{0,00}:{1,00}:{2,00}", Hour, Minute, Second);
			if (Millisecond != 0) s += string.Format(".{0,000}", Millisecond);
			return s;
		}

		/// <summary>
		/// Call-back method required for custom serialization.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Hour", _Hour);
			info.AddValue("Minute", _Minute);
			info.AddValue("Second", _Second);
			info.AddValue("Millisecond", _Millisecond);
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected ClockTime(SerializationInfo info, StreamingContext context)
		{
			_Hour = (int)info.GetValue("Hour", typeof(int));
			_Minute = (int)info.GetValue("Minute", typeof(int));
			_Second = (int)info.GetValue("Second", typeof(int));
			_Millisecond = (int)info.GetValue("Millisecond", typeof(int));
		}

		/// <summary>
		/// Returns a new instance that is a copy of this one.
		/// </summary>
		/// <returns>A new instance that is a copy of this one.</returns>
		public ClockTime Clone()
		{
			var cloned = new ClockTime(); OnClone(cloned);
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
			var temp = cloned as ClockTime;
			if (temp == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(ClockTime).EasyName()));

			temp._Hour = _Hour;
			temp._Minute = _Minute;
			temp._Second = _Second;
			temp._Millisecond = _Millisecond;
		}

		/// <summary>
		/// Compares the two instances given and returns -1 if the left instance is the
		/// smallest one, +1 if the left instance is the bigger one, or 0 if both can be
		/// considered equivalent.
		/// </summary>
		/// <param name="left">The left instance.</param>
		/// <param name="right">The right instance.</param>
		/// <returns>An integer expressing the relative order of the left instance with
		/// respect to the right one.</returns>
		public static int Compare(ClockTime left, ClockTime right)
		{
			if (object.ReferenceEquals(left, null)) return object.ReferenceEquals(right, null) ? 0 : -1;
			if (object.ReferenceEquals(right, null)) return object.ReferenceEquals(left, null) ? 0 : +1;

			if (left.Hour != right.Hour) return (left.Hour > right.Hour) ? +1 : -1;
			if (left.Minute != right.Minute) return (left.Minute > right.Minute) ? +1 : -1;
			if (left.Second != right.Second) return (left.Second > right.Second) ? +1 : -1;
			if (left.Millisecond != right.Millisecond) return (left.Millisecond > right.Millisecond) ? +1 : -1;

			return 0;
		}

		/// <summary>
		/// Compares this instance against the other one and returns -1 if this instance
		/// is the smallest one, +1 if it is the bigger one, or 0 if both can be considered
		/// equivalent.
		/// </summary>
		/// <param name="other">The other instance to compare this one against.</param>
		/// <returns>An integer expressing the relative order of this instance with respect
		/// to the other one.</returns>
		public virtual int CompareTo(ClockTime other)
		{
			return Compare(this, other);
		}

		/// <summary>
		/// Determines whether this object is exactly the same one as the given one.
		/// </summary>
		public override bool Equals(object obj)
		{
			var temp = obj as ClockTime;
			return temp == null ? false : (Compare(this, temp) == 0);
		}

		/// <summary>
		/// Serves as the hash function for this type.
		/// </summary>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary></summary>
		public static bool operator >(ClockTime left, ClockTime right)
		{
			return Compare(left, right) > 0;
		}

		/// <summary></summary>
		public static bool operator <(ClockTime left, ClockTime right)
		{
			return Compare(left, right) < 0;
		}

		/// <summary></summary>
		public static bool operator >=(ClockTime left, ClockTime right)
		{
			return Compare(left, right) >= 0;
		}

		/// <summary></summary>
		public static bool operator <=(ClockTime left, ClockTime right)
		{
			return Compare(left, right) <= 0;
		}

		/// <summary></summary>
		public static bool operator ==(ClockTime left, ClockTime right)
		{
			return Compare(left, right) == 0;
		}

		/// <summary></summary>
		public static bool operator !=(ClockTime left, ClockTime right)
		{
			return Compare(left, right) != 0;
		}

		/// <summary>
		/// Returns true if the state of this instance can be considered as equivalent to the
		/// target object given, or false otherwise.
		/// </summary>
		/// <param name="target">The target object to test for equivalence against.</param>
		/// <returns>True if the state of this instance can be considered as equivalent to the
		/// target object given, or false otherwise</returns>
		public bool EquivalentTo(ClockTime target)
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
			var temp = target as ClockTime; if (temp == null) return false;

			return Compare(this, temp) == 0;
		}

		/// <summary>
		/// Parsers the given string and creates a new ClockTime instance.
		/// </summary>
		/// <param name="str">The source string, with the expressed in the universal "hh:mm:ss.nnn"
		/// format, or the "hh", "hhmm", "hhmmss" and "hhmmssnnn" ones.</param>
		/// <param name="separators">If not null the characters to use as field separators.</param>
		/// <returns>A new ClockTime instance.</returns>
		public static ClockTime Parse(string str, char[] separators = null)
		{
			ClockTime value = null;
			Exception e = TryParse(out value, str, separators);

			if (e != null) throw e;
			return value;
		}

		/// <summary>
		/// Tries to parse the given string and creates a new ClockTime instance, returning null in
		/// case of any errors or an exception describing the error.
		/// </summary>
		/// <param name="value">The value where to place the result.</param>
		/// <param name="str">The source string, with the expressed in the universal "hh:mm:ss.nnn"
		/// format, or the "hh", "hhmm", "hhmmss" and "hhmmssnnn" ones.</param>
		/// <param name="separators">If not null the characters to use as field separators.</param>
		/// <returns>A new ClockTime instance.</returns>
		public static Exception TryParse(out ClockTime value, string str, char[] separators = null)
		{
			value = null;
			int hour = 0, minute = 0, second = 0, millisecond = 0;

			str = str.NullIfTrimmedIsEmpty();
			if (str == null) return new ArgumentNullException("str", "String to parse cannot be null.");

			if (separators == null || separators.Length == 0) separators = new char[] { ':', '.' };

			if (str.IndexOfAny(separators) < 0)
			{
				if (str.Length == 2)
				{
					hour = int.Parse(str);
				}
				else if (str.Length == 4)
				{
					hour = int.Parse(str.Substring(0, 2));
					minute = int.Parse(str.Substring(2, 2));
				}
				else if (str.Length == 6)
				{
					hour = int.Parse(str.Substring(0, 2));
					minute = int.Parse(str.Substring(2, 2));
					second = int.Parse(str.Substring(4, 2));
				}
				else if (str.Length == 9)
				{
					hour = int.Parse(str.Substring(0, 2));
					minute = int.Parse(str.Substring(2, 2));
					second = int.Parse(str.Substring(4, 2));
					millisecond = int.Parse(str.Substring(6, 3));
				}
				else return new FormatException("Source '{0}' is not a valid time specification.".FormatWith(str));
			}

			else
			{
				string[] args = str.Split(separators);
				if (args.Length < 2) return new ArgumentException("String '{0}' cannot be split in at least two parts.".FormatWith(str));

				hour = int.Parse(args[0]);
				minute = int.Parse(args[1]);
				if (args.Length > 2) second = int.Parse(args[2]);
				if (args.Length > 3) millisecond = int.Parse(args[3]);
			}

			try { value = new ClockTime(hour, minute, second, millisecond); }
			catch (Exception e) { return e; }

			return null;
		}

		/// <summary>
		/// Creates an equivalent DateTime instance based upon this current one.
		/// </summary>
		public TimeSpan ToTimeSpan()
		{
			return new TimeSpan(0, Hour, Minute, Second, Millisecond);
		}

		/// <summary>
		/// Converts the given ClockTime instance into a TimeSpan equivalent.
		/// </summary>
		/// <param name="time">The source ClockTime instance.</param>
		/// <returns>A new TimeSpan instance.</returns>
		public static implicit operator TimeSpan(ClockTime time)
		{
			return time.ToTimeSpan();
		}

		/// <summary>
		/// Converts the given TimeSpan instance into a ClockTime equivalent.
		/// </summary>
		/// <param name="span">The source TimeSpan.</param>
		/// <returns>A new ClockTime instance.</returns>
		public static implicit operator ClockTime(TimeSpan span)
		{
			return span.ToClockTime();
		}

		/// <summary>
		/// Adds to this instance the parameters given, they being possitive or negative numbers,
		/// and returns a new ClockTime instance with the result.
		/// </summary>
		/// <param name="ndays">An out parameter to hold the number of days of overflow, if any.</param>
		/// <param name="hours">The number of hours to add.</param>
		/// <param name="minutes">The number of minutes to add.</param>
		/// <param name="seconds">The number of seconds to add.</param>
		/// <param name="milliseconds">The number of milliseconds to add.</param>
		/// <returns>A new ClockTime instance.</returns>
		public ClockTime Add(out int ndays, int hours, int minutes = 0, int seconds = 0, int milliseconds = 0)
		{
			ndays = 0;

			milliseconds += Millisecond;
			if (milliseconds > 999) { seconds += milliseconds / 1000; milliseconds = milliseconds % 1000; }
			if (milliseconds < 0) { seconds += milliseconds / 1000 - 1; milliseconds = 1000 + milliseconds % 1000; }

			seconds += Second;
			if (seconds > 59) { minutes += seconds / 60; seconds = seconds % 60; }
			if (seconds < 0) { minutes += seconds / 60 - 1; seconds = 60 + seconds % 60; }

			minutes += Minute;
			if (minutes > 59) { hours += minutes / 60; minutes = minutes % 60; }
			if (minutes < 0) { hours += minutes / 60 - 1; minutes = 60 + minutes % 60; }

			hours += Hour;
			if (hours > 23) { ndays = hours / 24; hours = hours % 24; }
			if (hours < 0) { ndays = hours / 24 - 1; hours = 24 + hours % 24; }

			return new ClockTime(hours, minutes, seconds, milliseconds);
		}

		/// <summary>
		/// Adds to this instance the parameters given, they being possitive or negative numbers,
		/// and returns a new ClockTime instance with the result. Any possible overflow is not
		/// taken into consideration.
		/// </summary>
		/// <param name="hours">The number of hours to add.</param>
		/// <param name="minutes">The number of minutes to add.</param>
		/// <param name="seconds">The number of seconds to add.</param>
		/// <param name="milliseconds">The number of milliseconds to add.</param>
		/// <returns>A new ClockTime instance.</returns>
		public ClockTime Add(int hours, int minutes = 0, int seconds = 0, int milliseconds = 0)
		{
			int ndays;
			return Add(out ndays, hours, minutes, seconds, milliseconds);
		}

		/// <summary>
		/// Creates a new instance set to the current time of the local host.
		/// </summary>
		/// <returns>A new instance set to the current time of the local host.</returns>
		public static ClockTime Now()
		{
			return new ClockTime(DateTime.Now);
		}

		/// <summary>
		/// Creates a new instance set to the current UTC time of the local host, expressed as UTC.
		/// </summary>
		/// <returns>A new instance set to the current time of the local host, expressed as UTC.</returns>
		public static ClockTime UtcNow()
		{
			return new ClockTime(DateTime.UtcNow);
		}

		/// <summary>
		/// The hour this instance refers to.
		/// </summary>
		public int Hour
		{
			get { return _Hour; }
		}

		/// <summary>
		/// The minute this instance refers to.
		/// </summary>
		public int Minute
		{
			get { return _Minute; }
		}

		/// <summary>
		/// The second this instance refers to.
		/// </summary>
		public int Second
		{
			get { return _Second; }
		}

		/// <summary>
		/// The millisecond this instance refers to.
		/// </summary>
		public int Millisecond
		{
			get { return _Millisecond; }
		}
	}

	// ====================================================
	/// <summary>
	/// Helpers and extensions for <see cref="ClockTime"/> objects.
	/// </summary>
	public static class ClockTimeHelper
	{
		/// <summary>
		/// Creates a ClockTime instance equivalent to the source one given.
		/// </summary>
		/// <param name="dt">The source instance.</param>
		/// <returns>A new ClockTime instance.</returns>
		public static ClockTime ToClockTime(this DateTime dt)
		{
			if (dt == null) throw new ArgumentNullException("dt", "DateTime cannot be null.");
			return new ClockTime(dt);
		}

		/// <summary>
		/// Creates a ClockTime instance equivalent to the source one given.
		/// </summary>
		/// <param name="ts">The source instance.</param>
		/// <returns>A new ClockTime instance.</returns>
		public static ClockTime ToClockTime(this TimeSpan ts)
		{
			if (ts == null) throw new ArgumentNullException("ts", "TimeSpan cannot be null.");
			return new ClockTime(ts);
		}
	}
}
