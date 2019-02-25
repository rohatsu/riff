// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract, Serializable]
    [TypeConverter(typeof(RFDateConverter))]
    public struct RFDate : IComparable, IComparable<RFDate>, IEquatable<RFDate>
    {
        [IgnoreDataMember]
        public DateTime Date
        {
            get
            {
                return ToDateTime().Date;
            }
        }

        [IgnoreDataMember]
        public int Day { get { return YMD % 100; } }

        [IgnoreDataMember]
        public int Month { get { return (YMD / 100) % 100; } }

        [IgnoreDataMember]
        public int Quarter { get { return (Month + 2) / 3; } }

        [IgnoreDataMember]
        public int Year { get { return YMD / 10000; } }

        public static readonly RFDate NullDate = new RFDate(0);

        [DataMember]
        private readonly int YMD;

        public RFDate(DateTime d)
        {
            if (d == DateTime.MinValue || d == DateTime.MaxValue)
            {
                YMD = 0;
            }
            else
            {
                YMD = d.Year * 10000 + d.Month * 100 + d.Day;
            }
        }

        public RFDate(int ymd)
        {
            YMD = ymd;
        }

        public RFDate(int year, int month, int day)
        {
            YMD = year * 10000 + month * 100 + day;
        }

        public static RFDate CalendarQuarterEnd(int year, int quarter)
        {
            return new RFDate(year, quarter * 3, 1).LastCalendarDayOfTheMonth();
        }

        public static int Compare(RFDate left, RFDate right)
        {
            return left.YMD.CompareTo(right.YMD);
        }

        public static void DefaultToLatestMonthEnd(ref RFDate? valueDate)
        {
            if (!valueDate.HasValue)
            {
                valueDate = RFDate.Today().FirstDayOfTheMonth().OffsetWeekdays(-1);
            }
            else
            {
                valueDate = valueDate.Value.LastWorkdayOfTheMonth();
            }
        }

        public static void DefaultToLatestQuarterEnd(ref RFDate? valueDate)
        {
            if (!valueDate.HasValue)
            {
                valueDate = RFDate.Today().QuarterStart().OffsetWeekdays(-1);
            }
            else if (!valueDate.Value.IsWorkdayQuarterEnd())
            {
                valueDate = valueDate.Value.QuarterStart().OffsetWeekdays(-1);
            }
        }

        public static RFDate EndMonthYear(int year, int month)
        {
            return StartMonthYear(year, month).LastWorkdayOfTheMonth();
        }

        public static implicit operator DateTime(RFDate d)
        {
            return new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Local);
        }

        public static implicit operator RFDate(DateTime d)
        {
            return new RFDate(d);
        }

        public static bool operator !=(RFDate left, RFDate right)
        {
            return (left.YMD != right.YMD);
        }

        public static bool operator <(RFDate left, RFDate right)
        {
            return left.YMD < right.YMD;
        }

        public static bool operator <=(RFDate left, RFDate right)
        {
            return left.YMD <= right.YMD;
        }

        public static bool operator ==(RFDate left, RFDate right)
        {
            return left.YMD == right.YMD;
        }

        public static bool operator >(RFDate left, RFDate right)
        {
            return left.YMD > right.YMD;
        }

        public static bool operator >=(RFDate left, RFDate right)
        {
            return left.YMD >= right.YMD;
        }

        public static RFDate Parse(string str, string format = null)
        {
            if (!string.IsNullOrWhiteSpace(str))
            {
                if (string.IsNullOrWhiteSpace(format))
                {
                    // yyyy-mm-dd
                    DateTime dt = DateTime.MinValue;
                    var shortStr = str;
                    if (str.Length >= "Tue Nov 03 2015 17:35:28 GMT+0800".Length && str[3] == ' ' && str[7] == ' ')
                    {
                        // try full TZ format
                        if (DateTime.TryParseExact(shortStr.Substring(4, 11), "MMM dd yyyy", null, System.Globalization.DateTimeStyles.AssumeLocal, out dt))
                        {
                            return new RFDate(dt);
                        }
                    }
                    if (str.Length > 10)
                    {
                        shortStr = str.Substring(0, 10); // clear time afterwards
                    }
                    if (DateTime.TryParseExact(shortStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeLocal, out dt))
                    {
                        return new RFDate(dt);
                    }
                    if (DateTime.TryParseExact(shortStr, "yyyy/MM/dd", null, System.Globalization.DateTimeStyles.AssumeLocal, out dt))
                    {
                        return new RFDate(dt);
                    }
                    if (DateTime.TryParseExact(shortStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.AssumeLocal, out dt))
                    {
                        return new RFDate(dt);
                    }
                    if (DateTime.TryParseExact(shortStr, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.AssumeLocal, out dt))
                    {
                        return new RFDate(dt);
                    }
                    if (DateTime.TryParseExact(shortStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.AssumeLocal, out dt))
                    {
                        return new RFDate(dt);
                    }
                    return new RFDate(DateTime.Parse(str)); // fallback
                }
                else
                {
                    return new RFDate(DateTime.ParseExact(str, format, null));
                }
            }
            return RFDate.NullDate;
        }

        public static RFDate QuarterStart(int year, int quarter)
        {
            return new RFDate(year, quarter * 3 - 2, 1);
        }

        public static List<RFDate> Range(RFDate startDate, RFDate endDate, Func<RFDate, bool> filterFunc)
        {
            var result = new List<RFDate>();
            while (startDate <= endDate)
            {
                if (filterFunc(startDate))
                {
                    result.Add(startDate);
                }
                startDate = startDate.OffsetDays(1);
            }
            return result;
        }

        public static RFDate StartMonthYear(int year, int month)
        {
            return new RFDate(year, month, 1);
        }

        /// <summary>
        /// Avoid using this in batch processing and use Context.Today instead
        /// </summary>
        public static RFDate Today()
        {
            return new RFDate(DateTime.Today);
        }

        public static RFDate WorkdayQuarterEnd(int year, int quarter)
        {
            return new RFDate(year, quarter * 3, 1).LastWorkdayOfTheMonth();
        }

        public RFDate CalendarQuarterEnd()
        {
            return CalendarQuarterEnd(Year, Quarter);
        }

        public int CompareTo(object obj)
        {
            if (obj is RFDate)
            {
                return YMD.CompareTo(((RFDate)obj).YMD);
            }
            return -1;
        }

        public int CompareTo(RFDate other)
        {
            return YMD.CompareTo(other.YMD);
        }

        public override bool Equals(object obj)
        {
            if (obj is RFDate)
            {
                return this.YMD == ((RFDate)obj).YMD;
            }
            return false;
        }

        public bool Equals(RFDate other)
        {
            return (YMD == other.YMD);
        }

        public RFDate FirstDayOfNextMonth()
        {
            return new RFDate(new RFDate(Year, Month, 1).Date.AddMonths(1));
        }

        public RFDate FirstDayOfTheMonth()
        {
            return new RFDate(Year, Month, 1);
        }

        public override int GetHashCode()
        {
            return YMD.GetHashCode();
        }

        public bool IsCalendarMonthEnd()
        {
            return this.YMD == this.LastCalendarDayOfTheMonth().YMD;
        }

        public bool IsCalendarQuarterEnd()
        {
            return IsCalendarMonthEnd() && Month % 3 == 0;
        }

        public bool IsValid()
        {
            return YMD > 19000000 && YMD < 21000000;
        }

        public bool IsWeekday()
        {
            var dt = ToDateTime();
            return (dt.DayOfWeek != DayOfWeek.Saturday && dt.DayOfWeek != DayOfWeek.Sunday);
        }

        public bool IsWorkdayMonthEnd()
        {
            return this.YMD == this.LastWorkdayOfTheMonth().YMD;
        }

        public bool IsWorkdayQuarterEnd()
        {
            return IsWorkdayMonthEnd() && Month % 3 == 0;
        }

        public RFDate LastCalendarDayOfTheMonth()
        {
            return new RFDate(new RFDate(Year, Month, 1).Date.AddMonths(1).AddDays(-1));
        }

        public RFDate LastWorkdayOfTheMonth()
        {
            return new RFDate(new RFDate(Year, Month, 1).Date.AddMonths(1)).PreviousWeekday();
        }

        public RFDate LatestWeekday()
        {
            var dt = (DateTime)this;
            while (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday)
            {
                dt = dt.AddDays(-1);
            }
            return new RFDate(dt);
        }

        public RFDate MonthYear()
        {
            return new RFDate(Year, Month, 1);
        }

        public RFDate NearestWeekday()
        {
            var dt = (DateTime)this;
            while (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday)
            {
                dt = dt.AddDays(1);
            }
            return new RFDate(dt);
        }

        public RFDate OffsetDays(int numDays)
        {
            if (numDays != 0)
            {
                var dt = (DateTime)this;
                bool isBackward = numDays < 0;
                numDays = Math.Abs(numDays);
                while (numDays > 0)
                {
                    dt = dt.AddDays(isBackward ? -1 : 1);
                    numDays--;
                }
                return new RFDate(dt);
            }
            return this;
        }

        public RFDate OffsetWeekdays(int numDays)
        {
            if (numDays != 0)
            {
                var dt = (DateTime)this;
                bool isBackward = numDays < 0;
                numDays = Math.Abs(numDays);
                while (numDays > 0)
                {
                    do
                    {
                        dt = dt.AddDays(isBackward ? -1 : 1);
                    } while (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday/* || (dt.Month == 1 && dt.Day == 1)*/); // also roll over Jan 1st
                    numDays--;
                }
                return new RFDate(dt);
            }
            return this;
        }

        public RFDate PreviousWeekday()
        {
            return OffsetWeekdays(-1);
        }

        public RFDate QuarterStart()
        {
            return QuarterStart(Year, Quarter);
        }

        public DateTime ToDateTime()
        {
            if (YMD == 0)
            {
                return DateTime.MinValue;
            }
            return new DateTime(Year, Month, Day, 0, 0, 0, DateTimeKind.Local);
        }

        public string ToJavascript()
        {
            return ToDateTime().ToString("yyyy/MM/dd");
        }

        public double ToJavascriptMilliseconds()
        {
            return new DateTime(Year, Month, Day, 0, 0, 0, DateTimeKind.Utc).Subtract(
                new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                ).TotalMilliseconds;
        }

        public string ToJavascriptMonth()
        {
            return ToDateTime().ToString("yyyy/MM");
        }

        public string ToString(string format)
        {
            return ToDateTime().ToString(format);
        }

        public override string ToString()
        {
            return YMD.ToString();
        }

        public DateTimeOffset ToUTC()
        {
            if (YMD == 0)
            {
                return DateTime.MinValue;
            }
            return new DateTimeOffset(Year, Month, Day, 0, 0, 0, new TimeSpan());
        }

        public int ToYMD()
        {
            return YMD;
        }

        public RFDate WorkdayQuarterEnd()
        {
            return WorkdayQuarterEnd(Year, Quarter);
        }
    }
}
