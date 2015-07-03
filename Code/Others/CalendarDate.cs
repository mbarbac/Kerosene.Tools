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
	/// Represents an arbitrary date in a western calendar format.
	/// <para>This class takes into consideration the Julian and Gregorian variations.</para>
	/// </summary>
	[Serializable]
	public class CalendarDate : ICloneable, ISerializable, IComparable<CalendarDate>, IEquivalent<CalendarDate>
	{
		int _Year = 0;
		int _Month = 0;
		int _Day = 0;

		/// <summary>
		/// Validates that the given year value is valid.
		/// <para>Year '0' is not considered valid; negative values are considered as BC ones.</para>
		/// </summary>
		/// <param name="year">The year value to validate.</param>
		public static void ValidateYear(int year)
		{
			if (year == 0) throw new ArgumentException("Year cannot be cero.");
		}

		/// <summary>
		///  Validates that the given month value is valid.
		/// <para>Valid month values are from 1 to 12 both included.</para>
		/// </summary>
		/// <param name="month">The month value to validate.</param>
		public static void ValidateMonth(int month)
		{
			if (month < 1) throw new ArgumentException("Month '{0}' cannot be less than 1.".FormatWith(month));
			if (month > 12) throw new ArgumentException("Month '{0}' cannot be greater than 12.".FormatWith(month));
		}

		/// <summary>
		/// Validates that the given year, month and day combination represents a valid date or not,
		/// throwing an exception in this case.
		/// <para>Dates from 5.10.1582 to 14.10.1582, both included, do not exist neither in the
		/// Julian or Gregorian calendars because the change from one calendar to the other.</para>
		/// </summary>
		/// <param name="year">The year value.</param>
		/// <param name="month">The month value.</param>
		/// <param name="day">The day value.</param>
		public static void ValidateDate(int year, int month, int day)
		{
			ValidateYear(year);
			ValidateMonth(month);

			if (day < 1) throw new ArgumentException("Day '{0}' cannot be less than 1.".FormatWith(day));

			int max = DaysInMonth(year, month);
			if (day > max) throw new ArgumentException(
				"Day '{0}' is greater than '{1}' (max for month '{2}', year '{3}')."
				.FormatWith(day, max, month, year));

			if (year == 1582 && month == 10 && (day >= 5 && day <= 14)) throw new ArgumentException(
				"Date '{0}/{1}/{2}' does not exist in the Julian or Gregorian calendars."
				.FormatWith(year, month, day));
		}

		/// <summary>
		/// Retursn the number of days in the given month of the given year.
		/// </summary>
		/// <param name="year">The year.</param>
		/// <param name="month">The month.</param>
		/// <returns>The number of days of the given year/month combination.</returns>
		public static int DaysInMonth(int year, int month)
		{
			ValidateYear(year);
			ValidateMonth(month);

			switch (month)
			{
				case 1: // january
				case 3: // march
				case 5: // may
				case 7: // july
				case 8: // august
				case 10: // october:
				case 12: // december
					return 31;

				case 4: // april
				case 6: // june
				case 9: // september
				case 11: // november
					return 30;
			}
			// february
			if (IsLeapYear(year)) return 29;
			return 28;
		}

		/// <summary>
		/// Returns whether the given year is a leap one or not.
		/// <para>Leap years were introduce in 45 BC but, from this moment to year 12 BC a cadence of
		/// three years where used instead of the standard 4 years one because and error made by the
		/// ancient roman astronomers.</para>
		/// </summary>
		/// <param name="year">The year to verify.</param>
		/// <returns>True if the given year is a leap one, false otherwise.</returns>
		public static bool IsLeapYear(int year)
		{
			ValidateYear(year);

			if (year >= 1582) // Gregorian calendar...
			{
				if ((year % 4) == 0) // The general rule...
				{
					if ((year % 400) == 0) return true; // The exception to the 100 rule...
					if ((year % 100) == 0) return false; // The 100 rule...
					return true;
				}
			}
			if (year >= -45) // Julian calendar took effect...
			{
				if (year <= -12 && (year % 3) == 0) return true;// A counting error happened in the ancient Rome...
				if ((year % 4) == 0) return true; // The general rule...
			}
			return false;
		}

		private CalendarDate() { }

		/// <summary>
		/// Initializes a new instance using the parameters given.
		/// </summary>
		/// <param name="year">The year.</param>
		/// <param name="month">The month.</param>
		/// <param name="day">The value.</param>
		public CalendarDate(int year, int month, int day)
		{
			ValidateDate(year, month, day);

			_Year = year;
			_Month = month;
			_Day = day;
		}

		/// <summary>
		/// Initializes a new instance extracting the year, month and day values from the given
		/// DateTime one.
		/// </summary>
		/// <param name="dt">The source DateTime instance.</param>
		public CalendarDate(DateTime dt)
		{
			if (dt == null) throw new ArgumentNullException("dt", "DateTime cannot be null.");
			ValidateDate(dt.Year, dt.Month, dt.Day);

			_Year = dt.Year;
			_Month = dt.Month;
			_Day = dt.Day;
		}

		/// <summary>
		/// Returns the string representation of this instance.
		/// </summary>
		/// <returns>A string containing the string representation of this instance.</returns>
		public override string ToString()
		{
			string s = "{0,0000}-{1,00}-{2,00}".FormatWith(_Year, _Month, _Day);
			return s;
		}

		/// <summary>
		/// Call-back method required for custom serialization.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Year", _Year);
			info.AddValue("Month", _Month);
			info.AddValue("Day", _Day);
		}

		/// <summary>
		/// Protected initializer required for custom serialization.
		/// </summary>
		protected CalendarDate(SerializationInfo info, StreamingContext context)
		{
			_Year = (int)info.GetValue("Year", typeof(int));
			_Month = (int)info.GetValue("Month", typeof(int));
			_Day = (int)info.GetValue("Day", typeof(int));
		}

		/// <summary>
		/// Returns a new instance that is a copy of this one.
		/// </summary>
		/// <returns>A new instance that is a copy of this one.</returns>
		public CalendarDate Clone()
		{
			var cloned = new CalendarDate(); OnClone(cloned);
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
			var temp = cloned as CalendarDate;
			if (temp == null) throw new InvalidCastException(
				"Cloned instance '{0}' is not a valid '{1}' one."
				.FormatWith(cloned.Sketch(), typeof(CalendarDate).EasyName()));

			temp._Year = _Year;
			temp._Month = _Month;
			temp._Day = _Day;
		}

		/// <summary>
		/// Compares the two instances given returning -1 if the left instance is the
		/// smallest one, +1 if the left instance is the bigger one, or 0 if both can be
		/// considered equivalent.
		/// </summary>
		/// <param name="left">The left instance.</param>
		/// <param name="right">The right instance.</param>
		/// <returns>An integer expressing the relative order of the left instance with
		/// respect to the right one.</returns>
		public static int Compare(CalendarDate left, CalendarDate right)
		{
			if (object.ReferenceEquals(left, null)) return object.ReferenceEquals(right, null) ? 0 : -1;
			if (object.ReferenceEquals(right, null)) return object.ReferenceEquals(left, null) ? 0 : +1;

			if (left.Year != right.Year) return (left.Year > right.Year) ? +1 : -1;
			if (left.Month != right.Month) return (left.Month > right.Month) ? +1 : -1;
			if (left.Day != right.Day) return (left.Day > right.Day) ? +1 : -1;

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
		public virtual int CompareTo(CalendarDate other)
		{
			return Compare(this, other);
		}

		/// <summary>
		/// Determines whether this object is exactly the same one as the given one.
		/// </summary>
		public override bool Equals(object obj)
		{
			var temp = obj as CalendarDate;
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
		public static bool operator >(CalendarDate left, CalendarDate right)
		{
			return Compare(left, right) > 0;
		}

		/// <summary></summary>
		public static bool operator <(CalendarDate left, CalendarDate right)
		{
			return Compare(left, right) < 0;
		}

		/// <summary></summary>
		public static bool operator >=(CalendarDate left, CalendarDate right)
		{
			return Compare(left, right) >= 0;
		}

		/// <summary></summary>
		public static bool operator <=(CalendarDate left, CalendarDate right)
		{
			return Compare(left, right) <= 0;
		}

		/// <summary></summary>
		public static bool operator ==(CalendarDate left, CalendarDate right)
		{
			return Compare(left, right) == 0;
		}

		/// <summary></summary>
		public static bool operator !=(CalendarDate left, CalendarDate right)
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
		public bool EquivalentTo(CalendarDate target)
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
			var temp = target as CalendarDate; if (temp == null) return false;

			return Compare(this, temp) == 0;
		}

		/// <summary>
		/// Parsers the given string and creates a new CalendarDate instance.
		/// </summary>
		/// <param name="str">The source string, with the expressed in the universal "yyyy-mm-dd"
		/// format, or the "yyyymmdd" one.</param>
		/// <param name="separators">If not null the characters to use as field separators.</param>
		/// <returns>A new CalendarDate instance.</returns>
		public static CalendarDate Parse(string str, char[] separators = null)
		{
			CalendarDate value = null;
			Exception e = TryParse(out value, str, separators);

			if (e != null) throw e;
			return value;
		}

		/// <summary>
		/// Tries to paser the given string and creates a new CalendarDate instance, returning null in
		/// case of any errors or an exception describing the error.
		/// </summary>
		/// <param name="value">The value where to place the result.</param>
		/// <param name="str">The source string, with the expressed in the universal "yyyy-mm-dd"
		/// format, or the "yyyymmdd" one.</param>
		/// <param name="separators">If not null the characters to use as field separators.</param>
		/// <returns>A new CalendarDate instance.</returns>
		public static Exception TryParse(out CalendarDate value, string str, char[] separators = null)
		{
			value = null;
			int year = 0, month = 0, day = 0;

			str = str.NullIfTrimmedIsEmpty();
			if (str == null) return new ArgumentNullException("str", "String to parse cannot be null.");

			if (separators == null || separators.Length == 0) separators = new char[] { '/', '-', ':', '.' };

			if (str.IndexOfAny(separators) < 0)
			{
				if (str.Length == 8)
				{
					year = int.Parse(str.Substring(0, 4));
					month = int.Parse(str.Substring(4, 2));
					day = int.Parse(str.Substring(6, 2));
				}
				else return new FormatException("Source '{0}' is not a valid time specification.".FormatWith(str));
			}

			else
			{
				string[] args = str.Split(separators);
				if (args.Length < 3) return new ArgumentException("String '{0}' cannot be split in three parts.".FormatWith(str));

				year = int.Parse(args[0]);
				month = int.Parse(args[1]);
				day = int.Parse(args[2]);
			}

			try { value = new CalendarDate(year, month, day); }
			catch (Exception e) { return e; }

			return null;
		}

		/// <summary>
		/// Creates an equivalent DateTime instance based upon this current one.
		/// </summary>
		/// <returns>A new DateTime instance.</returns>
		public DateTime ToDateTime()
		{
			return new DateTime(Year, Month, Day);
		}

		/// <summary>
		/// Creates an equivalent DateTime instance based upon this current one and the given
		/// ClockTime instance.
		/// </summary>
		/// <param name="clock">The additional ClockTime instance to be used.</param>
		/// <returns>A new DateTime instance.</returns>
		public DateTime ToDateTime(ClockTime clock)
		{
			if (clock == null) throw new ArgumentNullException("clock", "ClockTime cannot be null.");
			return new DateTime(Year, Month, Day, clock.Hour, clock.Minute, clock.Second, clock.Millisecond);
		}

		/// <summary>
		/// Converts the given CalendarDate instance into a DateTime equivalent.
		/// </summary>
		/// <param name="calendar">The source CalendarDate instance.</param>
		/// <returns>A new DateTime instance.</returns>
		public static implicit operator DateTime(CalendarDate calendar)
		{
			return calendar.ToDateTime();
		}

		/// <summary>
		/// Converts the given DateTime instance into an equivalent CalendarDate one.
		/// </summary>
		/// <param name="dt">The source DateTime instance.</param>
		/// <returns>A new CalendarDate instance.</returns>
		public static implicit operator CalendarDate(DateTime dt)
		{
			return dt.ToCalendarDate();
		}

		/// <summary>
		/// Adds to this date the given number of days, it being a positive or negative number,
		/// and returns a new instance with the result of the operation.
		/// </summary>
		/// <param name="ndays">The positive or negative number of days to add.</param>
		/// <returns>A new CalendarDate instance.</returns>
		public CalendarDate Add(int ndays)
		{
			if (ndays == 0) return new CalendarDate(this);

			int year = Year;
			int month = Month;
			int day = Day;

			while (ndays < 0) // Case ndays is negative...
			{
				if ((--day) < 1)
				{
					if ((--month) < 1)
					{
						if ((--year) == 0) year = -1;
						month = 12;
					}
					day = CalendarDate.DaysInMonth(year, month);
				}
				if (year == 1582 && month == 10 && day == 14) day = 4;
				ndays++;
			}
			while (ndays > 0) // Case ndays is positive...
			{
				int max = CalendarDate.DaysInMonth(year, month);
				if ((++day) > max)
				{
					if ((++month) > 12)
					{
						if ((++year) == 0) year = 1;
						month = 1;
					}
					day = 1;
				}
				if (year == 1582 && month == 10 && day == 5) day = 15;
				ndays--;
			}
			return new CalendarDate(year, month, day);
		}

		/// <summary>
		/// The year this instance refers to.
		/// </summary>
		public int Year
		{
			get { return _Year; }
		}

		/// <summary>
		/// The month this instance refers to.
		/// </summary>
		public int Month
		{
			get { return _Month; }
		}

		/// <summary>
		/// The day this instance refers to.
		/// </summary>
		public int Day
		{
			get { return _Day; }
		}

		/// <summary>
		/// Creates a new instance set to the current date of the local host.
		/// </summary>
		/// <returns>A new instance set to the current date of the local host.</returns>
		public static CalendarDate Now()
		{
			return new CalendarDate(DateTime.Now);
		}

		/// <summary>
		/// Creates a new instance set to the current UTC date of the local host, expressed as UTC.
		/// </summary>
		/// <returns>A new instance set to the current date of the local host, expressed as UTC.</returns>
		public static CalendarDate UtcNow()
		{
			return new CalendarDate(DateTime.UtcNow);
		}
	}

	// ====================================================
	/// <summary>
	/// Helpers and extensions for working with <see cref="CalendarDate"/> instances.
	/// </summary>
	public static class CalendarDateHelper
	{
		/// <summary>
		/// Creates a CalendarDate instance equivalent to the source one given.
		/// </summary>
		/// <param name="dt">The source instance.</param>
		/// <returns>A new CalendarDate instance.</returns>
		public static CalendarDate ToCalendarDate(this DateTime dt)
		{
			if (dt == null) throw new ArgumentNullException("dt", "DateTime cannot be null.");
			return new CalendarDate(dt);
		}

		/// <summary>
		/// Splits the given DateTime into its calendar and clock parts.
		/// </summary>
		/// <param name="dt">The source DateTime instance.</param>
		/// <returns>A tuple containing the CalendarDate and ClockTime instances buily from the
		/// source DateTime one.</returns>
		public static Tuple<CalendarDate, ClockTime> ToCalendarAndClock(this DateTime dt)
		{
			if (dt == null) throw new ArgumentNullException("dt", "DateTime cannot be null.");

			var cd = dt.ToCalendarDate();
			var ct = dt.ToClockTime();
			return new Tuple<CalendarDate, ClockTime>(cd, ct);
		}
	}
}
