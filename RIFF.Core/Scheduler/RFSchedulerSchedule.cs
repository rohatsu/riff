// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
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
            if (TimeSpan.TotalHours >= 24)
            {
                throw new ApplicationException("Daily Schedule needs to be less than 24 hours.");
            }
        }

        public override DateTime GetNextTrigger(DateTime startTime)
        {
            var triggerTime = startTime.Date.Add(TimeSpan);
            if (triggerTime < startTime)
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
            if (Offset >= Interval)
            {
                throw new ApplicationException("Interval offset needs to be less than the interval itself.");
            }
            if (Interval.TotalSeconds < 1)
            {
                throw new ApplicationException("Invalid schedule interval.");
            }
        }

        public RFIntervalSchedule(TimeSpan interval) : base(null)
        {
            Interval = interval;
            Offset = new TimeSpan();
            if (Interval.TotalSeconds < 1)
            {
                throw new ApplicationException("Invalid schedule interval.");
            }
        }

        public override DateTime GetNextTrigger(DateTime startTime)
        {
            var offsetTime = startTime.Date.Add(Offset);
            while (offsetTime <= startTime)
            {
                offsetTime += Interval;
            }
            return offsetTime;
        }

        public override string ToString()
        {
            var intervalString = ((Interval.Hours != 0 ? (Interval.Hours.ToString() + "h") : String.Empty) + " " + (Interval.Minutes != 0 ? (Interval.Minutes.ToString() + "m") : String.Empty)).Trim();
            var offsetString = ((Offset.Hours != 0 ? (Offset.Hours.ToString() + "h") : String.Empty) + " " + (Offset.Minutes != 0 ? (Offset.Minutes.ToString() + "m") : String.Empty)).Trim();
            if (offsetString.NotBlank())
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
    }
}
