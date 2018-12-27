// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

namespace RIFF.Core
{
    [DataContract]
    public abstract class RFSchedulerBase
    {
        [DataMember]
        public string TimeZone { get; private set; }

        protected RFSchedulerBase(string timeZone)
        {
            TimeZone = timeZone;
        }

        public string TimeZoneShort()
        {
            if (TimeZone.NotBlank())
            {
                switch (TimeZone)
                {
                    case "GMT Standard Time":
                        return "UK";
                    case "Greenwich Standard Time":
                        return "GMT";
                    case "UTC":
                        return "UTC";
                }

                if(TimeZone.Contains(" Standard Time"))
                {
                    return TimeZone.Substring(0, TimeZone.IndexOf(" Standard Time"));
                }

                return TimeZone;
            }
            return string.Empty;
        }

        protected DateTime ConvertToScheduleZone(DateTime timestamp)
        {
            if(!string.IsNullOrWhiteSpace(TimeZone))
            {
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(timestamp, TimeZone);
            }
            return timestamp;
        }

        protected RFInterval ConvertToScheduleZone(RFInterval interval)
        {
            if(!string.IsNullOrWhiteSpace(TimeZone))
            {
                var stIntervalStart = ConvertToScheduleZone(interval.IntervalStart);
                var stIntervalEnd = ConvertToScheduleZone(interval.IntervalEnd);
                return new RFInterval(stIntervalStart, stIntervalEnd);
            }
            return interval;
        }
    }

    [DataContract]
    public class RFSchedulerConfig : IRFGraphProcessorConfig
    {
        [DataMember]
        public Func<RFInterval, RFGraphInstance> GraphInstance { get; set; }

        [DataMember]
        public RFSchedulerRange Range { get; set; }

        [DataMember]
        public List<RFSchedulerSchedule> Schedules { get; set; }

        [DataMember]
        public bool IsEnabled { get; set; }

        [DataMember]
        public RFCatalogKey TriggerKey { get; set; }

        public RFSchedulerConfig()
        {
            Schedules = new List<RFSchedulerSchedule>();
            IsEnabled = true;
        }

        public string SchedulerRangeString
        {
            get
            {
                return Range?.ToString() ?? String.Empty;
            }
        }

        public string SchedulerScheduleString
        {
            get
            {
                return Schedules != null ? String.Join(", ", Schedules.Select(s => s.ToString())) : String.Empty;
            }
        }

        public bool ShouldTrigger(RFInterval interval)
        {
            var shouldTrigger = Schedules.Any(s => s.ShouldTrigger(interval));
            var isAllowed = Range == null || Range.InRange(interval);
            return (IsEnabled && shouldTrigger && isAllowed);
        }
    }

    [DataContract]
    public class RFSchedulerProcessor : RFEngineProcessor<RFEngineProcessorIntervalParam>
    {
        protected List<Func<IRFProcessingContext, RFSchedulerConfig>> mConfigFunc;

        public RFSchedulerProcessor(List<Func<IRFProcessingContext, RFSchedulerConfig>> configFunc)
        {
            mConfigFunc = configFunc;
        }

        public RFSchedulerProcessor(RFSchedulerConfig config)
        {
            mConfigFunc = new List<Func<IRFProcessingContext, RFSchedulerConfig>> { (_) => config };
        }

        public override RFProcessingResult Process()
        {
            var interval = InstanceParams.Interval;
            var result = new RFProcessingResult();

            foreach(var configFunc in mConfigFunc)
            {
                var config = configFunc(Context);
                if(config.ShouldTrigger(interval))
                {
                    var key = config.TriggerKey;
                    if(config.GraphInstance != null)
                    {
                        key = key.CreateForInstance(config.GraphInstance(interval));
                        Context.SaveEntry(RFDocument.Create(key, new RFGraphProcessorTrigger { TriggerStatus = true, TriggerTime = interval.IntervalEnd }));
                    }
                    else
                    {
                        Context.SaveEntry(RFDocument.Create(key, new RFScheduleTrigger { LastTriggerTime = interval.IntervalEnd }));
                    }
                    result.WorkDone = true;
                }
            }
            return result;
        }
    }

    /*
    [DataContract]
    public class RFScheduledTaskTriggerProcessor : RFSchedulerProcessor
    {
        public RFScheduledTaskTriggerProcessor(RFCatalogKey configKey) : base((context) => context.LoadDocumentContent<List<RFSchedulerConfig>>(configKey))
        {

        }
    }*/

    [DataContract]
    public class RFSchedulerTriggerKey : RFCatalogKey
    {
        [DataMember(EmitDefaultValue = false)]
        public RFEnum TriggerName { get; set; }

        public static RFCatalogKey Create(RFKeyDomain domain, RFEnum triggerName)
        {
            return domain.Associate(new RFSchedulerTriggerKey
            {
                TriggerName = triggerName,
                StoreType = RFStoreType.Document,
                Plane = RFPlane.Ephemeral
            });
        }

        public override string FriendlyString()
        {
            return string.Format("{0}", TriggerName);
        }
    }

    [DataContract]
    public class RFScheduleTrigger
    {
        [DataMember]
        public DateTime LastTriggerTime { get; set; }
    }
}
