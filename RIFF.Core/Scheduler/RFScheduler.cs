// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

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

        protected DateTime ConvertToScheduleZone(DateTime timestamp)
        {
            if (!string.IsNullOrWhiteSpace(TimeZone))
            {
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(timestamp, TimeZone);
            }
            return timestamp;
        }

        protected RFInterval ConvertToScheduleZone(RFInterval interval)
        {
            if (!string.IsNullOrWhiteSpace(TimeZone))
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
        public RFCatalogKey TriggerKey { get; set; }

        public RFSchedulerConfig()
        {
            Schedules = new List<RFSchedulerSchedule>();
        }

        public bool ShouldTrigger(RFInterval interval)
        {
            var shouldTrigger = Schedules.Any(s => s.ShouldTrigger(interval));
            var isAllowed = Range == null || Range.InRange(interval);
            return (shouldTrigger && isAllowed);
        }
    }

    [DataContract]
    public class RFSchedulerProcessor : RFEngineProcessor<RFEngineProcessorIntervalParam>
    {
        protected Func<IRFProcessingContext, List<RFSchedulerConfig>> mConfigFunc;

        public RFSchedulerProcessor(Func<IRFProcessingContext, List<RFSchedulerConfig>> configFunc)
        {
            mConfigFunc = configFunc;
        }

        public RFSchedulerProcessor(RFSchedulerConfig config)
        {
            mConfigFunc = (_) => new List<RFSchedulerConfig> { config };
        }

        public override RFProcessingResult Process()
        {
            var interval = InstanceParams.Interval;
            var result = new RFProcessingResult();

            foreach (var config in mConfigFunc(Context))
            {
                if (config.ShouldTrigger(interval))
                {
                    var key = config.TriggerKey;
                    if (config.GraphInstance != null)
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

    [DataContract]
    public class RFScheduledTaskTriggerProcessor : RFSchedulerProcessor
    {
        public RFScheduledTaskTriggerProcessor(RFCatalogKey configKey) : base((context) => context.LoadDocumentContent<List<RFSchedulerConfig>>(configKey))
        {

        }
    }

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
