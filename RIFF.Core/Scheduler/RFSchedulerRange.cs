// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
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

        public RFCompositeRange() : base(null)
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
                sb.Add(String.Format("On {0}", String.Join(" & ", ExplicitRanges)));
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
            return String.Format("{0}-{1} {2}", WindowStart.ToString(@"hh\:mm"), WindowEnd.ToString(@"hh\:mm"), TimeZoneShort()).Trim();
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
                return string.Format("{0}{1} last day of the month {2}", CalendarDay.Value, RFStringHelpers.OrdinalSuffix(CalendarDay.Value), TimeZoneShort()).Trim();
            }
            if (WeekDay.HasValue)
            {
                return string.Format("{0}{1} last weekday of the month {2}", WeekDay.Value, RFStringHelpers.OrdinalSuffix(WeekDay.Value), TimeZoneShort()).Trim();
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
                return string.Format("{0}{1} day of the month {2}", CalendarDay.Value, RFStringHelpers.OrdinalSuffix(CalendarDay.Value), TimeZoneShort()).Trim();
            }
            if (WeekDay.HasValue)
            {
                return string.Format("{0}{1} weekday of the month {2}", WeekDay.Value, RFStringHelpers.OrdinalSuffix(WeekDay.Value), TimeZoneShort()).Trim();
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
        public bool IsEnabled { get; set; } = true;

        protected RFSchedulerRange(string timeZone) : base(timeZone)
        {
        }

        public bool InRange(RFInterval interval)
        {
            if (IsEnabled)
            {
                var stInterval = ConvertToScheduleZone(interval);
                return InternalInRange(stInterval);
            }
            return false;
        }

        public static Dictionary<string, DayOfWeek> _daysOfWeek = new Dictionary<string, DayOfWeek>
        {
            ["mon"] = DayOfWeek.Monday,
            ["tue"] = DayOfWeek.Tuesday,
            ["wed"] = DayOfWeek.Wednesday,
            ["thu"] = DayOfWeek.Thursday,
            ["fri"] = DayOfWeek.Friday,
            ["sat"] = DayOfWeek.Saturday,
            ["sun"] = DayOfWeek.Sunday,
        };

        protected static List<DayOfWeek> ParseDays(string weekdaysConfig)
        {
            var weekdays = new List<DayOfWeek>();
            foreach (var token in weekdaysConfig.Split(',').Where(w => w.NotBlank()).Select(w => w.Trim().ToLower()))
            {
                if (token.Contains('-'))
                {
                    var start = token.Split('-')[0];
                    var end = token.Split('-')[1];
                    if (_daysOfWeek.ContainsKey(start) && _daysOfWeek.ContainsKey(end))
                    {
                        var dayOfWeek = _daysOfWeek[start];
                        var endDay = _daysOfWeek[end];
                        do
                        {
                            weekdays.Add(dayOfWeek);
                            if (dayOfWeek == DayOfWeek.Saturday)
                            {
                                dayOfWeek = DayOfWeek.Sunday;
                            }
                            else
                            {
                                dayOfWeek++;
                            }
                        } while (dayOfWeek != endDay);
                        weekdays.Add(endDay);
                    }
                }
                else if (_daysOfWeek.ContainsKey(token))
                {
                    weekdays.Add(_daysOfWeek[token]);
                }
            }
            return weekdays;
        }

        protected static (TimeSpan, TimeSpan) ParseWindow(string windowStart, string windowEnd)
        {
            var start = windowStart.IsBlank() ? new TimeSpan(0, 0, 0) : TimeSpan.Parse(windowStart);
            var end = windowEnd.IsBlank() ? new TimeSpan(23, 59, 59) : TimeSpan.Parse(windowEnd);
            return (start, end);
        }

        public static RFSchedulerRange ReadFromConfig(string configSection, string configKey, IRFUserConfig config)
        {
            var compositeRange = new RFCompositeRange();
            var timeZone = config.GetString(configSection, configKey, false, "Time Zone");
            if (timeZone.IsBlank())
            {
                timeZone = null;
            }

            var allowDays = config.GetString(configSection, configKey, false, "Allow Days"); // mon,tue or mon-fri
            if (allowDays.NotBlank())
            {
                compositeRange.AllowRanges.Add(new RFWeeklyWindow(ParseDays(allowDays), timeZone));
            }
            var denyDays = config.GetString(configSection, configKey, false, "Deny Days"); // mon,tue or mon-fri
            if (denyDays.NotBlank())
            {
                compositeRange.DenyRanges.Add(new RFWeeklyWindow(ParseDays(denyDays), timeZone));
            }

            var awindowStart = config.GetString(configSection, configKey, false, "Allow Window Start"); // HH:mm
            var awindowEnd = config.GetString(configSection, configKey, false, "Allow Window End"); // HH:mm
            if (awindowStart.NotBlank() || awindowEnd.NotBlank())
            {
                var window = ParseWindow(awindowStart, awindowEnd);
                compositeRange.AllowRanges.Add(new RFDailyWindow(window.Item1, window.Item2, timeZone));
            }

            var dwindowStart = config.GetString(configSection, configKey, false, "Deny Window Start"); // HH:mm
            var dwindowEnd = config.GetString(configSection, configKey, false, "Deny Window End"); // HH:mm
            if (dwindowStart.NotBlank() || dwindowEnd.NotBlank())
            {
                var window = ParseWindow(dwindowStart, dwindowEnd);
                compositeRange.DenyRanges.Add(new RFDailyWindow(window.Item1, window.Item2, timeZone));
            }

            return compositeRange;
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

        public static RFWeeklyWindow Never()
        {
            return new RFWeeklyWindow(new List<DayOfWeek>());
        }

        public override string ToString()
        {
            switch(DaysOfWeek.Count)
            {
                case 7:
                    return String.Format("Every day");
                case 5:
                    if (!DaysOfWeek.Contains(DayOfWeek.Saturday) && !DaysOfWeek.Contains(DayOfWeek.Sunday))
                    {
                        return String.Format("Mon-Fri {0}", TimeZoneShort()).Trim();
                    }
                    if (!DaysOfWeek.Contains(DayOfWeek.Monday) && !DaysOfWeek.Contains(DayOfWeek.Sunday))
                    {
                        return String.Format("Tue-Sat {0}", TimeZoneShort()).Trim();
                    }
                    break;
                case 0:
                    return "Never";
            }
            return String.Format("{0} {1}", String.Join("/", DaysOfWeek.OrderBy(d => d).Select(d => d.ToString().Substring(0, 3))), TimeZoneShort()).Trim();
        }

        protected override bool InternalInRange(RFInterval interval)
        {
            var intervalDay = interval.IntervalEnd.DayOfWeek;
            return (DaysOfWeek.Contains(intervalDay));
        }
    }
}
