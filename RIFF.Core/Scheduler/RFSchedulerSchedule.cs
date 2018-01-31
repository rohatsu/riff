// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFDailySchedule : RFSchedulerSchedule
    {
        [DataMember]
        protected TimeSpan TimeSpan { get; set; }

        public RFDailySchedule(TimeSpan timeSpan, string timeZone = null) : base(timeZone)
        {
            TimeSpan = timeSpan;
            if(TimeSpan.TotalHours >= 24)
            {
                throw new ApplicationException("Daily Schedule needs to be less than 24 hours.");
            }
        }

        public override DateTime GetNextTrigger(DateTime startTime)
        {
            var triggerTime = startTime.Date.Add(TimeSpan);
            if(triggerTime < startTime)
            {
                triggerTime = triggerTime.AddDays(1);
            }
            return triggerTime;
        }

        public override string ToString()
        {
            return TimeSpan.ToString(@"hh\:mm");
        }
    }

    [DataContract]
    public class RFIntervalSchedule : RFSchedulerSchedule
    {
        [DataMember]
        protected TimeSpan Interval { get; set; }

        [DataMember]
        protected TimeSpan Offset { get; set; }

        public RFIntervalSchedule(TimeSpan interval, TimeSpan offset) : base(null)
        {
            Interval = interval;
            Offset = offset;
            if(Offset >= Interval)
            {
                throw new ApplicationException("Interval offset needs to be less than the interval itself.");
            }
            if(Interval.TotalSeconds < 1)
            {
                throw new ApplicationException("Invalid schedule interval.");
            }
        }

        public RFIntervalSchedule(TimeSpan interval) : base(null)
        {
            Interval = interval;
            Offset = new TimeSpan();
            if(Interval.TotalSeconds < 1)
            {
                throw new ApplicationException("Invalid schedule interval.");
            }
        }

        public override DateTime GetNextTrigger(DateTime startTime)
        {
            var offsetTime = startTime.Date.Add(Offset);
            while(offsetTime <= startTime)
            {
                offsetTime += Interval;
            }
            return offsetTime;
        }

        public override string ToString()
        {
            var intervalString = string.Join(" ", new string[] {
                Interval.Hours != 0 ? (Interval.Hours.ToString() + "h") : null,
                Interval.Minutes != 0 ? (Interval.Minutes.ToString() + "m") : null,
                Interval.Seconds != 0 ? (Interval.Seconds.ToString() + "s") : null,
            }.Where(s => s.NotBlank()));

            var offsetString = string.Join(" ", new string[] {
                Offset.Hours != 0 ? (Offset.Hours.ToString() + "h") : null,
                Offset.Minutes != 0 ? (Offset.Minutes.ToString() + "m") : null,
                Offset.Seconds != 0 ? (Offset.Seconds.ToString() + "s") : null,
            }.Where(s => s.NotBlank()));

            if(offsetString.NotBlank())
            {
                return String.Format("Every {0} (delta {1})", intervalString, offsetString);
            }
            else
            {
                return String.Format("Every {0}", intervalString);
            }
        }
    }

    [DataContract]
    public class RFCompositeSchedule : RFSchedulerSchedule
    {
        [DataMember]
        public List<RFDailySchedule> DailySchedules { get; set; }

        [DataMember]
        public List<RFIntervalSchedule> IntervalSchedules { get; set; }

        public RFCompositeSchedule(string timeZone) : base(timeZone)
        {
            DailySchedules = new List<RFDailySchedule>();
            IntervalSchedules = new List<RFIntervalSchedule>();
        }

        public override DateTime GetNextTrigger(DateTime startTime)
        {
            return DailySchedules.Select(d => d.GetNextTrigger(startTime)).Concat(IntervalSchedules.Select(i => i.GetNextTrigger(startTime))).Min();
        }

        public override string ToString()
        {
            return String.Join(", ", DailySchedules.Select(d => d.ToString()).Concat(IntervalSchedules.Select(i => i.ToString())));
        }
    }

    [DataContract]
    public abstract class RFSchedulerSchedule : RFSchedulerBase
    {
        protected RFSchedulerSchedule(string timeZone) : base(timeZone)
        {
        }

        public abstract DateTime GetNextTrigger(DateTime startTime);

        public bool ShouldTrigger(RFInterval interval)
        {
            var stInterval = ConvertToScheduleZone(interval);
            var nextTrigger = GetNextTrigger(stInterval.IntervalStart);
            return stInterval.Includes(nextTrigger);
        }

        public List<RFSchedulerSchedule> Single()
        {
            return new List<RFSchedulerSchedule> { this };
        }

        public static RFSchedulerSchedule ReadFromConfig(string configSection, string configKey, IRFUserConfig config)
        {
            var timeZone = config.GetString(configSection, configKey, false, "Time Zone");
            if(timeZone.IsBlank())
            {
                timeZone = null;
            }

            var compositeSchedule = new RFCompositeSchedule(timeZone);

            var explicitTimes = config.GetString(configSection, configKey, false, "Times");
            if(explicitTimes.NotBlank())
            {
                foreach(var token in explicitTimes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Where(t => t.NotBlank()).Select(t => t.Trim()))
                {
                    compositeSchedule.DailySchedules.Add(new RFDailySchedule(TimeSpan.Parse(token), timeZone));
                }
            }

            var offsetConfig = config.GetString(configSection, configKey, false, "Offset");
            var offset = new TimeSpan();
            if(offsetConfig.NotBlank())
            {
                offset = TimeSpan.Parse(offsetConfig);
            }

            var explicitIntervals = config.GetString(configSection, configKey, false, "Intervals");
            if(explicitIntervals.NotBlank())
            {
                foreach(var token in explicitIntervals.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Where(t => t.NotBlank()).Select(t => t.Trim()))
                {
                    compositeSchedule.IntervalSchedules.Add(new RFIntervalSchedule(TimeSpan.Parse(token), offset));
                }
            }

            return compositeSchedule;
        }
    }
}
