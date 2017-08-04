// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFCompositeRange : RFSchedulerRange
    {
        [DataMember]
        public List<RFSchedulerRange> AllowRanges { get; set; }

        [DataMember]
        public List<RFSchedulerRange> DenyRanges { get; set; }

        [DataMember]
        public List<RFSchedulerRange> ExplicitRanges { get; set; }

        public RFCompositeRange(string timeZone = null) : base(timeZone)
        {
            AllowRanges = new List<RFSchedulerRange>();
            DenyRanges = new List<RFSchedulerRange>();
            ExplicitRanges = new List<RFSchedulerRange>();
        }

        public override string ToString()
        {
            var sb = new List<string>();
            if (AllowRanges.Any())
            {
                sb.Add(String.Join(" & ", AllowRanges));
            }
            if (DenyRanges.Any())
            {
                sb.Add(String.Format("Except {0}", String.Join(" & ", DenyRanges)));
            }
            if (ExplicitRanges.Any())
            {
                sb.Add(String.Format("Only {0}", String.Join(" & ", ExplicitRanges)));
            }
            return string.Join(", ", sb);
        }

        protected override bool InternalInRange(RFInterval interval)
        {
            var isAllowed = (AllowRanges.Count == 0 || AllowRanges.Any(r => r.InRange(interval)));
            var isDenied = (DenyRanges.Count > 0 && DenyRanges.Any(r => r.InRange(interval)));
            var isExplicit = (ExplicitRanges.Count == 0 || ExplicitRanges.All(r => r.InRange(interval)));
            return isAllowed && !isDenied && isExplicit;
        }
    }

    [DataContract]
    public class RFDailyWindow : RFSchedulerRange
    {
        [DataMember]
        protected TimeSpan WindowEnd { get; set; }

        [DataMember]
        protected TimeSpan WindowStart { get; set; }

        public RFDailyWindow(TimeSpan windowStart, TimeSpan windowEnd, string timeZone = null) : base(timeZone)
        {
            WindowStart = windowStart;
            WindowEnd = windowEnd;
        }

        public override string ToString()
        {
            return String.Format("{0}-{1}", WindowStart.ToString(@"hh\:mm"), WindowEnd.ToString(@"hh\:mm"));
        }

        protected override bool InternalInRange(RFInterval interval)
        {
            var refDate = interval.IntervalEnd.Date;
            var windowStart = refDate;
            var windowEnd = refDate;
            var window = new RFInterval(windowStart.Add(WindowStart), windowEnd.Add(WindowEnd));
            return window.Includes(interval.IntervalStart) && window.Includes(interval.IntervalEnd);
        }
    }

    [DataContract]
    public class RFInvertedRange : RFSchedulerRange
    {
        [DataMember]
        protected RFSchedulerRange Range { get; set; }

        public RFInvertedRange(RFSchedulerRange range) : base(null)
        {
            Range = range;
        }

        public override string ToString()
        {
            return String.Format("Outside {0}", Range);
        }

        protected override bool InternalInRange(RFInterval interval)
        {
            return (!Range.InRange(interval));
        }
    }

    [DataContract]
    public class RFMonthEndWindow : RFMonthlyWindow
    {
        public RFMonthEndWindow(string timeZone) : base(timeZone)
        {
        }

        protected override bool InternalInRange(RFInterval interval)
        {
            var today = new RFDate(interval.IntervalEnd.Date);
            if (CalendarDay.HasValue && today.OffsetDays(CalendarDay.Value - 1) != today.LastCalendarDayOfTheMonth())
            {
                return false;
            }
            if (WeekDay.HasValue && today.OffsetWeekdays(WeekDay.Value - 1) != today.LastWorkdayOfTheMonth())
            {
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            if (CalendarDay.HasValue)
            {
                return string.Format("{0}{1} last day of the month", CalendarDay.Value, RFStringHelpers.OrdinalSuffix(CalendarDay.Value));
            }
            if (WeekDay.HasValue)
            {
                return string.Format("{0}{1} last weekday of the month", WeekDay.Value, RFStringHelpers.OrdinalSuffix(WeekDay.Value));
            }
            return "MonthEnd (?)";
        }
    }

    [DataContract]
    public class RFMonthlyWindow : RFSchedulerRange
    {
        [DataMember]
        protected int? CalendarDay { get; set; }

        [DataMember]
        protected int? WeekDay { get; set; }

        public RFMonthlyWindow(string timeZone) : base(timeZone)
        {
        }

        public static RFMonthlyWindow NthCalendarDay(int n)
        {
            return new RFMonthlyWindow(null) { CalendarDay = n };
        }

        public static RFMonthlyWindow NthWeekDay(int n)
        {
            return new RFMonthlyWindow(null) { WeekDay = n };
        }

        public static RFMonthEndWindow NthLastCalendarDay(int n)
        {
            return new RFMonthEndWindow(null) { CalendarDay = n };
        }

        public static RFMonthEndWindow NthLastWeekDay(int n)
        {
            return new RFMonthEndWindow(null) { WeekDay = n };
        }

        public override string ToString()
        {
            if (CalendarDay.HasValue)
            {
                return string.Format("{0}{1} day of the month", CalendarDay.Value, RFStringHelpers.OrdinalSuffix(CalendarDay.Value));
            }
            if (WeekDay.HasValue)
            {
                return string.Format("{0}{1} weekday of the month", WeekDay.Value, RFStringHelpers.OrdinalSuffix(WeekDay.Value));
            }
            return "Monthly (?)";
        }

        protected override bool InternalInRange(RFInterval interval)
        {
            var intervalDay = interval.IntervalEnd.Day;

            if (CalendarDay.HasValue && intervalDay != CalendarDay.Value)
            {
                return false;
            }
            if (WeekDay.HasValue && new RFDate(interval.IntervalEnd.Date).FirstDayOfTheMonth().OffsetWeekdays(WeekDay.Value - 1).Day != intervalDay)
            {
                return false;
            }
            return true;
        }
    }

    [DataContract]
    public abstract class RFSchedulerRange : RFSchedulerBase
    {
        protected RFSchedulerRange(string timeZone) : base(timeZone)
        {
        }

        public bool InRange(RFInterval interval)
        {
            var stInterval = ConvertToScheduleZone(interval);
            return InternalInRange(stInterval);
        }

        protected abstract bool InternalInRange(RFInterval interval);
    }

    [DataContract]
    public class RFWeeklyWindow : RFSchedulerRange
    {
        [DataMember]
        protected List<DayOfWeek> DaysOfWeek { get; set; }

        public RFWeeklyWindow(DayOfWeek dayOfWeek, string timeZone = null) : base(timeZone)
        {
            DaysOfWeek = new List<DayOfWeek> { dayOfWeek };
        }

        public RFWeeklyWindow(List<DayOfWeek> daysOfWeek, string timeZone = null) : base(timeZone)
        {
            DaysOfWeek = daysOfWeek;
        }

        public static RFWeeklyWindow AllWeek(string timeZone = null)
        {
            return new RFWeeklyWindow(new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday }, timeZone);
        }

        public static RFWeeklyWindow MonFri(string timeZone = null)
        {
            return new RFWeeklyWindow(new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, timeZone);
        }

        public static RFWeeklyWindow TueSat(string timeZone = null)
        {
            return new RFWeeklyWindow(new List<DayOfWeek> { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday }, timeZone);
        }

        public static RFWeeklyWindow Weekend(string timeZone = null)
        {
            return new RFWeeklyWindow(new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday }, timeZone);
        }

        public override string ToString()
        {
            return String.Join("/", DaysOfWeek.OrderBy(d => d).Select(d => d.ToString().Substring(0, 3)));
        }

        protected override bool InternalInRange(RFInterval interval)
        {
            var intervalDay = interval.IntervalEnd.DayOfWeek;
            return (DaysOfWeek.Contains(intervalDay));
        }
    }
}
